using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAllowAnonymous]
    public class RestaurantCustomerOrderingAppService(
        IRepository<RestaurantMenuCategory, Guid> categoryRepository,
        IRepository<RestaurantMenuItem, Guid> menuItemRepository,
        IRepository<RestaurantMenuVariant, Guid> variantRepository,
        IRepository<RestaurantModifierGroup, Guid> modifierGroupRepository,
        IRepository<RestaurantModifier, Guid> modifierRepository,
        IRepository<RestaurantMenuItemModifierGroup, Guid> menuItemModifierGroupRepository,
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantOrderItemModifier, Guid> orderItemModifierRepository,
        IRepository<RestaurantTable, Guid> tableRepository,
        IRepository<RestaurantTableSession, Guid> tableSessionRepository,
        IRepository<RestaurantChannel, Guid> channelRepository,
        IRepository<Product, Guid> productRepository)
        : ErpAppServiceBase, IRestaurantCustomerOrderingAppService
    {
        public async Task<RestaurantCustomerMenuDto> GetMenu(RestaurantCustomerMenuRequestDto input)
        {
            var tenantId = ResolveTenantId(input?.TenantId);
            await EnsurePublicOrderingAvailable(tenantId);
            var now = DateTime.Now;
            var search = input?.Search?.Trim();
            var categoryId = input?.CategoryId;

            var categories = await categoryRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new RestaurantMenuCategoryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            var query = menuItemRepository.GetAll()
                .Include(x => x.CategoryFk)
                .Include(x => x.ProductFk)
                .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
                .Where(x => x.IsAvailable && (!x.UnavailableUntil.HasValue || x.UnavailableUntil <= now))
                .Where(x => !categoryId.HasValue || x.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.DisplayName.Contains(search) ||
                    x.ProductFk.Name.Contains(search) ||
                    x.CategoryFk.Name.Contains(search) ||
                    x.ShortCode.Contains(search));
            }

            var menuItems = await query
                .OrderBy(x => x.CategoryFk.SortOrder)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();

            var result = new RestaurantCustomerMenuDto { Categories = categories };
            foreach (var item in menuItems)
            {
                result.Items.Add(new RestaurantCustomerMenuItemDto
                {
                    MenuItemId = item.Id,
                    ProductId = item.ProductId,
                    CategoryId = item.CategoryId,
                    CategoryName = item.CategoryFk.Name,
                    Name = string.IsNullOrWhiteSpace(item.DisplayName) ? item.ProductFk.Name : item.DisplayName,
                    Description = item.Description,
                    ImageUrl = item.ImageUrl,
                    Price = item.Price,
                    IsAvailable = true,
                    IsVeg = item.IsVeg,
                    SpiceLevel = item.SpiceLevel,
                    Variants = await GetVariants(item.Id, tenantId),
                    ModifierGroups = await GetModifierGroups(item.Id, tenantId)
                });
            }

            return result;
        }

        public async Task<RestaurantCustomerQuoteDto> Quote(QuoteRestaurantCustomerOrderDto input)
        {
            var tenantId = ResolveTenantId(input?.TenantId);
            if (input?.Lines == null || input.Lines.All(x => x.Qty <= 0))
                throw new UserFriendlyException("At least one order item is required");

            var quote = new RestaurantCustomerQuoteDto();
            foreach (var line in input.Lines.Where(x => x.Qty > 0))
            {
                var quoteLine = await BuildQuoteLine(line, tenantId);
                quote.Lines.Add(quoteLine);
            }

            quote.GrossAmount = quote.Lines.Sum(x => x.Qty * x.Rate + x.ModifierTotal);
            quote.DiscountAmount = 0;
            quote.TaxAmount = quote.Lines.Sum(x => x.TaxAmount);
            quote.NetAmount = quote.GrossAmount;
            quote.GrandTotal = quote.Lines.Sum(x => x.Amount);
            return quote;
        }

        public async Task<CreateRestaurantCustomerOrderResultDto> CreateOrder(CreateRestaurantCustomerOrderDto input)
        {
            var tenantId = ResolveTenantId(input?.TenantId);
            if (input == null)
                throw new UserFriendlyException("Customer order is required");
            if (input.PaymentMode is not RestaurantCustomerPaymentMode.CashOnDelivery and not RestaurantCustomerPaymentMode.CounterSettlement)
                throw new UserFriendlyException("Only COD and counter settlement are supported in v1");
            if (string.IsNullOrWhiteSpace(input.CustomerPhoneNo))
                throw new UserFriendlyException("Customer phone number is required");
            var orderLines = input.Lines?.Where(x => x.Qty > 0).ToList() ?? new List<RestaurantCustomerOrderLineDto>();
            if (orderLines.Count == 0)
                throw new UserFriendlyException("At least one order item is required");

            if (!string.IsNullOrWhiteSpace(input.ClientRequestId))
            {
                var existing = await orderRepository.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.ClientRequestId == input.ClientRequestId);
                if (existing != null)
                {
                    return new CreateRestaurantCustomerOrderResultDto
                    {
                        OrderId = existing.Id,
                        OrderNo = existing.OrderNo,
                        Status = existing.Status,
                        Quote = await Quote(input)
                    };
                }
            }

            var quote = await Quote(input);
            RestaurantTableSession session = null;
            if (input.TableId.HasValue)
                session = await EnsureTableSession(input, tenantId);

            var order = new RestaurantOrder
            {
                TenantId = tenantId,
                OrderNo = await GetNextOrderNo(tenantId),
                OrderType = input.TableId.HasValue && input.OrderType == RestaurantOrderType.Mobile
                    ? RestaurantOrderType.Qr
                    : input.OrderType,
                Status = RestaurantOrderStatus.Draft,
                TableId = input.TableId,
                TableSessionId = session?.Id,
                Source = "CustomerApp",
                ClientRequestId = input.ClientRequestId,
                CustomerName = input.CustomerName,
                CustomerPhoneNo = input.CustomerPhoneNo,
                Notes = BuildCustomerOrderNotes(input),
                GrossAmount = quote.GrossAmount,
                DiscountAmount = quote.DiscountAmount,
                TaxAmount = quote.TaxAmount,
                NetAmount = quote.NetAmount,
                GrandTotal = quote.GrandTotal,
                CreatedAt = DateTime.Now
            };

            var orderId = await orderRepository.InsertAndGetIdAsync(order);
            for (var i = 0; i < orderLines.Count; i++)
                await CreateOrderItem(orderId, orderLines[i], quote.Lines[i], tenantId);

            if (input.TableId.HasValue)
            {
                var table = await tableRepository.FirstOrDefaultAsync(x => x.Id == input.TableId.Value && x.TenantId == tenantId);
                if (table != null)
                {
                    table.Status = RestaurantTableStatus.Occupied;
                    await tableRepository.UpdateAsync(table);
                }
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return new CreateRestaurantCustomerOrderResultDto
            {
                OrderId = orderId,
                OrderNo = order.OrderNo,
                Status = order.Status,
                Quote = quote
            };
        }

        public async Task<RestaurantCustomerOrderStatusDto> GetOrderStatus(Guid orderId)
        {
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
                throw new UserFriendlyException("Restaurant order not found");

            return new RestaurantCustomerOrderStatusDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                Status = order.Status,
                GrandTotal = order.GrandTotal,
                CreatedAt = order.CreatedAt,
                SentAt = order.SentAt
            };
        }

        private async Task<RestaurantCustomerQuoteLineDto> BuildQuoteLine(RestaurantCustomerOrderLineDto input, int tenantId)
        {
            var menuItem = await menuItemRepository.GetAll()
                .Include(x => x.ProductFk)
                .FirstOrDefaultAsync(x => x.Id == input.MenuItemId && x.TenantId == tenantId && !x.IsDeleted);
            if (menuItem == null)
                throw new UserFriendlyException("Menu item not found");
            if (!menuItem.IsActive || !menuItem.IsAvailable || (menuItem.UnavailableUntil.HasValue && menuItem.UnavailableUntil > DateTime.Now))
                throw new UserFriendlyException("Menu item is currently unavailable", menuItem.DisplayName);

            var rate = menuItem.Price;
            string variantName = null;
            if (input.VariantId.HasValue && input.VariantId.Value != Guid.Empty)
            {
                var variant = await variantRepository.FirstOrDefaultAsync(x =>
                    x.Id == input.VariantId.Value &&
                    x.MenuItemId == input.MenuItemId &&
                    x.TenantId == tenantId &&
                    x.IsActive &&
                    !x.IsDeleted);
                if (variant == null)
                    throw new UserFriendlyException("Menu variant not found");

                variantName = variant.Name;
                rate = variant.IsAbsolutePrice ? variant.PriceDelta : menuItem.Price + variant.PriceDelta;
            }

            var modifierTotal = await GetModifierTotal(input, tenantId);
            var taxRate = menuItem.ProductFk.TaxId == Guid.Empty ? 0 : ERPCommonManager.GetTaxRate(menuItem.ProductFk.TaxId);
            var gross = input.Qty * rate + modifierTotal;
            var tax = gross * taxRate;

            return new RestaurantCustomerQuoteLineDto
            {
                MenuItemId = input.MenuItemId,
                VariantId = input.VariantId,
                ItemName = string.IsNullOrWhiteSpace(menuItem.DisplayName) ? menuItem.ProductFk.Name : menuItem.DisplayName,
                VariantName = variantName,
                Qty = input.Qty,
                Rate = rate,
                ModifierTotal = modifierTotal,
                TaxAmount = tax,
                Amount = gross + tax
            };
        }

        private async Task<decimal> GetModifierTotal(RestaurantCustomerOrderLineDto input, int tenantId)
        {
            decimal total = 0;
            foreach (var modifierInput in input.Modifiers ?? new List<RestaurantCustomerOrderModifierDto>())
            {
                var modifier = await modifierRepository.GetAll()
                    .Include(x => x.ModifierGroupFk)
                    .FirstOrDefaultAsync(x => x.Id == modifierInput.ModifierId && x.TenantId == tenantId && x.IsActive && !x.IsDeleted);
                if (modifier == null)
                    throw new UserFriendlyException("Modifier not found");

                var allowed = await menuItemModifierGroupRepository.CountAsync(x =>
                    x.TenantId == tenantId &&
                    x.MenuItemId == input.MenuItemId &&
                    x.ModifierGroupId == modifier.ModifierGroupId) > 0;
                if (!allowed)
                    throw new UserFriendlyException("Modifier is not available for this menu item");

                total += input.Qty * modifier.PriceDelta * Math.Max(1, modifierInput.Qty);
            }

            return total;
        }

        private async Task CreateOrderItem(Guid orderId, RestaurantCustomerOrderLineDto input, RestaurantCustomerQuoteLineDto quoteLine, int tenantId)
        {
            var menuItem = await menuItemRepository.GetAll()
                .Include(x => x.ProductFk)
                .Include(x => x.StationFk)
                .FirstAsync(x => x.Id == input.MenuItemId && x.TenantId == tenantId);

            var orderItem = new RestaurantOrderItem
            {
                TenantId = tenantId,
                OrderId = orderId,
                MenuItemId = input.MenuItemId,
                VariantId = input.VariantId == Guid.Empty ? null : input.VariantId,
                ProductId = menuItem.ProductId,
                UnitId = menuItem.ProductFk.UnitId,
                StationId = menuItem.StationId,
                TaxId = menuItem.ProductFk.TaxId,
                Qty = input.Qty,
                Rate = quoteLine.Rate,
                ItemNameSnapshot = quoteLine.ItemName,
                VariantNameSnapshot = quoteLine.VariantName,
                StationTypeSnapshot = menuItem.StationFk?.StationType,
                UnitPriceSnapshot = quoteLine.Rate,
                ModifierTotal = quoteLine.ModifierTotal,
                DiscountAmount = 0,
                NetAmount = input.Qty * quoteLine.Rate + quoteLine.ModifierTotal,
                TaxAmount = quoteLine.TaxAmount,
                Amount = quoteLine.Amount,
                Notes = input.Notes,
                Status = RestaurantOrderItemStatus.Draft,
                CreatedAt = DateTime.Now
            };

            var orderItemId = await orderItemRepository.InsertAndGetIdAsync(orderItem);
            foreach (var modifierInput in input.Modifiers ?? new List<RestaurantCustomerOrderModifierDto>())
            {
                var modifier = await modifierRepository.GetAll()
                    .FirstAsync(x => x.Id == modifierInput.ModifierId && x.TenantId == tenantId);
                var qty = Math.Max(1, modifierInput.Qty);
                await orderItemModifierRepository.InsertAsync(new RestaurantOrderItemModifier
                {
                    TenantId = tenantId,
                    OrderItemId = orderItemId,
                    ModifierId = modifier.Id,
                    ModifierNameSnapshot = modifier.Name,
                    PriceDelta = modifier.PriceDelta,
                    Qty = qty,
                    Amount = input.Qty * modifier.PriceDelta * qty
                });
            }
        }

        private async Task<RestaurantTableSession> EnsureTableSession(CreateRestaurantCustomerOrderDto input, int tenantId)
        {
            var table = await tableRepository.FirstOrDefaultAsync(x =>
                x.Id == input.TableId.Value &&
                x.TenantId == tenantId &&
                x.IsActive);
            if (table == null)
                throw new UserFriendlyException("Restaurant table not found");

            var session = await tableSessionRepository.GetAll()
                .Where(x => x.TenantId == tenantId &&
                            x.TableId == input.TableId.Value &&
                            x.ClosedAt == null &&
                            x.Status != RestaurantOrderStatus.Closed &&
                            x.Status != RestaurantOrderStatus.Cancelled)
                .OrderByDescending(x => x.OpenedAt)
                .FirstOrDefaultAsync();

            if (session != null)
                return session;

            session = new RestaurantTableSession
            {
                TenantId = tenantId,
                TableId = input.TableId.Value,
                SessionNo = "TS-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                Status = RestaurantOrderStatus.Draft,
                OpenedAt = DateTime.Now,
                CustomerName = input.CustomerName,
                CustomerPhoneNo = input.CustomerPhoneNo
            };
            session.Id = await tableSessionRepository.InsertAndGetIdAsync(session);
            return session;
        }

        private async Task<List<RestaurantMenuVariantDto>> GetVariants(Guid menuItemId, int tenantId)
        {
            return await variantRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.MenuItemId == menuItemId && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .Select(x => new RestaurantMenuVariantDto
                {
                    Id = x.Id,
                    MenuItemId = x.MenuItemId,
                    Name = x.Name,
                    PriceDelta = x.PriceDelta,
                    IsAbsolutePrice = x.IsAbsolutePrice,
                    IsDefault = x.IsDefault,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        private async Task<List<RestaurantModifierGroupDto>> GetModifierGroups(Guid menuItemId, int tenantId)
        {
            var groupIds = await menuItemModifierGroupRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.MenuItemId == menuItemId)
                .Select(x => x.ModifierGroupId)
                .ToListAsync();

            var groups = await modifierGroupRepository.GetAll()
                .Where(x => x.TenantId == tenantId && groupIds.Contains(x.Id) && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            var result = new List<RestaurantModifierGroupDto>();
            foreach (var group in groups)
            {
                result.Add(new RestaurantModifierGroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    MinSelect = group.MinSelect,
                    MaxSelect = group.MaxSelect,
                    IsRequired = group.IsRequired,
                    SortOrder = group.SortOrder,
                    IsActive = group.IsActive,
                    Modifiers = await modifierRepository.GetAll()
                        .Where(x => x.TenantId == tenantId && x.ModifierGroupId == group.Id && x.IsActive && !x.IsDeleted)
                        .OrderBy(x => x.SortOrder)
                        .Select(x => new RestaurantModifierDto
                        {
                            Id = x.Id,
                            ModifierGroupId = x.ModifierGroupId,
                            Name = x.Name,
                            PriceDelta = x.PriceDelta,
                            SortOrder = x.SortOrder,
                            IsActive = x.IsActive
                        })
                        .ToListAsync()
                });
            }

            return result;
        }

        private int ResolveTenantId(int? inputTenantId)
        {
            if (!inputTenantId.HasValue && !AbpSession.UserId.HasValue)
                throw new UserFriendlyException("Tenant id is required for customer ordering");

            var tenantId = inputTenantId ?? AbpSession.TenantId;
            if (!tenantId.HasValue)
                throw new UserFriendlyException("Tenant id is required for customer ordering");
            return tenantId.Value;
        }

        private async Task EnsurePublicOrderingAvailable(int tenantId)
        {
            var channelAvailabilityEnabled = string.Equals(
                await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled,
                    tenantId),
                "true",
                StringComparison.OrdinalIgnoreCase);
            if (!channelAvailabilityEnabled)
                throw new UserFriendlyException("Customer ordering is currently unavailable");

            var ownOnlineIsOpen = await channelRepository.CountAsync(x =>
                x.TenantId == tenantId &&
                x.Provider == RestaurantChannelProvider.OwnOnline &&
                x.IsActive &&
                x.IsOnline &&
                !x.IsDeleted) > 0;
            if (!ownOnlineIsOpen)
                throw new UserFriendlyException("Own online ordering channel is currently offline");
        }

        private async Task<string> GetNextOrderNo(int tenantId)
        {
            var count = await orderRepository.CountAsync(x => x.TenantId == tenantId);
            return "RO-" + DateTime.Now.ToString("yyyyMMdd") + "-" + (count + 1).ToString("0000");
        }

        private static string BuildCustomerOrderNotes(CreateRestaurantCustomerOrderDto input)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(input.Notes))
                parts.Add(input.Notes.Trim());
            if (!string.IsNullOrWhiteSpace(input.DeliveryAddress))
                parts.Add("Delivery: " + input.DeliveryAddress.Trim());
            parts.Add("Payment: " + input.PaymentMode);
            return string.Join(" | ", parts);
        }
    }
}
