using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory
{
    public class MaterialStockPostingService(
        IRepository<Product, Guid> productRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<StockMaintain, Guid> stockMaintainRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<SalesDetail, Guid> salesDetailRepository,
        StockManagementAppService stockManagementAppService) : ErpAppServiceBase
    {
        public async Task EnsurePurchaseLineAllowedAsync(Guid productId)
        {
            if (productId == Guid.Empty)
                return;

            var product = await productRepository.GetAsync(productId);
            if (product.ProductType == ProductTypeEnum.Services)
                return;

            if (product.ProductType == ProductTypeEnum.KitchenItem)
                throw new UserFriendlyException("Kitchen items cannot be purchased directly",
                    $"{product.Name} should consume raw materials through its recipe instead.");

            if (await HasActiveRecipeAsync(productId, product.TenantId))
                throw new UserFriendlyException("Recipe/menu products cannot be purchased directly",
                    $"{product.Name} has an active BOM recipe. Purchase its raw materials instead.");
        }

        public async Task ApplyDirectStockAsync(MaterialStockPostingRequest request)
        {
            if (request.ProductId == Guid.Empty || request.UnitId == Guid.Empty || request.Qty == 0)
                return;

            var product = await productRepository.GetAsync(request.ProductId);
            if (!IsDirectStockProduct(product))
                return;

            await stockManagementAppService.MaintainStock(new StockMaintainDto
            {
                DateMiti = request.DateMiti,
                ProductId = request.ProductId,
                Qty = request.Qty,
                Rate = request.Rate,
                FinancialYearId = request.FinancialYearId,
                Type = request.MovementType,
                UnitId = request.UnitId
            });

            await stockPostingRepository.InsertAsync(new StockPosting
            {
                VoucherNumbering = request.VoucherNumbering,
                VoucherNo = request.VoucherNo,
                Date = DateConverter.ConvertToEnglish(request.DateMiti),
                DateMiti = request.DateMiti,
                LedgerId = request.LedgerId,
                VoucherTypeId = request.VoucherTypeId,
                ProductId = request.ProductId,
                UnitId = request.UnitId,
                GrossAmount = request.GrossAmount,
                DiscountAmount = request.DiscountAmount,
                NetAmount = request.NetAmount,
                Amount = request.Amount,
                TaxAmount = request.TaxAmount,
                AgainstVoucherTypeId = request.AgainstVoucherTypeId,
                AgainstVoucherNo = request.AgainstVoucherNo ?? "",
                InWardQty = request.MovementType == StockMaintainTypeEnum.Inward ? request.Qty : 0,
                OutWardQty = request.MovementType == StockMaintainTypeEnum.Outward ? request.Qty : 0,
                Rate = request.Rate,
                IsValueIncrease = request.IsValueIncrease,
                FinancialYearId = request.FinancialYearId,
                MasterId = request.MasterId,
                VendorVoucherNo = request.VendorVoucherNo ?? "",
                SourceDetailId = request.SourceDetailId,
                TenantId = request.TenantId
            });
        }

        public async Task ApplySalesIssueAsync(MaterialSalesStockPostingRequest request)
        {
            var product = await productRepository.GetAsync(request.ProductId);
            if (product.ProductType == ProductTypeEnum.Services)
                return;

            var activeRecipe = await GetActiveRecipeAsync(request.ProductId, request.TenantId);
            if (activeRecipe.Count == 0)
            {
                if (product.ProductType == ProductTypeEnum.KitchenItem)
                    return;

                await ValidateAvailableStockAsync(request.ProductId, request.UnitId, request.Qty, product.Name);
                await ApplyDirectStockAsync(new MaterialStockPostingRequest
                {
                    DateMiti = request.DateMiti,
                    LedgerId = request.LedgerId,
                    VoucherTypeId = request.VoucherTypeId,
                    VoucherNo = request.VoucherNo,
                    VoucherNumbering = request.VoucherNumbering,
                    ProductId = request.ProductId,
                    UnitId = request.UnitId,
                    Qty = request.Qty,
                    Rate = request.Rate,
                    GrossAmount = request.GrossAmount,
                    DiscountAmount = request.DiscountAmount,
                    NetAmount = request.NetAmount,
                    Amount = request.Amount,
                    TaxAmount = request.TaxAmount,
                    MovementType = StockMaintainTypeEnum.Outward,
                    IsValueIncrease = false,
                    FinancialYearId = request.FinancialYearId,
                    MasterId = request.MasterId,
                    SourceDetailId = request.SourceDetailId,
                    TenantId = request.TenantId
                });
                return;
            }

            var recipeProductQty = await ConvertQtyAsync(request.ProductId, request.UnitId, product.UnitId, request.Qty);
            foreach (var recipeLine in activeRecipe)
            {
                var requiredQty = recipeProductQty * recipeLine.Quantity * (1 + recipeLine.WastagePercentage / 100);
                var rawMaterial = recipeLine.RawMaterialFk ?? await productRepository.GetAsync(recipeLine.RawMaterialId);
                var rate = recipeLine.CostRate > 0 ? recipeLine.CostRate : rawMaterial.PurchaseRate;
                var amount = requiredQty * rate;

                await ValidateAvailableStockAsync(recipeLine.RawMaterialId, recipeLine.UnitId, requiredQty,
                    rawMaterial.Name);
                await ApplyDirectStockAsync(new MaterialStockPostingRequest
                {
                    DateMiti = request.DateMiti,
                    LedgerId = request.LedgerId,
                    VoucherTypeId = request.VoucherTypeId,
                    VoucherNo = request.VoucherNo,
                    VoucherNumbering = request.VoucherNumbering,
                    ProductId = recipeLine.RawMaterialId,
                    UnitId = recipeLine.UnitId,
                    Qty = requiredQty,
                    Rate = rate,
                    GrossAmount = amount,
                    DiscountAmount = 0,
                    NetAmount = amount,
                    Amount = amount,
                    TaxAmount = 0,
                    MovementType = StockMaintainTypeEnum.Outward,
                    IsValueIncrease = false,
                    FinancialYearId = request.FinancialYearId,
                    MasterId = request.MasterId,
                    SourceDetailId = request.SourceDetailId,
                    TenantId = request.TenantId
                });
            }
        }

        public async Task ApplySalesReturnAsync(MaterialSalesReturnStockPostingRequest request)
        {
            if (request.SalesDetailId.HasValue && request.SalesDetailId.Value != Guid.Empty)
            {
                var restoredFromOriginal = await ApplySalesReturnFromOriginalSaleAsync(request);
                if (restoredFromOriginal)
                    return;
            }

            await ApplySalesReturnByCurrentProductAsync(request);
        }

        public async Task ReverseExistingStockPostingsAsync(IReadOnlyCollection<StockPosting> postings)
        {
            foreach (var posting in postings)
            {
                if (posting.InWardQty != 0)
                    await stockManagementAppService.MaintainStock(new StockMaintainDto
                    {
                        DateMiti = posting.DateMiti,
                        ProductId = posting.ProductId,
                        Qty = -posting.InWardQty,
                        Rate = posting.Rate,
                        FinancialYearId = posting.FinancialYearId,
                        Type = StockMaintainTypeEnum.Inward,
                        UnitId = posting.UnitId
                    });

                if (posting.OutWardQty != 0)
                    await stockManagementAppService.MaintainStock(new StockMaintainDto
                    {
                        DateMiti = posting.DateMiti,
                        ProductId = posting.ProductId,
                        Qty = -posting.OutWardQty,
                        Rate = posting.Rate,
                        FinancialYearId = posting.FinancialYearId,
                        Type = StockMaintainTypeEnum.Outward,
                        UnitId = posting.UnitId
                    });
            }
        }

        public async Task<decimal> GetAvailableStockAsync(Guid productId, Guid unitId)
        {
            var postings = await stockPostingRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.ProductId == productId &&
                            x.FinancialYearId == FinancialYearId &&
                            !x.IsDeleted)
                .Select(x => new
                {
                    x.UnitId,
                    Qty = x.InWardQty - x.OutWardQty
                })
                .ToListAsync();

            if (postings.Count > 0)
            {
                decimal available = 0;
                foreach (var posting in postings)
                {
                    available += await ConvertQtyAsync(productId, posting.UnitId, unitId, posting.Qty);
                }

                return available;
            }

            return await GetAvailableStockFromMaintainAsync(productId, unitId);
        }

        private async Task<decimal> GetAvailableStockFromMaintainAsync(Guid productId, Guid unitId)
        {
            var stock = await stockMaintainRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId)
                .OrderByDescending(x => x.LastModifiedDate)
                .FirstOrDefaultAsync();

            if (stock == null)
                return 0;

            var available = stock.OpeningQty + stock.InwardQty - stock.OutwardQty;
            return await ConvertQtyAsync(productId, stock.UnitId, unitId, available);
        }

        private async Task<bool> ApplySalesReturnFromOriginalSaleAsync(MaterialSalesReturnStockPostingRequest request)
        {
            var originalDetail = await salesDetailRepository.FirstOrDefaultAsync(request.SalesDetailId.Value);
            if (originalDetail == null || originalDetail.Qty == 0)
                return false;

            var originalPostings = await stockPostingRepository.GetAll()
                .Where(x => x.TenantId == request.TenantId &&
                            x.SourceDetailId == request.SalesDetailId &&
                            x.OutWardQty > 0)
                .AsNoTracking()
                .ToListAsync();

            if (originalPostings.Count == 0)
                return false;

            var ratio = request.Qty / originalDetail.Qty;
            foreach (var posting in originalPostings)
            {
                var qty = posting.OutWardQty * ratio;
                var amount = posting.Amount * ratio;

                await ApplyDirectStockAsync(new MaterialStockPostingRequest
                {
                    DateMiti = request.DateMiti,
                    LedgerId = request.LedgerId,
                    VoucherTypeId = request.VoucherTypeId,
                    VoucherNo = request.VoucherNo,
                    VoucherNumbering = request.VoucherNumbering,
                    AgainstVoucherTypeId = request.AgainstVoucherTypeId,
                    AgainstVoucherNo = request.AgainstVoucherNo,
                    ProductId = posting.ProductId,
                    UnitId = posting.UnitId,
                    Qty = qty,
                    Rate = posting.Rate,
                    GrossAmount = posting.GrossAmount * ratio,
                    DiscountAmount = posting.DiscountAmount * ratio,
                    NetAmount = posting.NetAmount * ratio,
                    Amount = amount,
                    TaxAmount = posting.TaxAmount * ratio,
                    MovementType = StockMaintainTypeEnum.Inward,
                    IsValueIncrease = true,
                    FinancialYearId = request.FinancialYearId,
                    MasterId = request.MasterId,
                    VendorVoucherNo = request.VendorVoucherNo,
                    SourceDetailId = request.SourceDetailId,
                    TenantId = request.TenantId
                });
            }

            return true;
        }

        private async Task ApplySalesReturnByCurrentProductAsync(MaterialSalesReturnStockPostingRequest request)
        {
            var product = await productRepository.GetAsync(request.ProductId);
            if (product.ProductType == ProductTypeEnum.Services)
                return;

            var activeRecipe = await GetActiveRecipeAsync(request.ProductId, request.TenantId);
            if (activeRecipe.Count == 0)
            {
                if (product.ProductType == ProductTypeEnum.KitchenItem)
                    return;

                await ApplyDirectStockAsync(new MaterialStockPostingRequest
                {
                    DateMiti = request.DateMiti,
                    LedgerId = request.LedgerId,
                    VoucherTypeId = request.VoucherTypeId,
                    VoucherNo = request.VoucherNo,
                    VoucherNumbering = request.VoucherNumbering,
                    AgainstVoucherTypeId = request.AgainstVoucherTypeId,
                    AgainstVoucherNo = request.AgainstVoucherNo,
                    ProductId = request.ProductId,
                    UnitId = request.UnitId,
                    Qty = request.Qty,
                    Rate = request.Rate,
                    GrossAmount = request.GrossAmount,
                    DiscountAmount = request.DiscountAmount,
                    NetAmount = request.NetAmount,
                    Amount = request.Amount,
                    TaxAmount = request.TaxAmount,
                    MovementType = StockMaintainTypeEnum.Inward,
                    IsValueIncrease = true,
                    FinancialYearId = request.FinancialYearId,
                    MasterId = request.MasterId,
                    VendorVoucherNo = request.VendorVoucherNo,
                    SourceDetailId = request.SourceDetailId,
                    TenantId = request.TenantId
                });
                return;
            }

            var recipeProductQty = await ConvertQtyAsync(request.ProductId, request.UnitId, product.UnitId, request.Qty);
            foreach (var recipeLine in activeRecipe)
            {
                var requiredQty = recipeProductQty * recipeLine.Quantity * (1 + recipeLine.WastagePercentage / 100);
                var rawMaterial = recipeLine.RawMaterialFk ?? await productRepository.GetAsync(recipeLine.RawMaterialId);
                var rate = recipeLine.CostRate > 0 ? recipeLine.CostRate : rawMaterial.PurchaseRate;
                var amount = requiredQty * rate;

                await ApplyDirectStockAsync(new MaterialStockPostingRequest
                {
                    DateMiti = request.DateMiti,
                    LedgerId = request.LedgerId,
                    VoucherTypeId = request.VoucherTypeId,
                    VoucherNo = request.VoucherNo,
                    VoucherNumbering = request.VoucherNumbering,
                    AgainstVoucherTypeId = request.AgainstVoucherTypeId,
                    AgainstVoucherNo = request.AgainstVoucherNo,
                    ProductId = recipeLine.RawMaterialId,
                    UnitId = recipeLine.UnitId,
                    Qty = requiredQty,
                    Rate = rate,
                    GrossAmount = amount,
                    DiscountAmount = 0,
                    NetAmount = amount,
                    Amount = amount,
                    TaxAmount = 0,
                    MovementType = StockMaintainTypeEnum.Inward,
                    IsValueIncrease = true,
                    FinancialYearId = request.FinancialYearId,
                    MasterId = request.MasterId,
                    VendorVoucherNo = request.VendorVoucherNo,
                    SourceDetailId = request.SourceDetailId,
                    TenantId = request.TenantId
                });
            }
        }

        private async Task ValidateAvailableStockAsync(Guid productId, Guid unitId, decimal requiredQty,
            string productName)
        {
            var availableQty = await GetAvailableStockAsync(productId, unitId);
            if (requiredQty > availableQty && await ShouldBlockNegativeStockAsync())
                throw new UserFriendlyException("Insufficient stock",
                    $"{productName} requires {requiredQty}, available {availableQty}");
        }

        private async Task<bool> ShouldBlockNegativeStockAsync()
        {
            if (!AbpSession.TenantId.HasValue)
                return true;

            var status = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.NegativeStockStatus,
                AbpSession.TenantId.Value);

            return string.Equals(status, "Block", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<List<Bom>> GetActiveRecipeAsync(Guid productId, int? tenantId)
        {
            return await bomRepository.GetAll()
                .Include(x => x.RawMaterialFk)
                .Where(x => x.TenantId == tenantId &&
                            x.ProductId == productId &&
                            !x.IsDeleted &&
                            x.IsActive)
                .ToListAsync();
        }

        private async Task<bool> HasActiveRecipeAsync(Guid productId, int? tenantId)
        {
            return await bomRepository.GetAll()
                .AnyAsync(x => x.TenantId == tenantId &&
                               x.ProductId == productId &&
                               !x.IsDeleted &&
                               x.IsActive);
        }

        private static bool IsDirectStockProduct(Product product)
        {
            return product.ProductType is ProductTypeEnum.Product or ProductTypeEnum.RawMaterial;
        }

        private async Task<decimal> ConvertQtyAsync(Guid productId, Guid fromUnitId, Guid toUnitId, decimal qty)
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

            var baseQty = qty * from.PrimaryQty / from.Qty;
            return baseQty * to.Qty / to.PrimaryQty;
        }
    }

    public class MaterialStockPostingRequest
    {
        public string DateMiti { get; set; }
        public Guid? LedgerId { get; set; }
        public Guid VoucherTypeId { get; set; }
        public string VoucherNo { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid AgainstVoucherTypeId { get; set; } = Guid.Empty;
        public string AgainstVoucherNo { get; set; } = "";
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public StockMaintainTypeEnum MovementType { get; set; }
        public bool IsValueIncrease { get; set; }
        public Guid FinancialYearId { get; set; }
        public Guid MasterId { get; set; }
        public string VendorVoucherNo { get; set; } = "";
        public Guid? SourceDetailId { get; set; }
        public int? TenantId { get; set; }
    }

    public class MaterialSalesStockPostingRequest
    {
        public string DateMiti { get; set; }
        public Guid? LedgerId { get; set; }
        public Guid VoucherTypeId { get; set; }
        public string VoucherNo { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public Guid FinancialYearId { get; set; }
        public Guid MasterId { get; set; }
        public Guid? SourceDetailId { get; set; }
        public int? TenantId { get; set; }
    }

    public class MaterialSalesReturnStockPostingRequest
    {
        public string DateMiti { get; set; }
        public Guid? LedgerId { get; set; }
        public Guid VoucherTypeId { get; set; }
        public string VoucherNo { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid AgainstVoucherTypeId { get; set; } = Guid.Empty;
        public string AgainstVoucherNo { get; set; } = "";
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public Guid FinancialYearId { get; set; }
        public Guid MasterId { get; set; }
        public string VendorVoucherNo { get; set; } = "";
        public Guid? SalesDetailId { get; set; }
        public Guid? SourceDetailId { get; set; }
        public int? TenantId { get; set; }
    }
}
