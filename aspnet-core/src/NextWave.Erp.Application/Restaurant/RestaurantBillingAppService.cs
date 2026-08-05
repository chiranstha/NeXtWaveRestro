using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Restaurant.Dtos;
using NextWave.Erp.Sales;
using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantBilling)]
    public class RestaurantBillingAppService(
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantBillLine, Guid> billLineRepository,
        IRepository<RestaurantBillPayment, Guid> billPaymentRepository,
        IRepository<RestaurantTable, Guid> tableRepository,
        IRepository<RestaurantTableSession, Guid> tableSessionRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        MaterialStockPostingService materialStockPostingService,
        RestaurantReorderService reorderService,
        ISalesMastersAppService salesMastersAppService,
        IUnitOfWorkManager unitOfWorkManager)
        : ErpAppServiceBase, IRestaurantBillingAppService
    {
        public async Task<RestaurantStockValidationDto> ValidateOrderStock(ValidateRestaurantBillStockDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");

            var billableLines = await BuildBillableLines(order, input.BillLines, tenantId);
            var requirements = await BuildMaterialRequirements(billableLines);
            return new RestaurantStockValidationDto
            {
                IsValid = requirements.All(x => x.IsAvailable),
                Requirements = requirements
            };
        }

        public async Task<FinalizeRestaurantBillResultDto> FinalizeBill(FinalizeRestaurantBillDto input)
        {
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var tenantId = AbpSession.GetTenantId();
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.OrderId && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");
            if (order.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed)
                throw new UserFriendlyException("Restaurant order is already billed");

            var billableLines = await BuildBillableLines(order, input.BillLines, tenantId);
            var validation = await ValidateOrderStock(new ValidateRestaurantBillStockDto
            {
                Id = order.Id,
                BillLines = input.BillLines ?? new List<FinalizeRestaurantBillLineDto>()
            });
            var stockWarnings = validation.Requirements.Where(x => !x.IsAvailable).ToList();
            if (stockWarnings.Count > 0)
            {
                var negativeStockStatus = await GetNegativeStockStatusAsync(tenantId);
                var missing = FormatStockWarnings(stockWarnings);

                if (IsNegativeStockBlocked(negativeStockStatus))
                    throw new UserFriendlyException("Insufficient stock", missing);

                // Warn mode: allow billing to proceed, warnings are returned in the result
            }

            if (billableLines.Count == 0)
                throw new UserFriendlyException("Cannot bill an empty restaurant order");

            foreach (var line in billableLines)
                line.SalesDetailId = Guid.NewGuid();

            var grossAmount = billableLines.Sum(x => x.GrossAmount);
            var discountAmount = billableLines.Sum(x => x.DiscountAmount);
            var taxAmount = billableLines.Sum(x => x.TaxAmount);
            var netAmount = billableLines.Sum(x => x.NetAmount);
            var grandTotal = billableLines.Sum(x => x.Amount);
            var payment = BuildPaymentAmounts(grandTotal, input);

            var salesInput = new CreateOrEditSalesMasterDto
            {
                VoucherNo = "",
                SalesAccountId = input.SalesAccountId,
                DateMiti = input.DateMiti,
                CreditPeriod = 0,
                CustomerName = string.IsNullOrWhiteSpace(input.CustomerName) ? order.CustomerName : input.CustomerName,
                CustomerAddress = input.CustomerAddress,
                CustomerVatNo = input.CustomerVatNo,
                CustomerPhoneNo = string.IsNullOrWhiteSpace(input.CustomerPhoneNo) ? order.CustomerPhoneNo : input.CustomerPhoneNo,
                Description = "Restaurant bill " + order.OrderNo,
                TaxAmount = taxAmount,
                BillDiscount = discountAmount,
                GrandTotal = grandTotal,
                NetAmount = netAmount,
                TotalAmount = grossAmount,
                TaxableAmount = billableLines.Where(x => x.TaxAmount > 0).Sum(x => x.NetAmount),
                IsPrint = input.IsPrint,
                SubTotalAmount = grossAmount,
                PaymentMethod = input.PaymentMethod,
                PaymentMethodLedgerId = input.PaymentMethodLedgerId,
                VatRefundAmount = 0,
                LrNo = "",
                PiNumber = "",
                VehicleNo = "",
                LedgerId = input.LedgerId,
                SalesModeType = SalesModeType.Na,
                AgainstId = new List<Guid> { order.Id },
                AgainstVoucherNo = order.OrderNo,
                InvoiceType = InvoiceTypeEnum.LocalInvoice,
                SourceModule = "Restaurant",
                SourceDocumentId = order.Id,
                SalesDetails = billableLines.Select(x => new SalesDetailDto
                {
                    Id = x.SalesDetailId,
                    GrossAmount = x.GrossAmount,
                    Qty = x.Qty,
                    Rate = x.Qty == 0 ? x.Item.Rate : x.GrossAmount / x.Qty,
                    TaxAmount = x.TaxAmount,
                    Discount = x.DiscountAmount,
                    DiscountPer = x.GrossAmount == 0
                        ? 0
                        : x.DiscountAmount * 100 / x.GrossAmount,
                    NetAmount = x.NetAmount,
                    Amount = x.Amount,
                    ProductType = x.Item.ProductFk.ProductType,
                    ProductId = x.Item.ProductId,
                    UnitId = x.Item.UnitId,
                    StockQty = 0,
                    TaxId = x.Item.TaxId,
                    ProductCode = x.Item.ProductFk.ProductCode
                }).ToList()
            };

            var salesMasterId = await salesMastersAppService.CreateOrEdit(salesInput);

            await billPaymentRepository.InsertAsync(new RestaurantBillPayment
            {
                TenantId = tenantId,
                OrderId = order.Id,
                SalesMasterId = salesMasterId,
                BillAmount = payment.BillAmount,
                TipAmount = payment.TipAmount,
                PayableAmount = payment.PayableAmount,
                CustomerPaidAmount = payment.CustomerPaidAmount,
                ReturnAmount = payment.ReturnAmount,
                PaidAt = DateTime.Now
            });

            foreach (var line in billableLines)
            {
                await billLineRepository.InsertAsync(new RestaurantBillLine
                {
                    TenantId = tenantId,
                    OrderId = order.Id,
                    OrderItemId = line.Item.Id,
                    SalesMasterId = salesMasterId,
                    SalesDetailId = line.SalesDetailId,
                    Qty = line.Qty,
                    GrossAmount = line.GrossAmount,
                    ModifierTotal = line.ModifierTotal,
                    DiscountAmount = line.DiscountAmount,
                    TaxAmount = line.TaxAmount,
                    NetAmount = line.NetAmount,
                    Amount = line.Amount,
                    BilledAt = DateTime.Now
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            var isFullyBilled = await IsOrderFullyBilled(order.Id, tenantId);

            if (isFullyBilled)
            {
                order.SalesMasterId = salesMasterId;
                order.Status = RestaurantOrderStatus.Billed;
                order.BilledAt = DateTime.Now;
                await orderRepository.UpdateAsync(order);
            }

            if (isFullyBilled && order.TableSessionId.HasValue)
            {
                var session = await tableSessionRepository.FirstOrDefaultAsync(order.TableSessionId.Value);
                if (session != null)
                {
                    session.Status = RestaurantOrderStatus.Closed;
                    session.ClosedAt = DateTime.Now;
                    await tableSessionRepository.UpdateAsync(session);
                }
            }

            if (isFullyBilled && order.TableId.HasValue)
            {
                var hasOpenOrder = await orderRepository.CountAsync(x =>
                    x.TenantId == tenantId &&
                    x.Id != order.Id &&
                    x.TableId == order.TableId &&
                    x.Status != RestaurantOrderStatus.Billed &&
                    x.Status != RestaurantOrderStatus.Closed &&
                    x.Status != RestaurantOrderStatus.Cancelled) > 0;

                if (!hasOpenOrder)
                {
                    var table = await tableRepository.FirstOrDefaultAsync(order.TableId.Value);
                    if (table != null)
                    {
                        table.Status = RestaurantTableStatus.Available;
                        await tableRepository.UpdateAsync(table);
                    }
                }
            }

            var affectedProductIds = validation.Requirements
                .Select(x => x.ProductId)
                .Concat(billableLines.Select(x => x.Item.ProductId))
                .Distinct()
                .ToList();
            if (affectedProductIds.Count > 0)
                await reorderService.GenerateDraftPurchaseOrdersForProductsAsync(affectedProductIds, input.DateMiti);

            await uow.CompleteAsync();
            var remainingGrandTotal = await GetRemainingGrandTotal(order.Id, tenantId);
            return new FinalizeRestaurantBillResultDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                SalesMasterId = salesMasterId,
                IsFullyBilled = isFullyBilled,
                RemainingGrandTotal = remainingGrandTotal,
                BillAmount = payment.BillAmount,
                TipAmount = payment.TipAmount,
                PayableAmount = payment.PayableAmount,
                CustomerPaidAmount = payment.CustomerPaidAmount,
                ReturnAmount = payment.ReturnAmount,
                StockWarnings = stockWarnings
            };
        }

        private static RestaurantBillPaymentAmounts BuildPaymentAmounts(decimal billAmount, FinalizeRestaurantBillDto input)
        {
            var tipAmount = input.TipAmount ?? 0;
            if (tipAmount < 0)
                throw new UserFriendlyException("Tip amount cannot be negative");

            if (input.CustomerPaidAmount.HasValue && input.CustomerPaidAmount.Value < 0)
                throw new UserFriendlyException("Customer paid amount cannot be negative");

            var payableAmount = billAmount + tipAmount;
            var customerPaidAmount = input.CustomerPaidAmount ?? 0;

            if (input.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR && !input.CustomerPaidAmount.HasValue)
                customerPaidAmount = payableAmount;

            if (input.PaymentMethod == PaymentMethod.Credit && !input.CustomerPaidAmount.HasValue)
                customerPaidAmount = 0;

            if (input.PaymentMethod is PaymentMethod.Cash or PaymentMethod.Card_Swipe or PaymentMethod.QR &&
                customerPaidAmount < payableAmount)
                throw new UserFriendlyException("Customer paid amount must be greater than or equal to payable amount");

            return new RestaurantBillPaymentAmounts
            {
                BillAmount = billAmount,
                TipAmount = tipAmount,
                PayableAmount = payableAmount,
                CustomerPaidAmount = customerPaidAmount,
                ReturnAmount = input.PaymentMethod == PaymentMethod.Credit
                    ? 0
                    : Math.Max(0, customerPaidAmount - payableAmount)
            };
        }

        private async Task<List<RestaurantBillableLine>> BuildBillableLines(
            RestaurantOrder order,
            List<FinalizeRestaurantBillLineDto> requestedBillLines,
            int tenantId)
        {
            var items = await orderItemRepository.GetAll()
                .Include(x => x.ProductFk)
                .Where(x => x.TenantId == tenantId &&
                            x.OrderId == order.Id &&
                            x.Status != RestaurantOrderItemStatus.Cancelled &&
                            x.Qty > 0)
                .ToListAsync();

            if (items.Count == 0)
                throw new UserFriendlyException("Cannot bill an empty restaurant order");

            if (requestedBillLines?.Any(x => x.OrderItemId == Guid.Empty || x.Qty <= 0) == true)
                throw new UserFriendlyException("Bill line quantity must be greater than zero");

            var itemIds = items.Select(x => x.Id).ToList();
            var billedRows = await billLineRepository.GetAll()
                .Where(x => x.TenantId == tenantId && itemIds.Contains(x.OrderItemId))
                .ToListAsync();

            var billedByItem = billedRows
                .GroupBy(x => x.OrderItemId)
                .ToDictionary(
                    x => x.Key,
                    x => new BilledLineAggregate
                    {
                        Qty = x.Sum(y => y.Qty),
                        GrossAmount = x.Sum(y => y.GrossAmount),
                        ModifierTotal = x.Sum(y => y.ModifierTotal),
                        DiscountAmount = x.Sum(y => y.DiscountAmount),
                        TaxAmount = x.Sum(y => y.TaxAmount),
                        NetAmount = x.Sum(y => y.NetAmount),
                        Amount = x.Sum(y => y.Amount)
                    });

            var requestedByItem = requestedBillLines?
                .GroupBy(x => x.OrderItemId)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Qty)) ?? new Dictionary<Guid, decimal>();
            var billAllRemaining = requestedByItem.Count == 0;

            var result = new List<RestaurantBillableLine>();
            foreach (var item in items)
            {
                billedByItem.TryGetValue(item.Id, out var billed);
                billed ??= new BilledLineAggregate();

                var unbilledQty = Math.Max(0, item.Qty - billed.Qty);
                if (unbilledQty <= 0)
                {
                    if (!billAllRemaining && requestedByItem.ContainsKey(item.Id))
                        throw new UserFriendlyException("Selected item is already fully billed");

                    continue;
                }

                if (billAllRemaining)
                {
                    result.Add(BuildBillableLine(item, unbilledQty, unbilledQty, billed));
                    continue;
                }

                if (!requestedByItem.TryGetValue(item.Id, out var requestedQty))
                    continue;

                if (requestedQty > unbilledQty)
                    throw new UserFriendlyException("Bill quantity cannot exceed unbilled quantity");

                result.Add(BuildBillableLine(item, requestedQty, unbilledQty, billed));
            }

            if (!billAllRemaining && requestedByItem.Keys.Any(id => items.All(item => item.Id != id)))
                throw new UserFriendlyException("One or more selected bill items were not found or cannot be billed");

            if (result.Count == 0)
                throw new UserFriendlyException("No unbilled items selected");

            return result;
        }

        private static RestaurantBillableLine BuildBillableLine(
            RestaurantOrderItem item,
            decimal qty,
            decimal unbilledQty,
            BilledLineAggregate billed)
        {
            var grossAmount = item.Qty * item.Rate + item.ModifierTotal;
            var isBillingRemainingItemQty = qty == unbilledQty;

            return new RestaurantBillableLine
            {
                Item = item,
                Qty = qty,
                GrossAmount = isBillingRemainingItemQty ? Math.Max(0, grossAmount - billed.GrossAmount) : Prorate(grossAmount, qty, item.Qty),
                ModifierTotal = isBillingRemainingItemQty ? Math.Max(0, item.ModifierTotal - billed.ModifierTotal) : Prorate(item.ModifierTotal, qty, item.Qty),
                DiscountAmount = isBillingRemainingItemQty ? Math.Max(0, item.DiscountAmount - billed.DiscountAmount) : Prorate(item.DiscountAmount, qty, item.Qty),
                TaxAmount = isBillingRemainingItemQty ? Math.Max(0, item.TaxAmount - billed.TaxAmount) : Prorate(item.TaxAmount, qty, item.Qty),
                NetAmount = isBillingRemainingItemQty ? Math.Max(0, item.NetAmount - billed.NetAmount) : Prorate(item.NetAmount, qty, item.Qty),
                Amount = isBillingRemainingItemQty ? Math.Max(0, item.Amount - billed.Amount) : Prorate(item.Amount, qty, item.Qty)
            };
        }

        private async Task<bool> IsOrderFullyBilled(Guid orderId, int tenantId)
        {
            var items = await orderItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId &&
                            x.OrderId == orderId &&
                            x.Status != RestaurantOrderItemStatus.Cancelled &&
                            x.Qty > 0)
                .ToListAsync();

            if (items.Count == 0)
                return false;

            var itemIds = items.Select(x => x.Id).ToList();
            var billedQtyByItem = await billLineRepository.GetAll()
                .Where(x => x.TenantId == tenantId && itemIds.Contains(x.OrderItemId))
                .GroupBy(x => x.OrderItemId)
                .Select(x => new { OrderItemId = x.Key, Qty = x.Sum(y => y.Qty) })
                .ToDictionaryAsync(x => x.OrderItemId, x => x.Qty);

            return items.All(x =>
            {
                billedQtyByItem.TryGetValue(x.Id, out var billedQty);
                return billedQty >= x.Qty;
            });
        }

        private async Task<decimal> GetRemainingGrandTotal(Guid orderId, int tenantId)
        {
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == orderId && x.TenantId == tenantId);
            if (order == null)
                return 0;

            var billedAmount = await billLineRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.OrderId == orderId)
                .Select(x => (decimal?)x.Amount)
                .SumAsync() ?? 0;

            return Math.Max(0, order.GrandTotal - billedAmount);
        }

        private static decimal Prorate(decimal amount, decimal qty, decimal totalQty)
        {
            return totalQty == 0 ? 0 : Math.Round(amount * qty / totalQty, 8, MidpointRounding.AwayFromZero);
        }

        private class RestaurantBillableLine
        {
            public RestaurantOrderItem Item { get; set; }
            public Guid SalesDetailId { get; set; }
            public decimal Qty { get; set; }
            public decimal GrossAmount { get; set; }
            public decimal ModifierTotal { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal NetAmount { get; set; }
            public decimal Amount { get; set; }
        }

        private class BilledLineAggregate
        {
            public decimal Qty { get; set; }
            public decimal GrossAmount { get; set; }
            public decimal ModifierTotal { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal NetAmount { get; set; }
            public decimal Amount { get; set; }
        }

        private class RestaurantBillPaymentAmounts
        {
            public decimal BillAmount { get; set; }
            public decimal TipAmount { get; set; }
            public decimal PayableAmount { get; set; }
            public decimal CustomerPaidAmount { get; set; }
            public decimal ReturnAmount { get; set; }
        }

        private async Task<List<RestaurantMaterialRequirementDto>> BuildMaterialRequirements(List<RestaurantBillableLine> billableLines)
        {
            var requirements = new List<RestaurantMaterialRequirementDto>();
            foreach (var line in billableLines)
            {
                var item = line.Item;
                var product = await productRepository.FirstOrDefaultAsync(item.ProductId);
                if (product == null || product.ProductType == ProductTypeEnum.Services)
                    continue;

                var recipe = await bomRepository.GetAll()
                    .Include(x => x.RawMaterialFk)
                    .Include(x => x.UnitFk)
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                x.ProductId == item.ProductId &&
                                !x.IsDeleted &&
                                x.IsActive)
                    .ToListAsync();

                if (recipe.Count == 0)
                {
                    // No BOM means this is a made-to-order item — raw material stock is
                    // not tracked at the finished-dish level, so skip the pre-check.
                    // ApplySalesIssueAsync will validate stock when the sales entry is posted.
                    continue;
                }

                var recipeProductQty = await ConvertQty(item.ProductId, item.UnitId, product.UnitId, line.Qty);
                foreach (var recipeLine in recipe)
                {
                    var requiredQty = recipeProductQty * recipeLine.Quantity * (1 + recipeLine.WastagePercentage / 100);
                    await AddRequirement(requirements, recipeLine.RawMaterialId, recipeLine.RawMaterialFk.Name, recipeLine.UnitId, requiredQty);
                }
            }

            foreach (var requirement in requirements)
            {
                requirement.AvailableQty = await materialStockPostingService.GetAvailableStockAsync(requirement.ProductId, requirement.UnitId);
                requirement.IsAvailable = requirement.AvailableQty >= requirement.RequiredQty;
            }

            return requirements;
        }

        private async Task AddRequirement(
            List<RestaurantMaterialRequirementDto> requirements,
            Guid productId,
            string productName,
            Guid unitId,
            decimal qty)
        {
            var existing = requirements.FirstOrDefault(x => x.ProductId == productId && x.UnitId == unitId);
            if (existing != null)
            {
                existing.RequiredQty += qty;
                return;
            }

            var unitName = await unitConversionRepository.GetAll()
                .Include(x => x.UnitFk)
                .Where(x => x.ProductId == productId && x.UnitId == unitId)
                .Select(x => x.UnitFk.Name)
                .FirstOrDefaultAsync();

            requirements.Add(new RestaurantMaterialRequirementDto
            {
                ProductId = productId,
                ProductName = productName,
                UnitId = unitId,
                UnitName = unitName ?? "",
                RequiredQty = qty
            });
        }

        private async Task<decimal> ConvertQty(Guid productId, Guid fromUnitId, Guid toUnitId, decimal qty)
        {
            if (fromUnitId == toUnitId)
                return qty;

            var conversions = await unitConversionRepository.GetAll()
                .Where(x => x.ProductId == productId)
                .ToListAsync();

            var from = conversions.FirstOrDefault(x => x.UnitId == fromUnitId);
            var to = conversions.FirstOrDefault(x => x.UnitId == toUnitId);
            if (from == null || to == null || from.Qty == 0 || to.PrimaryQty == 0)
                return qty;

            var primaryQty = qty * from.PrimaryQty / from.Qty;
            return primaryQty * to.Qty / to.PrimaryQty;
        }

        private async Task<string> GetNegativeStockStatusAsync(int tenantId)
        {
            return await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.NegativeStockStatus,
                tenantId);
        }

        private static bool IsNegativeStockBlocked(string status)
        {
            return string.Equals(status, "Block", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNegativeStockWarning(string status)
        {
            return string.IsNullOrWhiteSpace(status) ||
                   string.Equals(status, "Warn", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatStockWarnings(List<RestaurantMaterialRequirementDto> stockWarnings)
        {
            return string.Join(", ", stockWarnings.Select(x =>
                $"{x.ProductName} required {x.RequiredQty} {x.UnitName}, available {x.AvailableQty}"));
        }

    }
}
