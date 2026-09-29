using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Common;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Restaurant.Dtos;
using NextWave.Erp.Sales;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Transaction;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace NextWave.Erp.Restaurant
{
    public class RestaurantRefundAppService(
        IRepository<RestaurantRefund, Guid> refundRepository,
        IRepository<RestaurantRefundLine, Guid> refundLineRepository,
        IRepository<RestaurantRefundTender, Guid> refundTenderRepository,
        IRepository<RestaurantRefundSettlement, Guid> settlementRepository,
        IRepository<RestaurantRefundSettlementTender, Guid> settlementTenderRepository,
        IRepository<RestaurantClientOperation, Guid> operationRepository,
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantBillPayment, Guid> billPaymentRepository,
        IRepository<RestaurantBillTender, Guid> billTenderRepository,
        IRepository<RestaurantBillLine, Guid> billLineRepository,
        IRepository<SalesMaster, Guid> salesMasterRepository,
        IRepository<SalesDetail, Guid> salesDetailRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<RestaurantCashShift, Guid> cashShiftRepository,
        IRepository<RestaurantCashMovement, Guid> cashMovementRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Bom, Guid> bomRepository,
        ISalesReturnMastersAppService salesReturnMastersAppService,
        IPaymentMastersAppService paymentMastersAppService,
        StockManagementAppService stockManagementAppService,
        IUnitOfWorkManager unitOfWorkManager)
        : ErpAppServiceBase, IRestaurantRefundAppService
    {
        [AbpAuthorize(AppPermissions.PagesRestaurantRefundApprove)]
        public async Task<RestaurantRefundDto> Create(CreateRestaurantRefundDto input)
        {
            ValidateRequestId(input?.ClientRequestId);
            if (input.SalesMasterId == Guid.Empty || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 500)
                throw new UserFriendlyException("Choose an invoice and enter a refund reason");
            if (input.TipRefundAmount < 0)
                throw new UserFriendlyException("Tip refund cannot be negative");

            var tenantId = AbpSession.GetTenantId();
            var requestId = input.ClientRequestId.Trim();
            var requestHash = Hash(new
            {
                input.SalesMasterId,
                Reason = input.Reason.Trim(),
                input.TipRefundAmount,
                Lines = (input.Lines ?? new List<CreateRestaurantRefundLineDto>()).OrderBy(x => x.SalesDetailId)
                    .Select(x => new { x.SalesDetailId, x.Quantity, x.RestockQuantity, x.ReturnedUnopenedPackagedItem })
            });

            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions
            {
                IsTransactional = true,
                IsolationLevel = IsolationLevel.Serializable
            });

            var existing = await refundRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ClientRequestId == requestId);
            if (existing != null)
            {
                EnsureSameRequest(existing.RequestHash, requestHash);
                var priorResult = await Map(existing);
                await uow.CompleteAsync();
                return priorResult;
            }

            await VerifyManagerPin(input.ManagerPin, tenantId);
            if (!await IsEnabled(AppSettings.ErpSettings.RestaurantRefundsEnabled, tenantId))
                throw new UserFriendlyException("Restaurant refunds are not enabled for this account");

            var sales = await salesMasterRepository.FirstOrDefaultAsync(x =>
                x.Id == input.SalesMasterId && x.TenantId == tenantId && x.SourceModule == "Restaurant");
            if (sales == null)
                throw new UserFriendlyException("Original restaurant invoice not found");
            var order = await orderRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.SalesMasterId == sales.Id);
            if (order == null)
                throw new UserFriendlyException("Restaurant order linked to this invoice was not found");

            var clearSetting = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantRefundPayableLedgerId, tenantId);
            if (!Guid.TryParse(clearSetting, out var clearingLedgerId) ||
                await accountLedgerRepository.CountAsync(x => x.TenantId == tenantId && x.Id == clearingLedgerId) == 0)
                throw new UserFriendlyException("Configure a tenant refund clearing ledger before issuing refunds");

            var sourceDetails = await salesDetailRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.SalesMasterId == sales.Id)
                .ToListAsync();
            var sourceById = sourceDetails.ToDictionary(x => x.Id);
            var requestLines = (input.Lines ?? new List<CreateRestaurantRefundLineDto>())
                .Where(x => x.Quantity > 0).ToList();
            if (requestLines.Select(x => x.SalesDetailId).Distinct().Count() != requestLines.Count)
                throw new UserFriendlyException("A refund line can only be selected once");

            var previousLines = await refundLineRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.RefundFk.SalesMasterId == sales.Id &&
                            x.RefundFk.Status != RestaurantRefundStatus.Cancelled)
                .ToListAsync();
            var billLines = await billLineRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.SalesMasterId == sales.Id)
                .ToListAsync();
            var orderItemBySaleDetail = billLines.GroupBy(x => x.SalesDetailId)
                .ToDictionary(x => x.Key, x => x.First().OrderItemId);
            var linesToSave = new List<RestaurantRefundLine>();
            decimal itemRefund = 0;

            foreach (var requestLine in requestLines)
            {
                if (!sourceById.TryGetValue(requestLine.SalesDetailId, out var detail) || detail.Qty <= 0)
                    throw new UserFriendlyException("A selected item does not belong to the original invoice");
                var alreadyRefundedQty = previousLines.Where(x => x.SalesDetailId == detail.Id).Sum(x => x.Qty);
                if (requestLine.Quantity > detail.Qty - alreadyRefundedQty)
                    throw new UserFriendlyException($"Refund quantity exceeds the remaining quantity for {detail.ProductName}");
                if (requestLine.RestockQuantity < 0 || requestLine.RestockQuantity > requestLine.Quantity)
                    throw new UserFriendlyException("Restock quantity must be between zero and the refund quantity");

                if (requestLine.RestockQuantity > 0)
                {
                    if (!requestLine.ReturnedUnopenedPackagedItem)
                        throw new UserFriendlyException("Only an explicitly confirmed, unopened packaged item may be restocked");
                    var product = await productRepository.FirstOrDefaultAsync(x => x.Id == detail.ProductId && x.TenantId == tenantId);
                    if (product == null || product.ProductType == ProductTypeEnum.Services ||
                        await bomRepository.CountAsync(x => x.TenantId == tenantId && x.ProductId == detail.ProductId && !x.IsDeleted && x.IsActive) > 0)
                        throw new UserFriendlyException("Prepared items and recipe products cannot be restocked from a restaurant refund");
                }

                var priorValue = previousLines.Where(x => x.SalesDetailId == detail.Id).Sum(x => x.NetAmount + x.TaxAmount);
                var lineGross = Paisa(detail.GrossAmount * requestLine.Quantity / detail.Qty);
                var lineDiscount = Paisa(detail.Discount * requestLine.Quantity / detail.Qty);
                var lineTax = Paisa(detail.TaxAmount * requestLine.Quantity / detail.Qty);
                var lineNet = Paisa(lineGross - lineDiscount);
                var lineTotal = Paisa(lineNet + lineTax);
                if (priorValue + lineTotal > detail.Amount + 0.01m)
                    throw new UserFriendlyException("Refund value exceeds the remaining value for a selected invoice line");

                itemRefund += lineTotal;
                linesToSave.Add(new RestaurantRefundLine
                {
                    TenantId = tenantId,
                    SalesDetailId = detail.Id,
                    OrderItemId = orderItemBySaleDetail.GetValueOrDefault(detail.Id),
                    Qty = requestLine.Quantity,
                    GrossAmount = lineGross,
                    DiscountAmount = lineDiscount,
                    TaxAmount = lineTax,
                    NetAmount = lineNet,
                    RestockQty = requestLine.RestockQuantity,
                    StockDisposition = requestLine.RestockQuantity > 0
                        ? RestaurantRefundStockDisposition.RestockSealedPackagedItem
                        : RestaurantRefundStockDisposition.Discard
                });
            }

            var sourcePayments = await billPaymentRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.SalesMasterId == sales.Id)
                .ToListAsync();
            var priorTips = await refundRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.SalesMasterId == sales.Id && x.Status != RestaurantRefundStatus.Cancelled)
                .SumAsync(x => (decimal?)x.TipRefundAmount) ?? 0;
            var availableTip = sourcePayments.Sum(x => x.TipAmount) - priorTips;
            if (input.TipRefundAmount > availableTip)
                throw new UserFriendlyException("Tip refund exceeds the remaining tip collected on this invoice");

            var totalRefund = Paisa(itemRefund + input.TipRefundAmount);
            if (totalRefund <= 0)
                throw new UserFriendlyException("Enter at least one item or tip amount to refund");

            var tenderSources = await GetTenderSources(tenantId, sales, sourcePayments);
            var previousTenders = await refundTenderRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.RefundFk.SalesMasterId == sales.Id &&
                            x.RefundFk.Status != RestaurantRefundStatus.Cancelled)
                .ToListAsync();
            foreach (var source in tenderSources)
            {
                source.Remaining = Math.Max(0, source.Amount - previousTenders
                    .Where(x => (source.TenderId.HasValue && x.OriginalTenderId == source.TenderId) ||
                                (!source.TenderId.HasValue && x.OriginalBillPaymentId == source.BillPaymentId))
                    .Sum(x => x.AllocatedAmount));
            }
            var payoutAmount = Math.Min(totalRefund, tenderSources.Sum(x => x.Remaining));
            payoutAmount = Paisa(payoutAmount);
            var creditNoteAmount = Paisa(totalRefund - payoutAmount);

            var refund = new RestaurantRefund
            {
                TenantId = tenantId,
                RestaurantOrderId = order.Id,
                SalesMasterId = sales.Id,
                ClientRequestId = requestId,
                RequestHash = requestHash,
                Reason = input.Reason.Trim(),
                ItemRefundAmount = itemRefund,
                TipRefundAmount = input.TipRefundAmount,
                CreditNoteAmount = creditNoteAmount,
                PayoutAmount = payoutAmount,
                Status = payoutAmount > 0 ? RestaurantRefundStatus.PendingSettlement : RestaurantRefundStatus.CreditApplied,
                ApprovedByUserId = AbpSession.GetUserId(),
                ApprovedAt = GetNepalNow(),
                CreatedAt = GetNepalNow()
            };
            refund.Id = await refundRepository.InsertAndGetIdAsync(refund);

            foreach (var line in linesToSave)
            {
                line.RefundId = refund.Id;
                await refundLineRepository.InsertAsync(line);
                if (line.RestockQty > 0)
                {
                    var detail = sourceById[line.SalesDetailId];
                    await stockManagementAppService.MaintainStock(new StockMaintainDto
                    {
                        DateMiti = DateConverter.ConvertToNepali(GetNepalNow()),
                        ProductId = detail.ProductId,
                        UnitId = detail.UnitId,
                        Qty = -line.RestockQty,
                        Rate = detail.Rate,
                        Type = StockMaintainTypeEnum.Inward,
                        FinancialYearId = FinancialYearId
                    });
                }
            }

            if (itemRefund > 0)
            {
                var itemReturn = BuildReturn(sales, clearingLedgerId, itemRefund, linesToSave.Select(x =>
                {
                    var source = sourceById[x.SalesDetailId];
                    return new SalesReturnDetailDto
                    {
                        Qty = x.Qty,
                        Rate = source.Rate,
                        GrossAmount = x.GrossAmount,
                        Discount = x.DiscountAmount,
                        DiscountPer = x.GrossAmount == 0 ? 0 : x.DiscountAmount * 100 / x.GrossAmount,
                        TaxAmount = x.TaxAmount,
                        NetAmount = x.NetAmount,
                        Amount = x.NetAmount + x.TaxAmount,
                        ProductId = source.ProductId,
                        UnitId = source.UnitId,
                        TaxId = source.TaxId,
                        SalesDetailId = source.Id
                    };
                }).ToList(), "Restaurant refund " + requestId);
                refund.SalesReturnMasterId = await salesReturnMastersAppService.CreateRestaurantRefundReturn(itemReturn);
            }

            if (input.TipRefundAmount > 0)
            {
                var tipSetting = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantTipLedgerId, tenantId);
                if (!Guid.TryParse(tipSetting, out var tipLedgerId) ||
                    await accountLedgerRepository.CountAsync(x => x.TenantId == tenantId && x.Id == tipLedgerId) == 0)
                    throw new UserFriendlyException("Configure the restaurant tip ledger before refunding tips");
                refund.TipSalesReturnMasterId = await salesReturnMastersAppService.CreateRestaurantRefundReturn(
                    BuildReturn(sales, clearingLedgerId, input.TipRefundAmount, new List<SalesReturnDetailDto>(), "Restaurant tip adjustment " + requestId, tipLedgerId));
            }

            if (creditNoteAmount > 0)
            {
                if (!sales.LedgerId.HasValue ||
                    await accountLedgerRepository.CountAsync(x => x.TenantId == tenantId && x.Id == sales.LedgerId.Value) == 0)
                    throw new UserFriendlyException("The original invoice has no valid customer or receivable ledger for a credit note");
                var creditNote = BuildReturn(sales, sales.LedgerId.Value, creditNoteAmount,
                    new List<SalesReturnDetailDto>(), "Restaurant refund credit note " + requestId, clearingLedgerId);
                refund.CreditNoteSalesReturnMasterId = await salesReturnMastersAppService.CreateRestaurantRefundReturn(creditNote);
            }

            var allocations = AllocateTenderPayout(tenderSources, payoutAmount);
            foreach (var allocation in allocations)
            {
                await refundTenderRepository.InsertAsync(new RestaurantRefundTender
                {
                    TenantId = tenantId,
                    RefundId = refund.Id,
                    OriginalTenderId = allocation.Source.TenderId,
                    OriginalBillPaymentId = allocation.Source.BillPaymentId,
                    PaymentMethod = allocation.Source.PaymentMethod,
                    PaymentLedgerId = allocation.Source.PaymentLedgerId,
                    AllocatedAmount = allocation.Amount
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            await uow.CompleteAsync();
            return await Map(refund);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantRefundSettle)]
        public async Task<RestaurantRefundDto> Settle(SettleRestaurantRefundDto input)
        {
            ValidateRequestId(input?.ClientRequestId);
            if (input.RefundId == Guid.Empty)
                throw new UserFriendlyException("Select a refund to settle");
            var tenantId = AbpSession.GetTenantId();
            var requestId = input.ClientRequestId.Trim();
            var payouts = (input.Payouts ?? new List<RestaurantRefundPayoutDto>()).OrderBy(x => x.RefundTenderId).ToList();
            var requestHash = Hash(new
            {
                input.RefundId,
                input.CashShiftId,
                Payouts = payouts.Select(x => new { x.RefundTenderId, x.Amount, Reference = x.Reference?.Trim() })
            });

            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions
            {
                IsTransactional = true,
                IsolationLevel = IsolationLevel.Serializable
            });
            var previous = await settlementRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ClientRequestId == requestId);
            if (previous != null)
            {
                EnsureSameRequest(previous.RequestHash, requestHash);
                var previousRefund = await refundRepository.GetAsync(previous.RefundId);
                var previousResult = await Map(previousRefund);
                await uow.CompleteAsync();
                return previousResult;
            }

            var refund = await refundRepository.FirstOrDefaultAsync(x => x.Id == input.RefundId && x.TenantId == tenantId);
            if (refund == null) throw new UserFriendlyException("Restaurant refund not found");
            if (refund.Status is RestaurantRefundStatus.Settled or RestaurantRefundStatus.Cancelled or RestaurantRefundStatus.CreditApplied)
                throw new UserFriendlyException("This refund has no outstanding payout");
            if (payouts.Count == 0 || payouts.Any(x => x.Amount <= 0) || payouts.Select(x => x.RefundTenderId).Distinct().Count() != payouts.Count)
                throw new UserFriendlyException("Enter valid refund payout amounts");

            var refundTenders = await refundTenderRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.RefundId == refund.Id).ToListAsync();
            if (payouts.Any(x => refundTenders.All(t => t.Id != x.RefundTenderId)))
                throw new UserFriendlyException("A payout tender does not belong to this refund");
            foreach (var payout in payouts)
            {
                var tender = refundTenders.Single(x => x.Id == payout.RefundTenderId);
                if (tender.SettledAmount + payout.Amount > tender.AllocatedAmount)
                    throw new UserFriendlyException("Payout amount exceeds the approved amount for a tender");
                if (tender.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR && string.IsNullOrWhiteSpace(payout.Reference))
                    throw new UserFriendlyException("Enter the cashier-verified card or QR settlement reference");
                if (tender.PaymentMethod == PaymentMethod.Credit)
                    throw new UserFriendlyException("Credit-note amounts are not paid out through the cashier");
            }

            var settlementAmount = Paisa(payouts.Sum(x => x.Amount));
            if (refund.SettledAmount + settlementAmount > refund.PayoutAmount)
                throw new UserFriendlyException("Payout exceeds the amount collected on the original invoice");

            RestaurantCashShift cashShift = null;
            var cashAmount = payouts.Where(p => refundTenders.Any(t => t.Id == p.RefundTenderId && t.PaymentMethod == PaymentMethod.Cash))
                .Sum(p => p.Amount);
            if (cashAmount > 0)
            {
                if (!input.CashShiftId.HasValue)
                    throw new UserFriendlyException("Open a cashier shift before paying a cash refund");
                cashShift = await cashShiftRepository.FirstOrDefaultAsync(x =>
                    x.Id == input.CashShiftId.Value && x.TenantId == tenantId && !x.IsClosed &&
                    x.OpenedByUserId == AbpSession.GetUserId());
                if (cashShift == null)
                    throw new UserFriendlyException("Open cashier shift not found for the signed-in cashier");
            }

            var clearingSetting = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantRefundPayableLedgerId, tenantId);
            if (!Guid.TryParse(clearingSetting, out var clearingLedgerId) ||
                await accountLedgerRepository.CountAsync(x => x.TenantId == tenantId && x.Id == clearingLedgerId) == 0)
                throw new UserFriendlyException("Refund clearing ledger is no longer configured");

            var settlement = new RestaurantRefundSettlement
            {
                TenantId = tenantId,
                RefundId = refund.Id,
                ClientRequestId = requestId,
                RequestHash = requestHash,
                Amount = settlementAmount,
                SettledByUserId = AbpSession.GetUserId(),
                SettledAt = GetNepalNow(),
                CashShiftId = cashShift?.Id
            };
            settlement.Id = await settlementRepository.InsertAndGetIdAsync(settlement);

            var dateMiti = DateConverter.ConvertToNepali(GetNepalNow());
            foreach (var payout in payouts)
            {
                var tender = refundTenders.Single(x => x.Id == payout.RefundTenderId);
                var paymentMasterId = await paymentMastersAppService.CreateRestaurantRefundPayment(new CreateOrEditPaymentMasterDto
                {
                    TotalAmount = payout.Amount,
                    Description = "Restaurant refund " + refund.Id.ToString("N"),
                    DateMiti = dateMiti,
                    LedgerId = tender.PaymentLedgerId,
                    PaymentDetails = new List<CreateOrEditReceiptDetailDto>
                    {
                        new() { Amount = payout.Amount, LedgerId = clearingLedgerId }
                    }
                });
                tender.SettledAmount += payout.Amount;
                await refundTenderRepository.UpdateAsync(tender);
                await settlementTenderRepository.InsertAsync(new RestaurantRefundSettlementTender
                {
                    TenantId = tenantId,
                    SettlementId = settlement.Id,
                    RefundTenderId = tender.Id,
                    Amount = payout.Amount,
                    Reference = payout.Reference?.Trim(),
                    PaymentMasterId = paymentMasterId
                });
            }

            if (cashAmount > 0)
                await cashMovementRepository.InsertAsync(new RestaurantCashMovement
                {
                    TenantId = tenantId,
                    CashShiftId = cashShift.Id,
                    IsCashIn = false,
                    Amount = cashAmount,
                    Reason = "Restaurant refund " + refund.Id.ToString("N"),
                    CreatedByUserId = AbpSession.GetUserId(),
                    CreatedAt = GetNepalNow()
                });

            refund.SettledAmount += settlementAmount;
            refund.SettledAt = GetNepalNow();
            refund.Status = refund.SettledAmount >= refund.PayoutAmount
                ? RestaurantRefundStatus.Settled
                : RestaurantRefundStatus.PartiallySettled;
            await refundRepository.UpdateAsync(refund);
            await CurrentUnitOfWork.SaveChangesAsync();
            await uow.CompleteAsync();
            return await Map(refund);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantBilling)]
        public async Task<RestaurantRefundDto> Get(EntityDto<Guid> input)
        {
            var refund = await refundRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == AbpSession.TenantId);
            if (refund == null) throw new UserFriendlyException("Restaurant refund not found");
            return await Map(refund);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantBilling)]
        public async Task<RestaurantOperationStatusDto> GetOperationStatus(string operationType, string clientRequestId)
        {
            ValidateRequestId(clientRequestId);
            var tenantId = AbpSession.GetTenantId();
            var requestId = clientRequestId.Trim();
            if (string.Equals(operationType, "RefundApproval", StringComparison.OrdinalIgnoreCase))
            {
                var refund = await refundRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientRequestId == requestId);
                return new RestaurantOperationStatusDto
                {
                    OperationType = "RefundApproval",
                    ClientRequestId = requestId,
                    Status = refund == null ? "NotFound" : refund.Status.ToString(),
                    EntityId = refund?.Id,
                    ResultJson = refund == null ? null : JsonSerializer.Serialize(await Map(refund))
                };
            }

            if (string.Equals(operationType, "RefundSettlement", StringComparison.OrdinalIgnoreCase))
            {
                var settlement = await settlementRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientRequestId == requestId);
                var refund = settlement == null ? null : await refundRepository.FirstOrDefaultAsync(x =>
                    x.Id == settlement.RefundId && x.TenantId == tenantId);
                return new RestaurantOperationStatusDto
                {
                    OperationType = "RefundSettlement",
                    ClientRequestId = requestId,
                    Status = settlement == null ? "NotFound" : refund?.Status.ToString() ?? "Unknown",
                    EntityId = refund?.Id,
                    ResultJson = refund == null ? null : JsonSerializer.Serialize(await Map(refund))
                };
            }

            var operation = await operationRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.UserId == AbpSession.GetUserId() &&
                x.ClientRequestId == requestId && x.OperationType == operationType);
            return new RestaurantOperationStatusDto
            {
                OperationType = operationType,
                ClientRequestId = requestId,
                Status = operation == null ? "NotFound" : "Completed",
                EntityId = operation?.EntityId,
                ResultJson = operation?.ResultJson
            };
        }

        private async Task<List<TenderSource>> GetTenderSources(int tenantId, SalesMaster sales, List<RestaurantBillPayment> payments)
        {
            var paymentIds = payments.Select(x => x.Id).ToList();
            var tenderRows = await billTenderRepository.GetAll()
                .Where(x => x.TenantId == tenantId && paymentIds.Contains(x.BillPaymentId))
                .ToListAsync();
            var result = tenderRows.Where(x => x.Amount > 0 && x.PaymentLedgerId.HasValue)
                .Select(x => new TenderSource(x.Id, x.BillPaymentId, x.PaymentMethod, x.PaymentLedgerId.Value, x.Amount))
                .ToList();

            foreach (var payment in payments.Where(p => tenderRows.All(t => t.BillPaymentId != p.Id) && p.PayableAmount > 0))
            {
                var method = payment.PaymentMethod;
                var ledgerId = sales.PaymentMethodLedgerId;
                if (method == PaymentMethod.Cash)
                {
                    var cash = await accountLedgerRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "Cash");
                    ledgerId = cash?.Id;
                }
                if (ledgerId.HasValue)
                    result.Add(new TenderSource(null, payment.Id, method, ledgerId.Value, payment.PayableAmount));
            }
            return result;
        }

        private static List<TenderAllocation> AllocateTenderPayout(List<TenderSource> sources, decimal amount)
        {
            var usable = sources.Where(x => x.Remaining > 0).OrderBy(x => x.PaymentMethod).ThenBy(x => x.TenderId ?? x.BillPaymentId).ToList();
            if (amount == 0) return new List<TenderAllocation>();
            if (usable.Sum(x => x.Remaining) < amount)
                throw new UserFriendlyException("Refund payout exceeds original tender amounts");
            var allocations = new List<TenderAllocation>();
            decimal assigned = 0;
            for (var i = 0; i < usable.Count; i++)
            {
                var source = usable[i];
                var allocation = i == usable.Count - 1
                    ? amount - assigned
                    : Math.Min(source.Remaining, Paisa(amount * source.Remaining / usable.Sum(x => x.Remaining)));
                if (allocation > 0)
                {
                    allocations.Add(new TenderAllocation(source, allocation));
                    assigned += allocation;
                }
            }

            // Rounding leftovers are assigned deterministically to the last tender with room.
            var residual = amount - assigned;
            for (var i = allocations.Count - 1; residual > 0 && i >= 0; i--)
            {
                var row = allocations[i];
                var room = row.Source.Remaining - row.Amount;
                var extra = Math.Min(room, residual);
                allocations[i] = row with { Amount = row.Amount + extra };
                residual -= extra;
            }
            if (residual != 0)
                throw new UserFriendlyException("Unable to reconcile refund payout rounding");
            return allocations;
        }

        private CreateOrEditSalesReturnMasterDto BuildReturn(
            SalesMaster source,
            Guid ledgerId,
            decimal amount,
            List<SalesReturnDetailDto> details,
            string description,
            Guid? salesAccountIdOverride = null)
        {
            var total = Paisa(amount);
            var gross = Paisa(details.Sum(x => x.GrossAmount ?? 0));
            var discount = Paisa(details.Sum(x => x.Discount ?? 0));
            var tax = Paisa(details.Sum(x => x.TaxAmount ?? 0));
            return new CreateOrEditSalesReturnMasterDto
            {
                VoucherNo = "",
                SalesAccountId = salesAccountIdOverride ?? source.SalesAccountId,
                Description = description,
                TotalAmount = gross > 0 ? gross : total,
                BillDiscount = discount,
                TaxAmount = tax,
                ValueAddedTax = tax,
                GrandTotal = total,
                DateMiti = DateConverter.ConvertToNepali(GetNepalNow()),
                LedgerId = ledgerId,
                DebitOrCreditNote = false,
                SalesMasterId = source.Id,
                ReturnType = ReturnType.RateDifference,
                ReturnTaxId = Guid.Empty,
                ReturnAmount = total,
                ReturnTaxAmount = tax,
                TaxableAmount = Math.Max(0, total - tax),
                InvoiceType = source.InvoiceType,
                SalesReturnDetail = details,
                NetAmount = total
            };
        }

        private async Task<RestaurantRefundDto> Map(RestaurantRefund refund)
        {
            var tenders = await refundTenderRepository.GetAll()
                .Where(x => x.TenantId == refund.TenantId && x.RefundId == refund.Id)
                .OrderBy(x => x.PaymentMethod)
                .Select(x => new RestaurantRefundTenderDto
                {
                    Id = x.Id,
                    PaymentMethod = x.PaymentMethod,
                    PaymentLedgerId = x.PaymentLedgerId,
                    AllocatedAmount = x.AllocatedAmount,
                    SettledAmount = x.SettledAmount
                }).ToListAsync();
            return new RestaurantRefundDto
            {
                Id = refund.Id,
                SalesMasterId = refund.SalesMasterId,
                SalesReturnMasterId = refund.SalesReturnMasterId,
                TipSalesReturnMasterId = refund.TipSalesReturnMasterId,
                CreditNoteSalesReturnMasterId = refund.CreditNoteSalesReturnMasterId,
                Status = refund.Status.ToString(),
                Reason = refund.Reason,
                ItemRefundAmount = refund.ItemRefundAmount,
                TipRefundAmount = refund.TipRefundAmount,
                CreditNoteAmount = refund.CreditNoteAmount,
                PayoutAmount = refund.PayoutAmount,
                SettledAmount = refund.SettledAmount,
                ApprovedAt = refund.ApprovedAt,
                SettledAt = refund.SettledAt,
                Tenders = tenders
            };
        }

        private async Task VerifyManagerPin(string suppliedPin, int tenantId)
        {
            var storedPin = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantManagerPin, tenantId);
            if (!RestaurantPinHasher.Verify(storedPin, suppliedPin, out _))
                throw new UserFriendlyException("A valid manager PIN is required to approve a restaurant refund");
        }

        private async Task<bool> IsEnabled(string settingName, int tenantId) =>
            string.Equals(await SettingManager.GetSettingValueForTenantAsync(settingName, tenantId), "true", StringComparison.OrdinalIgnoreCase);

        private static void ValidateRequestId(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId) || requestId.Trim().Length > 100)
                throw new UserFriendlyException("A valid unique request ID is required");
        }

        private static void EnsureSameRequest(string previousHash, string requestHash)
        {
            if (!string.Equals(previousHash, requestHash, StringComparison.Ordinal))
                throw new UserFriendlyException("This request ID was already used for different refund details");
        }

        private static decimal Paisa(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string Hash<T>(T value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static DateTime GetNepalNow()
        {
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kathmandu"); }
            catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time"); }
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        }

        private sealed class TenderSource(Guid? tenderId, Guid billPaymentId, PaymentMethod paymentMethod, Guid paymentLedgerId, decimal amount)
        {
            public Guid? TenderId { get; } = tenderId;
            public Guid BillPaymentId { get; } = billPaymentId;
            public PaymentMethod PaymentMethod { get; } = paymentMethod;
            public Guid PaymentLedgerId { get; } = paymentLedgerId;
            public decimal Amount { get; } = amount;
            public decimal Remaining { get; set; } = amount;
        }

        private sealed record TenderAllocation(TenderSource Source, decimal Amount);
    }
}
