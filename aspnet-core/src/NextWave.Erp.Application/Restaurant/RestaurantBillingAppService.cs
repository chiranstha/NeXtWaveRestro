using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Accounting;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Restaurant.Dtos;
using NextWave.Erp.Sales;
using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Transactions;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantBilling)]
    public class RestaurantBillingAppService(
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantBillLine, Guid> billLineRepository,
        IRepository<RestaurantBillPayment, Guid> billPaymentRepository,
        IRepository<RestaurantBillTender, Guid> billTenderRepository,
        IRepository<RestaurantCashShift, Guid> cashShiftRepository,
        IRepository<RestaurantTable, Guid> tableRepository,
        IRepository<RestaurantTableSession, Guid> tableSessionRepository,
        IRepository<RestaurantPrintJob, Guid> printJobRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        MaterialStockPostingService materialStockPostingService,
        RestaurantReorderService reorderService,
        IRestaurantOrderAppService restaurantOrderAppService,
        ISalesMastersAppService salesMastersAppService,
        IUnitOfWorkManager unitOfWorkManager)
        : ErpAppServiceBase, IRestaurantBillingAppService
    {
        public async Task<FinalizeRestaurantBillResultDto> GetBillStatus(string clientRequestId)
        {
            if (string.IsNullOrWhiteSpace(clientRequestId) || clientRequestId.Trim().Length > 100)
                throw new UserFriendlyException("A valid billing request ID is required");

            var tenantId = AbpSession.GetTenantId();
            var payment = await billPaymentRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ClientRequestId == clientRequestId.Trim());
            if (payment == null) return null;

            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == payment.OrderId && x.TenantId == tenantId);
            return new FinalizeRestaurantBillResultDto
            {
                OrderId = payment.OrderId,
                OrderNo = order?.OrderNo,
                SalesMasterId = payment.SalesMasterId,
                IsFullyBilled = payment.IsOrderFullyBilled,
                RemainingGrandTotal = payment.RemainingGrandTotal,
                BillAmount = payment.BillAmount,
                TipAmount = payment.TipAmount,
                PayableAmount = payment.PayableAmount,
                CustomerPaidAmount = payment.CustomerPaidAmount,
                ReturnAmount = payment.ReturnAmount
            };
        }

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
            if (input == null || input.OrderId == Guid.Empty)
                throw new UserFriendlyException("Restaurant order not found");
            if (string.IsNullOrWhiteSpace(input.ClientRequestId) || input.ClientRequestId.Trim().Length > 100)
                throw new UserFriendlyException("A unique billing request ID is required");

            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions
            {
                IsTransactional = true,
                IsolationLevel = IsolationLevel.Serializable
            });
            var tenantId = AbpSession.GetTenantId();

            var requestId = input.ClientRequestId.Trim();
            var requestHash = HashBillingRequest(input);
            var previousPayment = await billPaymentRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ClientRequestId == requestId);
            if (previousPayment != null)
            {
                if (!string.Equals(previousPayment.RequestHash, requestHash, StringComparison.Ordinal))
                    throw new UserFriendlyException("This billing request ID was already used for different bill details");
                var previousOrder = await orderRepository.FirstOrDefaultAsync(x =>
                    x.Id == previousPayment.OrderId && x.TenantId == tenantId);
                await uow.CompleteAsync();
                return new FinalizeRestaurantBillResultDto
                {
                    OrderId = previousPayment.OrderId,
                    OrderNo = previousOrder?.OrderNo,
                    SalesMasterId = previousPayment.SalesMasterId,
                    IsFullyBilled = previousPayment.IsOrderFullyBilled,
                    RemainingGrandTotal = previousPayment.RemainingGrandTotal,
                    BillAmount = previousPayment.BillAmount,
                    TipAmount = previousPayment.TipAmount,
                    PayableAmount = previousPayment.PayableAmount,
                    CustomerPaidAmount = previousPayment.CustomerPaidAmount,
                    ReturnAmount = previousPayment.ReturnAmount
                };
            }

            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.OrderId && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");
            var versionChecksEnabled = string.Equals(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantOrderVersionChecksEnabled, tenantId), "true", StringComparison.OrdinalIgnoreCase);
            if (versionChecksEnabled)
            {
                var currentVersion = order.RowVersion == null ? null : Convert.ToBase64String(order.RowVersion);
                if (string.IsNullOrWhiteSpace(input.ExpectedOrderVersion) ||
                    !string.Equals(input.ExpectedOrderVersion, currentVersion, StringComparison.Ordinal))
                {
                    var latest = await restaurantOrderAppService.GetOrderForPos(order.Id);
                    throw new UserFriendlyException(
                        "This order changed on another device. Reload the latest order before billing.",
                        JsonSerializer.Serialize(new { Code = "Restaurant.OrderConflict", LatestOrder = latest }));
                }
            }
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
            var tenders = input.Tenders?.ToList() ?? new List<RestaurantBillTenderDto>();
            var mixedTenderEnabled = string.Equals(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantMixedTenderEnabled, tenantId), "true", StringComparison.OrdinalIgnoreCase);
            if (tenders.Count > 1 && !mixedTenderEnabled)
                throw new UserFriendlyException("Mixed tender has not been enabled for this restaurant");
            var payment = BuildPaymentAmounts(grandTotal, input, tenders);

            if (tenders.Count > 0 && !input.CashShiftId.HasValue)
                throw new UserFriendlyException("Open a cashier shift before settling a restaurant bill");
            if (input.CashShiftId.HasValue)
            {
                var shift = await cashShiftRepository.FirstOrDefaultAsync(x =>
                    x.Id == input.CashShiftId.Value && x.TenantId == tenantId && !x.IsClosed &&
                    x.OpenedByUserId == AbpSession.UserId);
                if (shift == null)
                    throw new UserFriendlyException("Open cash shift not found for the signed-in cashier");
            }

            var cashLedger = await accountLedgerRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Name == "Cash");
            foreach (var tender in tenders)
            {
                if (tender.PaymentMethod == PaymentMethod.Cash)
                {
                    if (cashLedger == null)
                        throw new UserFriendlyException("Configure a Cash ledger before taking restaurant payment");
                    tender.PaymentLedgerId = cashLedger.Id;
                    continue;
                }

                if (tender.PaymentMethod is not PaymentMethod.Card_Swipe and not PaymentMethod.QR)
                    throw new UserFriendlyException("Restaurant settlement supports cash, card, and QR tenders");
                var ledgerSetting = tender.PaymentMethod == PaymentMethod.Card_Swipe
                    ? AppSettings.ErpSettings.RestaurantCardLedgerId
                    : AppSettings.ErpSettings.RestaurantQrLedgerId;
                var configuredLedger = await SettingManager.GetSettingValueForTenantAsync(ledgerSetting, tenantId);
                if (!Guid.TryParse(configuredLedger, out var configuredLedgerId) ||
                    configuredLedgerId != tender.PaymentLedgerId)
                    throw new UserFriendlyException($"Configure the {tender.PaymentMethod} settlement ledger before accepting that payment method");
                if (!tender.PaymentLedgerId.HasValue ||
                    await accountLedgerRepository.CountAsync(x =>
                        x.Id == tender.PaymentLedgerId.Value && x.TenantId == tenantId) == 0)
                    throw new UserFriendlyException("Select a valid tenant bank ledger for each card or QR payment");
            }

            Guid? tipLedgerId = null;
            if (payment.TipAmount > 0)
            {
                var configuredLedger = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantTipLedgerId, tenantId);
                if (!Guid.TryParse(configuredLedger, out var configuredId) ||
                    await accountLedgerRepository.CountAsync(x => x.Id == configuredId && x.TenantId == tenantId) == 0)
                    throw new UserFriendlyException("Configure a restaurant tip ledger before collecting tips");
                tipLedgerId = configuredId;
            }

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
                PaymentMethod = tenders.Count == 0 ? PaymentMethod.Credit : tenders[0].PaymentMethod,
                PaymentMethodLedgerId = tenders.Count == 0 ? null : tenders[0].PaymentLedgerId,
                PaymentAllocations = tenders.Select(x => new SalesPaymentAllocationDto
                {
                    PaymentMethod = x.PaymentMethod,
                    PaymentLedgerId = x.PaymentLedgerId,
                    Amount = x.Amount,
                    Reference = x.Reference
                }).ToList(),
                RestaurantTipAmount = payment.TipAmount,
                RestaurantTipLedgerId = tipLedgerId,
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

            var billPayment = new RestaurantBillPayment
            {
                TenantId = tenantId,
                OrderId = order.Id,
                SalesMasterId = salesMasterId,
                BillAmount = payment.BillAmount,
                TipAmount = payment.TipAmount,
                PayableAmount = payment.PayableAmount,
                CustomerPaidAmount = payment.CustomerPaidAmount,
                ReturnAmount = payment.ReturnAmount,
                PaymentMethod = tenders.Count == 0 ? PaymentMethod.Credit : tenders[0].PaymentMethod,
                CashShiftId = input.CashShiftId,
                ClientRequestId = requestId,
                RequestHash = requestHash,
                PaidAt = GetNepalNow()
            };
            var billPaymentId = await billPaymentRepository.InsertAndGetIdAsync(billPayment);

            foreach (var tender in tenders)
            {
                await billTenderRepository.InsertAsync(new RestaurantBillTender
                {
                    TenantId = tenantId,
                    BillPaymentId = billPaymentId,
                    CashShiftId = input.CashShiftId,
                    PaymentMethod = tender.PaymentMethod,
                    PaymentLedgerId = tender.PaymentLedgerId,
                    Amount = tender.Amount,
                    ReceivedAmount = tender.ReceivedAmount,
                    ChangeAmount = tender.PaymentMethod == PaymentMethod.Cash
                        ? Math.Max(0, tender.ReceivedAmount - tender.Amount) : 0,
                    Reference = tender.Reference?.Trim()
                });
            }

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
                    BilledAt = GetNepalNow()
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            var isFullyBilled = await IsOrderFullyBilled(order.Id, tenantId);
            billPayment.IsOrderFullyBilled = isFullyBilled;
            billPayment.RemainingGrandTotal = await GetRemainingGrandTotal(order.Id, tenantId);

            var printingEnabled = string.Equals(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantTicketPrintingEnabled, tenantId), "true", StringComparison.OrdinalIgnoreCase);
            if (printingEnabled)
            {
                var receiptRoute = (await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantReceiptPrintRouteName, tenantId))?.Trim();
                await printJobRepository.InsertAsync(new RestaurantPrintJob
                {
                    TenantId = tenantId,
                    ExternalJobId = "RECEIPT-" + billPaymentId.ToString("N"),
                    Type = RestaurantPrintJobType.BillReceipt,
                    Status = string.IsNullOrWhiteSpace(receiptRoute) ? RestaurantPrintJobStatus.Failed : RestaurantPrintJobStatus.Pending,
                    OrderId = order.Id,
                    RouteName = string.IsNullOrWhiteSpace(receiptRoute) ? "unconfigured" : receiptRoute,
                    Payload = BuildReceiptPayload(order, billableLines, payment, input, billPayment.PaidAt),
                    LastError = string.IsNullOrWhiteSpace(receiptRoute) ? "No receipt printer route is configured." : null,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (isFullyBilled)
            {
                order.SalesMasterId = salesMasterId;
                order.Status = RestaurantOrderStatus.Billed;
                order.BilledAt = GetNepalNow();
                await orderRepository.UpdateAsync(order);
            }

            // A bill settles an order. Staff close the table session after the party leaves,
            // which allows guests to place additional QR orders on the same session.

            var affectedProductIds = validation.Requirements
                .Select(x => x.ProductId)
                .Concat(billableLines.Select(x => x.Item.ProductId))
                .Distinct()
                .ToList();
            if (affectedProductIds.Count > 0)
                await reorderService.GenerateDraftPurchaseOrdersForProductsAsync(affectedProductIds, input.DateMiti);

            await billPaymentRepository.UpdateAsync(billPayment);

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

        private static byte[] BuildReceiptPayload(
            RestaurantOrder order,
            List<RestaurantBillableLine> lines,
            RestaurantBillPaymentAmounts payment,
            FinalizeRestaurantBillDto input,
            DateTime paidAt)
        {
            var text = new StringBuilder("\u001b@\u001ba\u0001");
            text.AppendLine("NEXTWAVE ERP");
            text.AppendLine("RESTAURANT RECEIPT");
            text.AppendLine(new string('-', 32));
            text.AppendLine($"Order: {order.OrderNo}");
            text.AppendLine($"Date: {paidAt:yyyy-MM-dd HH:mm}");
            if (!string.IsNullOrWhiteSpace(input.CustomerName ?? order.CustomerName))
                text.AppendLine($"Guest: {input.CustomerName ?? order.CustomerName}");
            text.AppendLine(new string('-', 32));
            foreach (var line in lines)
            {
                text.AppendLine($"{line.Qty:0.##} x {line.Item.ItemNameSnapshot}");
                text.AppendLine($"  {line.Amount:0.00}");
            }
            text.AppendLine(new string('-', 32));
            text.AppendLine($"Bill: {payment.BillAmount:0.00}");
            if (payment.TipAmount > 0) text.AppendLine($"Tip: {payment.TipAmount:0.00}");
            text.AppendLine($"Payable: {payment.PayableAmount:0.00}");
            text.AppendLine($"Paid: {payment.CustomerPaidAmount:0.00}");
            if (payment.ReturnAmount > 0) text.AppendLine($"Change: {payment.ReturnAmount:0.00}");
            text.AppendLine($"Method: {input.PaymentMethod}");
            text.AppendLine();
            text.AppendLine("Thank you");
            text.AppendLine();
            text.Append("\u001dV\u0000");
            return Encoding.UTF8.GetBytes(text.ToString());
        }

        private static RestaurantBillPaymentAmounts BuildPaymentAmounts(
            decimal billAmount,
            FinalizeRestaurantBillDto input,
            List<RestaurantBillTenderDto> tenders)
        {
            var tipAmount = input.TipAmount ?? 0;
            if (tipAmount < 0)
                throw new UserFriendlyException("Tip amount cannot be negative");

            if (input.CustomerPaidAmount.HasValue && input.CustomerPaidAmount.Value < 0)
                throw new UserFriendlyException("Customer paid amount cannot be negative");

            var payableAmount = billAmount + tipAmount;
            if (input.PaymentMethod == PaymentMethod.Credit && tenders.Count == 0)
                return new RestaurantBillPaymentAmounts
                {
                    BillAmount = billAmount,
                    TipAmount = 0,
                    PayableAmount = billAmount,
                    CustomerPaidAmount = 0,
                    ReturnAmount = 0
                };
            if (tenders.Count == 0)
                throw new UserFriendlyException("Choose at least one payment method");
            if (tenders.Count > 3)
                throw new UserFriendlyException("A bill can use up to three payment methods");
            if (tenders.Any(x => x.Amount <= 0))
                throw new UserFriendlyException("Every payment allocation must be greater than zero");
            if (tenders.Select(x => x.PaymentMethod).Distinct().Count() != tenders.Count)
                throw new UserFriendlyException("Choose each payment method only once per bill");
            if (tenders.Sum(x => x.Amount) != payableAmount)
                throw new UserFriendlyException("Payment allocations must equal the bill and tip total");

            foreach (var tender in tenders)
            {
                if (tender.ReceivedAmount <= 0)
                    tender.ReceivedAmount = tender.Amount;
                if (tender.PaymentMethod == PaymentMethod.Cash && tender.ReceivedAmount < tender.Amount)
                    throw new UserFriendlyException("Cash received must cover the cash allocation");
                if (tender.PaymentMethod != PaymentMethod.Cash && tender.ReceivedAmount != tender.Amount)
                    throw new UserFriendlyException("Card and QR payments must match their allocated amount");
            }

            var receivedAmount = tenders.Sum(x => x.ReceivedAmount);
            var cashChange = tenders
                .Where(x => x.PaymentMethod == PaymentMethod.Cash)
                .Sum(x => x.ReceivedAmount - x.Amount);
            if (receivedAmount - payableAmount != cashChange)
                throw new UserFriendlyException("Overpayment can only be returned as cash change");
            input.PaymentMethod = tenders[0].PaymentMethod;
            input.PaymentMethodLedgerId = tenders[0].PaymentLedgerId;

            return new RestaurantBillPaymentAmounts
            {
                BillAmount = billAmount,
                TipAmount = tipAmount,
                PayableAmount = payableAmount,
                CustomerPaidAmount = receivedAmount,
                ReturnAmount = cashChange
            };
        }

        private static string HashBillingRequest(FinalizeRestaurantBillDto input)
        {
            var request = JsonSerializer.Serialize(input);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request)));
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
