using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    public class RestaurantReorderService(
        IRepository<Product, Guid> productRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<RestaurantSupplierItemMapping, Guid> supplierItemMappingRepository,
        IRepository<PurchaseOrderMaster, Guid> purchaseOrderMasterRepository,
        IRepository<PurchaseOrderDetails, Guid> purchaseOrderDetailRepository,
        IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
        MaterialStockPostingService materialStockPostingService) : ErpAppServiceBase
    {
        public async Task<List<RestaurantLowStockSuggestionDto>> GetLowStockSuggestionsAsync(IReadOnlyCollection<Guid> productIds = null)
        {
            var tenantId = AbpSession.GetTenantId();
            var query = productRepository.GetAll()
                .Include(x => x.UnitFk)
                .Where(x => x.TenantId == tenantId &&
                            x.ProductType == ProductTypeEnum.RawMaterial &&
                            x.IsActive &&
                            !x.IsDeleted &&
                            x.MinimumStock > 0);

            if (productIds?.Count > 0)
                query = query.Where(x => productIds.Contains(x.Id));

            var products = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            var suggestions = new List<RestaurantLowStockSuggestionDto>();
            foreach (var product in products)
            {
                var availableBaseQty = await materialStockPostingService.GetAvailableStockAsync(product.Id, product.UnitId);
                if (availableBaseQty >= product.MinimumStock)
                    continue;

                var pendingBaseQty = await GetPendingPurchaseQtyAsync(product.Id, product.UnitId);
                var targetBaseQty = product.MaximumStock > 0 ? product.MaximumStock : product.MinimumStock;
                var suggestedBaseQty = targetBaseQty - availableBaseQty - pendingBaseQty;
                if (suggestedBaseQty <= 0)
                    continue;

                var mapping = await GetPreferredMappingAsync(product.Id, tenantId);
                var unitId = mapping?.UnitId ?? product.UnitId;
                var unitName = mapping?.UnitFk?.Name ?? product.UnitFk?.Name ?? "";
                var suggestedQty = await ConvertQtyAsync(product.Id, product.UnitId, unitId, suggestedBaseQty);
                var availableQty = await ConvertQtyAsync(product.Id, product.UnitId, unitId, availableBaseQty);
                var pendingPurchaseQty = await ConvertQtyAsync(product.Id, product.UnitId, unitId, pendingBaseQty);
                var minimumStock = await ConvertQtyAsync(product.Id, product.UnitId, unitId, product.MinimumStock);
                var maximumStock = await ConvertQtyAsync(product.Id, product.UnitId, unitId, product.MaximumStock);

                if (mapping != null && mapping.MinimumOrderQty > 0 && suggestedQty < mapping.MinimumOrderQty)
                    suggestedQty = mapping.MinimumOrderQty;

                var rate = mapping?.Rate > 0 ? mapping.Rate : product.PurchaseRate;
                suggestions.Add(new RestaurantLowStockSuggestionDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitId = unitId,
                    UnitName = unitName,
                    AvailableQty = availableQty,
                    MinimumStock = minimumStock,
                    MaximumStock = maximumStock,
                    PendingPurchaseQty = pendingPurchaseQty,
                    SuggestedQty = suggestedQty,
                    SupplierLedgerId = mapping?.SupplierLedgerId,
                    SupplierName = mapping?.SupplierLedgerFk?.Name ?? "",
                    SupplierSku = mapping?.SupplierSku ?? "",
                    Rate = rate,
                    Amount = suggestedQty * rate,
                    MissingSupplierMapping = mapping == null
                });
            }

            return suggestions;
        }

        public async Task<GenerateDraftPurchaseOrdersResultDto> GenerateDraftPurchaseOrdersAsync(GenerateDraftPurchaseOrdersDto input)
        {
            input ??= new GenerateDraftPurchaseOrdersDto();
            var productIds = input.ProductIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            var suggestions = await GetLowStockSuggestionsAsync(productIds);
            var result = new GenerateDraftPurchaseOrdersResultDto
            {
                SkippedItems = suggestions.Where(x => x.MissingSupplierMapping || !x.SupplierLedgerId.HasValue).ToList()
            };

            var orderable = suggestions
                .Where(x => !x.MissingSupplierMapping && x.SupplierLedgerId.HasValue && x.SuggestedQty > 0)
                .ToList();

            if (orderable.Count == 0)
                return result;

            var tenantId = AbpSession.GetTenantId();
            var dateMiti = string.IsNullOrWhiteSpace(input.DateMiti)
                ? DateConverter.ConvertToNepali(DateTime.Today)
                : input.DateMiti;
            var date = DateConverter.ConvertToEnglish(dateMiti);
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseOrder");
            var nextNumber = await GetNextPurchaseOrderNumberAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseOrder");
            var orderIndex = 0;

            foreach (var supplierGroup in orderable.GroupBy(x => x.SupplierLedgerId.Value))
            {
                var leadDays = await GetMaxLeadTimeDaysAsync(supplierGroup.Select(x => x.ProductId).ToList(), supplierGroup.Key, tenantId);
                var dueDateMiti = string.IsNullOrWhiteSpace(input.DueDateMiti)
                    ? DateConverter.ConvertToNepali(date.AddDays(leadDays))
                    : input.DueDateMiti;
                var dueDate = DateConverter.ConvertToEnglish(dueDateMiti);
                var voucherNumberingValue = nextNumber + orderIndex;
                var voucherNo = $"{voucherNumbering.Prefix}{voucherNumberingValue}{voucherNumbering.Postfix}";

                var master = new PurchaseOrderMaster
                {
                    TenantId = tenantId,
                    VoucherNumbering = voucherNumberingValue,
                    VoucherNo = voucherNo,
                    Date = date,
                    DateMiti = dateMiti,
                    DueDate = dueDate,
                    DueDateMiti = dueDateMiti,
                    Cancelled = false,
                    IsCompleted = false,
                    Description = "Auto reorder from restaurant inventory",
                    TotalAmount = supplierGroup.Sum(x => x.Amount),
                    VoucherTypeId = voucherTypeId,
                    FinancialYearId = FinancialYearId,
                    LedgerId = supplierGroup.Key,
                    CreateUserId = AbpSession.UserId,
                    UpdateUserId = null,
                    PostingNumbering = PostingNumbering
                };

                var masterId = await purchaseOrderMasterRepository.InsertAndGetIdAsync(master);
                foreach (var item in supplierGroup)
                {
                    await purchaseOrderDetailRepository.InsertAsync(new PurchaseOrderDetails
                    {
                        TenantId = tenantId,
                        Qty = item.SuggestedQty,
                        Rate = item.Rate,
                        Amount = item.Amount,
                        ProductCode = item.SupplierSku,
                        PurchaseOrderMasterId = masterId,
                        ProductId = item.ProductId,
                        UnitId = item.UnitId
                    });
                }

                result.PurchaseOrderIds.Add(masterId);
                orderIndex++;
            }

            return result;
        }

        public Task<GenerateDraftPurchaseOrdersResultDto> GenerateDraftPurchaseOrdersForProductsAsync(
            IReadOnlyCollection<Guid> productIds,
            string dateMiti)
        {
            return GenerateDraftPurchaseOrdersAsync(new GenerateDraftPurchaseOrdersDto
            {
                ProductIds = productIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>(),
                DateMiti = dateMiti
            });
        }

        private async Task<RestaurantSupplierItemMapping> GetPreferredMappingAsync(Guid productId, int tenantId)
        {
            return await supplierItemMappingRepository.GetAll()
                .Include(x => x.SupplierLedgerFk)
                .Include(x => x.UnitFk)
                .Where(x => x.TenantId == tenantId &&
                            x.ProductId == productId &&
                            x.IsPreferred &&
                            x.IsActive &&
                            !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private async Task<decimal> GetPendingPurchaseQtyAsync(Guid productId, Guid targetUnitId)
        {
            var tenantId = AbpSession.GetTenantId();
            var completedPurchaseOrderIds = purchaseMasterRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.PurchaseOrderMasterId.HasValue)
                .Select(x => x.PurchaseOrderMasterId.Value);

            var pendingRows = await purchaseOrderDetailRepository.GetAll()
                .Include(x => x.PurchaseOrderMasterFk)
                .Where(x => x.TenantId == tenantId &&
                            x.ProductId == productId &&
                            !x.IsDeleted &&
                            x.PurchaseOrderMasterFk.TenantId == tenantId &&
                            !x.PurchaseOrderMasterFk.IsDeleted &&
                            !x.PurchaseOrderMasterFk.Cancelled &&
                            !completedPurchaseOrderIds.Contains(x.PurchaseOrderMasterId))
                .Select(x => new { x.UnitId, x.Qty })
                .ToListAsync();

            decimal qty = 0;
            foreach (var row in pendingRows)
                qty += await ConvertQtyAsync(productId, row.UnitId, targetUnitId, row.Qty);

            return qty;
        }

        private async Task<int> GetNextPurchaseOrderNumberAsync()
        {
            var existingNumbers = await purchaseOrderMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering)
                .ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseOrder");
            return existingNumbers.Count == 0 ? voucherNumbering.StartIndex : existingNumbers.Max() + 1;
        }

        private async Task<int> GetMaxLeadTimeDaysAsync(List<Guid> productIds, Guid supplierLedgerId, int tenantId)
        {
            var leadDays = await supplierItemMappingRepository.GetAll()
                .Where(x => x.TenantId == tenantId &&
                            productIds.Contains(x.ProductId) &&
                            x.SupplierLedgerId == supplierLedgerId &&
                            x.IsActive &&
                            !x.IsDeleted)
                .Select(x => x.LeadTimeDays)
                .ToListAsync();

            return leadDays.Count == 0 ? 0 : Math.Max(0, leadDays.Max());
        }

        private async Task<decimal> ConvertQtyAsync(Guid productId, Guid fromUnitId, Guid toUnitId, decimal qty)
        {
            if (qty == 0 || fromUnitId == toUnitId)
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
    }
}
