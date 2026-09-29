using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantChannels)]
    public class RestaurantChannelAppService(
        IRepository<RestaurantChannel, Guid> channelRepository,
        IRepository<RestaurantChannelAccount, Guid> accountRepository,
        IRepository<RestaurantChannelItem, Guid> channelItemRepository,
        IRepository<RestaurantMenuSyncLog, Guid> syncLogRepository,
        IRepository<RestaurantAggregatorOrder, Guid> aggregatorOrderRepository,
        IRepository<RestaurantAggregatorPayout, Guid> payoutRepository,
        IRepository<RestaurantAggregatorPayoutLine, Guid> payoutLineRepository,
        IRepository<RestaurantMenuItem, Guid> menuItemRepository,
        IRestaurantCustomerOrderingAppService customerOrderingAppService,
        IRestaurantOrderAppService orderAppService)
        : ErpAppServiceBase, IRestaurantChannelAppService
    {
        public async Task<List<RestaurantChannelDto>> GetChannels()
        {
            return await channelRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new RestaurantChannelDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ChannelType = x.ChannelType,
                    Provider = x.Provider,
                    CommissionPercent = x.CommissionPercent,
                    DefaultPriceMarkupPercent = x.DefaultPriceMarkupPercent,
                    SortOrder = x.SortOrder,
                    IsOnline = x.IsOnline,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<Guid> CreateOrEditChannel(CreateOrEditRestaurantChannelDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Channel name is required");

            var tenantId = AbpSession.GetTenantId();
            RestaurantChannel channel;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                channel = new RestaurantChannel { TenantId = tenantId };
                await channelRepository.InsertAsync(channel);
            }
            else
            {
                channel = await channelRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.TenantId == tenantId);
                if (channel == null) throw new UserFriendlyException("Restaurant channel not found");
            }

            channel.Name = input.Name.Trim();
            channel.ChannelType = input.ChannelType;
            channel.Provider = input.Provider;
            channel.CommissionPercent = Math.Max(0, input.CommissionPercent);
            channel.DefaultPriceMarkupPercent = Math.Max(0, input.DefaultPriceMarkupPercent);
            channel.SortOrder = input.SortOrder;
            channel.IsOnline = input.IsOnline;
            channel.IsActive = input.IsActive;

            await CurrentUnitOfWork.SaveChangesAsync();
            return channel.Id;
        }

        public async Task<List<RestaurantChannelAccountDto>> GetChannelAccounts(Guid? channelId)
        {
            return await accountRepository.GetAll()
                .Include(x => x.ChannelFk)
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .Where(x => !channelId.HasValue || x.ChannelId == channelId)
                .OrderBy(x => x.ChannelFk.SortOrder)
                .ThenBy(x => x.DisplayName)
                .Select(x => new RestaurantChannelAccountDto
                {
                    Id = x.Id,
                    ChannelId = x.ChannelId,
                    ChannelName = x.ChannelFk.Name,
                    Provider = x.Provider,
                    ExternalStoreId = x.ExternalStoreId,
                    DisplayName = x.DisplayName,
                    ApiBaseUrl = x.ApiBaseUrl,
                    ApiCredentialsJson = x.ApiCredentialsJson,
                    WebhookSecret = x.WebhookSecret,
                    IsOnline = x.IsOnline,
                    IsActive = x.IsActive,
                    LastMenuSyncAt = x.LastMenuSyncAt,
                    LastOrderSyncAt = x.LastOrderSyncAt
                })
                .ToListAsync();
        }

        public async Task<Guid> CreateOrEditChannelAccount(CreateOrEditRestaurantChannelAccountDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            if (await channelRepository.CountAsync(x => x.Id == input.ChannelId && x.TenantId == tenantId && !x.IsDeleted) == 0)
                throw new UserFriendlyException("Restaurant channel not found");

            RestaurantChannelAccount account;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                account = new RestaurantChannelAccount { TenantId = tenantId };
                await accountRepository.InsertAsync(account);
            }
            else
            {
                account = await accountRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.TenantId == tenantId);
                if (account == null) throw new UserFriendlyException("Restaurant channel account not found");
            }

            account.ChannelId = input.ChannelId;
            account.Provider = input.Provider;
            account.ExternalStoreId = input.ExternalStoreId?.Trim();
            account.DisplayName = input.DisplayName?.Trim();
            account.ApiBaseUrl = input.ApiBaseUrl?.Trim();
            account.ApiCredentialsJson = input.ApiCredentialsJson;
            account.WebhookSecret = input.WebhookSecret;
            account.IsOnline = input.IsOnline;
            account.IsActive = input.IsActive;

            await CurrentUnitOfWork.SaveChangesAsync();
            return account.Id;
        }

        public async Task<List<RestaurantChannelItemDto>> GetChannelItems(Guid? channelId)
        {
            return await channelItemRepository.GetAll()
                .Include(x => x.ChannelFk)
                .Include(x => x.MenuItemFk)
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .Where(x => !channelId.HasValue || x.ChannelId == channelId)
                .OrderBy(x => x.ChannelFk.SortOrder)
                .ThenBy(x => x.MenuItemFk.DisplayName)
                .Select(x => new RestaurantChannelItemDto
                {
                    Id = x.Id,
                    ChannelId = x.ChannelId,
                    ChannelName = x.ChannelFk.Name,
                    MenuItemId = x.MenuItemId,
                    MenuItemName = x.MenuItemFk.DisplayName,
                    ExternalItemId = x.ExternalItemId,
                    ExternalSku = x.ExternalSku,
                    BasePrice = x.MenuItemFk.Price,
                    ChannelPrice = x.ChannelPrice,
                    IsOnline = x.IsOnline,
                    SyncStatus = x.SyncStatus,
                    LastSyncedAt = x.LastSyncedAt,
                    LastSyncMessage = x.LastSyncMessage
                })
                .ToListAsync();
        }

        public async Task<Guid> SaveChannelItem(SaveRestaurantChannelItemDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var menuItem = await menuItemRepository.FirstOrDefaultAsync(x => x.Id == input.MenuItemId && x.TenantId == tenantId && !x.IsDeleted);
            if (menuItem == null) throw new UserFriendlyException("Menu item not found");
            if (await channelRepository.CountAsync(x => x.Id == input.ChannelId && x.TenantId == tenantId && !x.IsDeleted) == 0)
                throw new UserFriendlyException("Restaurant channel not found");

            RestaurantChannelItem item;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                item = await channelItemRepository.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.ChannelId == input.ChannelId &&
                    x.MenuItemId == input.MenuItemId &&
                    !x.IsDeleted);

                if (item == null)
                {
                    item = new RestaurantChannelItem { TenantId = tenantId };
                    await channelItemRepository.InsertAsync(item);
                }
            }
            else
            {
                item = await channelItemRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.TenantId == tenantId);
                if (item == null) throw new UserFriendlyException("Restaurant channel item not found");
            }

            item.ChannelId = input.ChannelId;
            item.MenuItemId = input.MenuItemId;
            item.ExternalItemId = input.ExternalItemId?.Trim();
            item.ExternalSku = input.ExternalSku?.Trim();
            item.ChannelPrice = input.ChannelPrice > 0 ? input.ChannelPrice : menuItem.Price;
            item.IsOnline = input.IsOnline;
            item.SyncStatus = RestaurantChannelSyncStatus.PendingPush;
            item.LastSyncMessage = "Queued for menu sync";

            await CurrentUnitOfWork.SaveChangesAsync();
            return item.Id;
        }

        public async Task<List<RestaurantMenuSyncLogDto>> QueueMenuPublish(Guid channelId)
        {
            var tenantId = AbpSession.GetTenantId();
            var channel = await channelRepository.FirstOrDefaultAsync(x => x.Id == channelId && x.TenantId == tenantId && !x.IsDeleted);
            if (channel == null)
                throw new UserFriendlyException("Restaurant channel not found");

            var menuItems = await menuItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
                .ToListAsync();
            var channelAvailabilityEnabled = string.Equals(
                await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled,
                    tenantId),
                "true",
                StringComparison.OrdinalIgnoreCase);

            var logs = new List<RestaurantMenuSyncLog>();
            foreach (var menuItem in menuItems)
            {
                var channelItem = await channelItemRepository.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.ChannelId == channelId &&
                    x.MenuItemId == menuItem.Id &&
                    !x.IsDeleted);
                if (channelItem == null)
                {
                    channelItem = new RestaurantChannelItem
                    {
                        TenantId = tenantId,
                        ChannelId = channelId,
                        MenuItemId = menuItem.Id,
                        ChannelPrice = ApplyMarkup(menuItem.Price, channel.DefaultPriceMarkupPercent),
                        IsOnline = channelAvailabilityEnabled && menuItem.IsAvailable
                    };
                    await channelItemRepository.InsertAsync(channelItem);
                }

                if (!channelAvailabilityEnabled)
                    channelItem.IsOnline = false;

                channelItem.SyncStatus = channelItem.IsOnline
                    ? RestaurantChannelSyncStatus.PendingPush
                    : RestaurantChannelSyncStatus.Disabled;
                channelItem.LastSyncMessage = "Queued for " + channel.Provider + " menu publish";

                var log = new RestaurantMenuSyncLog
                {
                    TenantId = tenantId,
                    ChannelId = channelId,
                    ChannelItemId = channelItem.Id,
                    Provider = channel.Provider,
                    Operation = "MenuPublish",
                    Status = channelItem.SyncStatus,
                    Message = channelItem.LastSyncMessage,
                    CreatedAt = DateTime.Now
                };
                await syncLogRepository.InsertAsync(log);
                logs.Add(log);
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            return logs.Select(MapSyncLog).ToList();
        }

        public async Task<RestaurantAggregatorOrderDto> IngestAggregatorOrder(IngestRestaurantAggregatorOrderDto input)
        {
            if (string.IsNullOrWhiteSpace(input.ExternalOrderId))
                throw new UserFriendlyException("External order id is required");
            if (input.Provider is not RestaurantChannelProvider.Foodmandu and not RestaurantChannelProvider.Pathao)
                throw new UserFriendlyException("Only Foodmandu and Pathao are supported in v1");

            var tenantId = AbpSession.GetTenantId();
            var existing = await aggregatorOrderRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Provider == input.Provider &&
                x.ExternalOrderId == input.ExternalOrderId);
            if (existing != null)
                return await MapAggregatorOrder(existing.Id);

            var channelId = input.ChannelId ?? await ResolveChannelId(input.Provider, tenantId);
            var order = new RestaurantAggregatorOrder
            {
                TenantId = tenantId,
                ChannelId = channelId,
                Provider = input.Provider,
                ExternalOrderId = input.ExternalOrderId.Trim(),
                CustomerName = input.CustomerName,
                CustomerPhoneNo = input.CustomerPhoneNo,
                DeliveryAddress = input.DeliveryAddress,
                ExpectedAmount = input.ExpectedAmount,
                CommissionAmount = input.CommissionAmount,
                RestaurantDiscountAmount = input.RestaurantDiscountAmount,
                DeliveryFeeAmount = input.DeliveryFeeAmount,
                PaidAmount = input.PaidAmount,
                RawPayloadJson = input.RawPayloadJson,
                Status = RestaurantExternalOrderStatus.Received,
                ReceivedAt = DateTime.Now
            };

            var id = await aggregatorOrderRepository.InsertAndGetIdAsync(order);
            await TouchAccountOrderSync(input.Provider, tenantId);
            await CurrentUnitOfWork.SaveChangesAsync();
            return await MapAggregatorOrder(id);
        }

        public async Task<RestaurantAggregatorOrderDto> AcceptAggregatorOrder(AcceptRestaurantAggregatorOrderDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var aggregatorOrder = await aggregatorOrderRepository.FirstOrDefaultAsync(x => x.Id == input.AggregatorOrderId && x.TenantId == tenantId);
            if (aggregatorOrder == null)
                throw new UserFriendlyException("Aggregator order not found");
            if (aggregatorOrder.Status is RestaurantExternalOrderStatus.Rejected or RestaurantExternalOrderStatus.Cancelled)
                throw new UserFriendlyException("Rejected or cancelled aggregator orders cannot be accepted");
            if (aggregatorOrder.OrderId.HasValue)
                return await MapAggregatorOrder(aggregatorOrder.Id);
            if (input.Lines == null || input.Lines.All(x => x.Qty <= 0))
                throw new UserFriendlyException("Accepted aggregator orders require mapped menu lines");

            var result = await customerOrderingAppService.CreateOrder(new CreateRestaurantCustomerOrderDto
            {
                TenantId = tenantId,
                OrderType = RestaurantOrderType.Delivery,
                CustomerName = aggregatorOrder.CustomerName,
                CustomerPhoneNo = string.IsNullOrWhiteSpace(aggregatorOrder.CustomerPhoneNo)
                    ? aggregatorOrder.ExternalOrderId
                    : aggregatorOrder.CustomerPhoneNo,
                DeliveryAddress = aggregatorOrder.DeliveryAddress,
                Notes = $"{aggregatorOrder.Provider} order {aggregatorOrder.ExternalOrderId}",
                PaymentMode = RestaurantCustomerPaymentMode.CounterSettlement,
                ClientRequestId = aggregatorOrder.Provider + "-" + aggregatorOrder.ExternalOrderId,
                Lines = input.Lines
            });

            aggregatorOrder.OrderId = result.OrderId;
            aggregatorOrder.Status = RestaurantExternalOrderStatus.Accepted;
            aggregatorOrder.AcceptedAt = DateTime.Now;
            aggregatorOrder.StatusMessage = "Accepted into restaurant POS";
            await aggregatorOrderRepository.UpdateAsync(aggregatorOrder);

            if (input.SendToKitchen)
            {
                var kitchenOrder = await orderAppService.GetOrder(result.OrderId);
                await orderAppService.SendToKitchen(new RestaurantOrderMutationDto
                {
                    OrderId = result.OrderId,
                    ClientRequestId = "aggregator-kitchen-" + aggregatorOrder.Id.ToString("N"),
                    ExpectedOrderVersion = kitchenOrder.RowVersion
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            return await MapAggregatorOrder(aggregatorOrder.Id);
        }

        public async Task UpdateAggregatorOrderStatus(UpdateRestaurantAggregatorOrderStatusDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var order = await aggregatorOrderRepository.FirstOrDefaultAsync(x => x.Id == input.AggregatorOrderId && x.TenantId == tenantId);
            if (order == null)
                throw new UserFriendlyException("Aggregator order not found");

            order.Status = input.Status;
            order.StatusMessage = input.Message;
            if (input.Status == RestaurantExternalOrderStatus.Cancelled)
                order.CancelledAt = DateTime.Now;
            if (input.Status == RestaurantExternalOrderStatus.Rejected)
                order.RejectedAt = DateTime.Now;
            await aggregatorOrderRepository.UpdateAsync(order);
        }

        public async Task<List<RestaurantAggregatorOrderDto>> GetAggregatorOrders(RestaurantReportFilterDto input)
        {
            input ??= new RestaurantReportFilterDto();
            var query = aggregatorOrderRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => !input.FromDate.HasValue || x.ReceivedAt.Date >= input.FromDate.Value.Date)
                .Where(x => !input.ToDate.HasValue || x.ReceivedAt.Date <= input.ToDate.Value.Date)
                .OrderByDescending(x => x.ReceivedAt)
                .Take(500);

            var rows = await query.Select(x => x.Id).ToListAsync();
            var result = new List<RestaurantAggregatorOrderDto>();
            foreach (var id in rows)
                result.Add(await MapAggregatorOrder(id));
            return result;
        }

        public async Task<RestaurantAggregatorPayoutDto> ImportPayout(ImportRestaurantAggregatorPayoutDto input)
        {
            if (input.Lines == null || input.Lines.Count == 0)
                throw new UserFriendlyException("Payout lines are required");

            var tenantId = AbpSession.GetTenantId();
            if (!string.IsNullOrWhiteSpace(input.ExternalPayoutId))
            {
                var existingPayout = await payoutRepository.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.Provider == input.Provider &&
                    x.ExternalPayoutId == input.ExternalPayoutId);
                if (existingPayout != null)
                    return await MapPayout(existingPayout.Id);
            }

            var payout = new RestaurantAggregatorPayout
            {
                TenantId = tenantId,
                ChannelId = input.ChannelId ?? await ResolveChannelId(input.Provider, tenantId),
                Provider = input.Provider,
                ExternalPayoutId = input.ExternalPayoutId,
                PeriodFrom = input.PeriodFrom,
                PeriodTo = input.PeriodTo,
                PaidAt = input.PaidAt,
                Notes = input.Notes,
                GrossAmount = input.Lines.Sum(x => x.ExpectedAmount),
                CommissionAmount = input.Lines.Sum(x => x.CommissionAmount),
                DeductionsAmount = input.Lines.Sum(x => x.RestaurantDiscountAmount + x.DeliveryFeeAmount),
                NetPaidAmount = input.Lines.Sum(x => x.PaidAmount)
            };
            var payoutId = await payoutRepository.InsertAndGetIdAsync(payout);

            foreach (var inputLine in input.Lines)
            {
                var matchedOrder = await aggregatorOrderRepository.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.Provider == input.Provider &&
                    x.ExternalOrderId == inputLine.ExternalOrderId);
                var expectedPaid = inputLine.ExpectedAmount - inputLine.CommissionAmount - inputLine.RestaurantDiscountAmount - inputLine.DeliveryFeeAmount;
                var discrepancy = Math.Abs(expectedPaid - inputLine.PaidAmount) > 0.01m;

                await payoutLineRepository.InsertAsync(new RestaurantAggregatorPayoutLine
                {
                    TenantId = tenantId,
                    PayoutId = payoutId,
                    AggregatorOrderId = matchedOrder?.Id,
                    ExternalOrderId = inputLine.ExternalOrderId,
                    ExpectedAmount = inputLine.ExpectedAmount,
                    CommissionAmount = inputLine.CommissionAmount,
                    RestaurantDiscountAmount = inputLine.RestaurantDiscountAmount,
                    DeliveryFeeAmount = inputLine.DeliveryFeeAmount,
                    PaidAmount = inputLine.PaidAmount,
                    MatchStatus = matchedOrder == null
                        ? RestaurantPayoutMatchStatus.Unmatched
                        : discrepancy
                            ? RestaurantPayoutMatchStatus.Discrepancy
                            : RestaurantPayoutMatchStatus.Matched,
                    MatchMessage = matchedOrder == null
                        ? "No matching aggregator order found"
                        : discrepancy
                            ? "Paid amount differs from expected settlement"
                            : "Matched"
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            return await MapPayout(payoutId);
        }

        public async Task<List<RestaurantAggregatorPayoutDto>> GetPayouts(RestaurantReportFilterDto input)
        {
            input ??= new RestaurantReportFilterDto();
            var payoutIds = await payoutRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => !input.FromDate.HasValue || x.PeriodTo.Date >= input.FromDate.Value.Date)
                .Where(x => !input.ToDate.HasValue || x.PeriodFrom.Date <= input.ToDate.Value.Date)
                .OrderByDescending(x => x.PeriodTo)
                .Select(x => x.Id)
                .Take(100)
                .ToListAsync();

            var result = new List<RestaurantAggregatorPayoutDto>();
            foreach (var payoutId in payoutIds)
                result.Add(await MapPayout(payoutId));
            return result;
        }

        private async Task<Guid?> ResolveChannelId(RestaurantChannelProvider provider, int tenantId)
        {
            return await channelRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.Provider == provider && !x.IsDeleted)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();
        }

        private async Task TouchAccountOrderSync(RestaurantChannelProvider provider, int tenantId)
        {
            var account = await accountRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Provider == provider &&
                !x.IsDeleted);
            if (account == null)
                return;

            account.LastOrderSyncAt = DateTime.Now;
            await accountRepository.UpdateAsync(account);
        }

        private async Task<RestaurantAggregatorOrderDto> MapAggregatorOrder(Guid id)
        {
            var order = await aggregatorOrderRepository.GetAll()
                .Include(x => x.ChannelFk)
                .Include(x => x.OrderFk)
                .FirstAsync(x => x.Id == id);

            return new RestaurantAggregatorOrderDto
            {
                Id = order.Id,
                ChannelId = order.ChannelId,
                ChannelName = order.ChannelFk?.Name,
                Provider = order.Provider,
                ExternalOrderId = order.ExternalOrderId,
                Status = order.Status,
                CustomerName = order.CustomerName,
                CustomerPhoneNo = order.CustomerPhoneNo,
                DeliveryAddress = order.DeliveryAddress,
                ExpectedAmount = order.ExpectedAmount,
                CommissionAmount = order.CommissionAmount,
                RestaurantDiscountAmount = order.RestaurantDiscountAmount,
                DeliveryFeeAmount = order.DeliveryFeeAmount,
                PaidAmount = order.PaidAmount,
                OrderId = order.OrderId,
                OrderNo = order.OrderFk?.OrderNo,
                ReceivedAt = order.ReceivedAt,
                AcceptedAt = order.AcceptedAt,
                StatusMessage = order.StatusMessage
            };
        }

        private async Task<RestaurantAggregatorPayoutDto> MapPayout(Guid payoutId)
        {
            var payout = await payoutRepository.GetAll()
                .Include(x => x.ChannelFk)
                .FirstAsync(x => x.Id == payoutId);
            var lines = await payoutLineRepository.GetAll()
                .Where(x => x.PayoutId == payoutId)
                .OrderBy(x => x.ExternalOrderId)
                .Select(x => new RestaurantAggregatorPayoutLineDto
                {
                    Id = x.Id,
                    PayoutId = x.PayoutId,
                    AggregatorOrderId = x.AggregatorOrderId,
                    ExternalOrderId = x.ExternalOrderId,
                    ExpectedAmount = x.ExpectedAmount,
                    CommissionAmount = x.CommissionAmount,
                    RestaurantDiscountAmount = x.RestaurantDiscountAmount,
                    DeliveryFeeAmount = x.DeliveryFeeAmount,
                    PaidAmount = x.PaidAmount,
                    MatchStatus = x.MatchStatus,
                    MatchMessage = x.MatchMessage
                })
                .ToListAsync();

            return new RestaurantAggregatorPayoutDto
            {
                Id = payout.Id,
                ChannelId = payout.ChannelId,
                ChannelName = payout.ChannelFk?.Name,
                Provider = payout.Provider,
                ExternalPayoutId = payout.ExternalPayoutId,
                PeriodFrom = payout.PeriodFrom,
                PeriodTo = payout.PeriodTo,
                PaidAt = payout.PaidAt,
                GrossAmount = payout.GrossAmount,
                CommissionAmount = payout.CommissionAmount,
                DeductionsAmount = payout.DeductionsAmount,
                NetPaidAmount = payout.NetPaidAmount,
                Notes = payout.Notes,
                Lines = lines
            };
        }

        private static RestaurantMenuSyncLogDto MapSyncLog(RestaurantMenuSyncLog log)
        {
            return new RestaurantMenuSyncLogDto
            {
                Id = log.Id,
                ChannelId = log.ChannelId,
                Provider = log.Provider,
                Operation = log.Operation,
                Status = log.Status,
                Message = log.Message,
                CreatedAt = log.CreatedAt,
                CompletedAt = log.CompletedAt
            };
        }

        private static decimal ApplyMarkup(decimal price, decimal markupPercent)
        {
            return markupPercent <= 0 ? price : Math.Round(price * (1 + markupPercent / 100), 2);
        }
    }
}
