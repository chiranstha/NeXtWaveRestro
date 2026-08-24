using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Restaurant.Dtos
{
    public class RestaurantSupplierItemMappingDto : EntityDto<Guid>
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid SupplierLedgerId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierSku { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Rate { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal MinimumOrderQty { get; set; }
        public bool IsPreferred { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrEditRestaurantSupplierItemMappingDto : EntityDto<Guid?>
    {
        public Guid ProductId { get; set; }
        public Guid SupplierLedgerId { get; set; }
        public string SupplierSku { get; set; }
        public Guid UnitId { get; set; }
        public decimal Rate { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal MinimumOrderQty { get; set; }
        public bool IsPreferred { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantLowStockSuggestionDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal MaximumStock { get; set; }
        public decimal PendingPurchaseQty { get; set; }
        public decimal SuggestedQty { get; set; }
        public Guid? SupplierLedgerId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierSku { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public bool MissingSupplierMapping { get; set; }
    }

    public class GenerateDraftPurchaseOrdersDto
    {
        public List<Guid> ProductIds { get; set; } = new();
        public string DateMiti { get; set; }
        public string DueDateMiti { get; set; }
    }

    public class GenerateDraftPurchaseOrdersResultDto
    {
        public List<Guid> PurchaseOrderIds { get; set; } = new();
        public List<RestaurantLowStockSuggestionDto> SkippedItems { get; set; } = new();
    }

    public class CreateRestaurantStockAdjustmentDto
    {
        public RestaurantStockAdjustmentType AdjustmentType { get; set; }
        public string DateMiti { get; set; }
        public string Description { get; set; }
        public List<CreateRestaurantStockAdjustmentLineDto> Lines { get; set; } = new();
    }

    public class CreateRestaurantStockAdjustmentLineDto
    {
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public decimal? CountedQty { get; set; }
        public decimal Rate { get; set; }
        public string Reason { get; set; }
    }

    public class RestaurantStockAdjustmentDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }
        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public RestaurantStockAdjustmentType AdjustmentType { get; set; }
        public string Description { get; set; }
        public string CreateUserName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<RestaurantStockAdjustmentLineDto> Lines { get; set; } = new();
    }

    public class RestaurantStockAdjustmentLineDto : EntityDto<Guid>
    {
        public Guid StockAdjustmentId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public string Reason { get; set; }
    }

    public class RestaurantConsumptionFilterDto : RestaurantReportFilterDto
    {
        public Guid? RawMaterialId { get; set; }
        public Guid? MenuProductId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? SalesMasterId { get; set; }
    }

    public class RestaurantConsumptionLedgerDto
    {
        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNo { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid SalesMasterId { get; set; }
        public string SalesVoucherNo { get; set; }
        public Guid? SalesDetailId { get; set; }
        public string VoucherNo { get; set; }
        public Guid MenuProductId { get; set; }
        public string MenuProductName { get; set; }
        public string MenuItemName { get; set; }
        public Guid RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }

    public class RestaurantRecipeCoverageDto
    {
        public Guid MenuItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public bool HasRecipe { get; set; }
        public int ActiveRecipeLineCount { get; set; }
        public decimal EstimatedRecipeCost { get; set; }
        public decimal MenuPrice { get; set; }
        public decimal FoodCostPercent { get; set; }
        public bool MissingRawMaterialSetup { get; set; }
    }

    public class RestaurantRecipeCostingReportDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal MenuPrice { get; set; }
        public decimal RecipeCost { get; set; }
        public decimal FoodCostPercent { get; set; }
        public decimal MarginAmount { get; set; }
        public List<RestaurantRecipeCostingLineDto> Lines { get; set; } = new();
    }

    public class RestaurantRecipeCostingLineDto
    {
        public Guid RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public decimal Quantity { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal WastagePercentage { get; set; }
        public decimal CostRate { get; set; }
        public decimal CostAmount { get; set; }
    }

    public class RestaurantFoodCostingReportDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal SoldQty { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal TheoreticalCostAmount { get; set; }
        public decimal ActualCostAmount { get; set; }
        public decimal WastageCostAmount { get; set; }
        public decimal MarginAmount { get; set; }
        public decimal FoodCostPercent { get; set; }
    }

    public class RestaurantWastageReportDto
    {
        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public string VoucherNo { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public string UserName { get; set; }
    }
}
