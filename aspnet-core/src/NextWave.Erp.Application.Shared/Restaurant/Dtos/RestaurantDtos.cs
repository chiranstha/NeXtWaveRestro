using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Restaurant.Dtos
{
    public class RestaurantAreaDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrEditRestaurantAreaDto : EntityDto<Guid?>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantTableDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int Capacity { get; set; }
        public int SortOrder { get; set; }
        public RestaurantTableStatus Status { get; set; }
        public bool IsActive { get; set; }
        public Guid AreaId { get; set; }
        public string AreaName { get; set; }
    }

    public class CreateOrEditRestaurantTableDto : EntityDto<Guid?>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int Capacity { get; set; }
        public int SortOrder { get; set; }
        public RestaurantTableStatus Status { get; set; } = RestaurantTableStatus.Available;
        public bool IsActive { get; set; } = true;
        public Guid AreaId { get; set; }
    }

    public class RestaurantStationDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public RestaurantStationType StationType { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrEditRestaurantStationDto : EntityDto<Guid?>
    {
        public string Name { get; set; }
        public RestaurantStationType StationType { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantDeviceDto : EntityDto<Guid>
    {
        public string DeviceCode { get; set; }
        public string Name { get; set; }
        public long? UserId { get; set; }
        public RestaurantDeviceStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public long LastPulledSeq { get; set; }
        public long LastAcknowledgedSeq { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public string LastSyncError { get; set; }
        public bool HasConflict { get; set; }
    }

    public class RegisterRestaurantDeviceDto
    {
        public Guid? Id { get; set; }
        public string DeviceCode { get; set; }
        public string Name { get; set; }
        public long? UserId { get; set; }
    }

    public class RestaurantOperationalSettingsDto
    {
        public decimal VatPercent { get; set; }
        public decimal ServiceChargePercent { get; set; }
        public bool RequireManagerPinForSensitiveActions { get; set; }
        public string ManagerPin { get; set; }
        public string NegativeStockStatus { get; set; }
        public bool TicketPrintingEnabled { get; set; }
        public bool ChannelAvailabilityEnabled { get; set; }
        public string TableWorkflow { get; set; }
    }

    public class RestaurantCashShiftDto : EntityDto<Guid>
    {
        public string RegisterName { get; set; }
        public long OpenedByUserId { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal ExpectedClosingCash { get; set; }
        public decimal CashSales { get; set; }
        public decimal CashIn { get; set; }
        public decimal CashOut { get; set; }
        public bool IsClosed { get; set; }
        public long? ClosedByUserId { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal? CountedClosingCash { get; set; }
        public decimal? CashVariance { get; set; }
        public string CloseNote { get; set; }
    }

    public class OpenRestaurantCashShiftDto
    {
        public string RegisterName { get; set; }
        public decimal OpeningCash { get; set; }
    }

    public class MoveRestaurantCashDto
    {
        public Guid ShiftId { get; set; }
        public bool IsCashIn { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }

    public class CloseRestaurantCashShiftDto
    {
        public Guid ShiftId { get; set; }
        public decimal CountedCash { get; set; }
        public string Note { get; set; }
        public string ManagerPin { get; set; }
    }

    public class RestaurantBillTenderDto
    {
        public PaymentMethod PaymentMethod { get; set; }
        public Guid? PaymentLedgerId { get; set; }
        public decimal Amount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public string Reference { get; set; }
    }

    public class RestaurantMenuCategoryDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrEditRestaurantMenuCategoryDto : EntityDto<Guid?>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantMenuItemDto : EntityDto<Guid>
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public ProductTypeEnum ProductType { get; set; }
        public Guid? StationId { get; set; }
        public string StationName { get; set; }
        public string DisplayName { get; set; }
        public string ShortCode { get; set; }
        public string Description { get; set; }
        public string ColorHex { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int PreparationMinutes { get; set; }
        public int SortOrder { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? UnavailableUntil { get; set; }
        public bool? IsVeg { get; set; }
        public int? SpiceLevel { get; set; }
        public bool IsFeatured { get; set; }
        public bool HasVariants { get; set; }
        public bool HasModifiers { get; set; }
        public bool IsActive { get; set; }
        public bool HasRecipe { get; set; }
        public List<RestaurantMenuVariantDto> Variants { get; set; } = new();
        public List<RestaurantModifierGroupDto> ModifierGroups { get; set; } = new();
        public List<RestaurantMenuItemTagDto> Tags { get; set; } = new();
    }

    public class CreateOrEditRestaurantMenuItemDto : EntityDto<Guid?>
    {
        public Guid CategoryId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? StationId { get; set; }
        public string DisplayName { get; set; }
        public string ShortCode { get; set; }
        public string Description { get; set; }
        public string ColorHex { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int PreparationMinutes { get; set; }
        public int SortOrder { get; set; }
        public bool IsAvailable { get; set; } = true;
        public DateTime? UnavailableUntil { get; set; }
        public bool? IsVeg { get; set; }
        public int? SpiceLevel { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantMenuVariantDto : EntityDto<Guid>
    {
        public Guid MenuItemId { get; set; }
        public string Name { get; set; }
        public decimal PriceDelta { get; set; }
        public bool IsAbsolutePrice { get; set; }
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrEditRestaurantMenuVariantDto : EntityDto<Guid?>
    {
        public Guid MenuItemId { get; set; }
        public string Name { get; set; }
        public decimal PriceDelta { get; set; }
        public bool IsAbsolutePrice { get; set; }
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantModifierDto : EntityDto<Guid>
    {
        public Guid ModifierGroupId { get; set; }
        public string Name { get; set; }
        public decimal PriceDelta { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrEditRestaurantModifierDto : EntityDto<Guid?>
    {
        public Guid ModifierGroupId { get; set; }
        public string Name { get; set; }
        public decimal PriceDelta { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantModifierGroupDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; }
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public List<RestaurantModifierDto> Modifiers { get; set; } = new();
    }

    public class CreateOrEditRestaurantModifierGroupDto : EntityDto<Guid?>
    {
        public string Name { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; } = 1;
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveRestaurantMenuItemModifierGroupsDto
    {
        public Guid MenuItemId { get; set; }
        public List<Guid> ModifierGroupIds { get; set; } = new();
    }

    public class RestaurantMenuItemTagDto : EntityDto<Guid>
    {
        public Guid MenuItemId { get; set; }
        public string Name { get; set; }
        public string ColorHex { get; set; }
        public int SortOrder { get; set; }
    }

    public class SaveRestaurantMenuItemTagsDto
    {
        public Guid MenuItemId { get; set; }
        public List<RestaurantMenuItemTagDto> Tags { get; set; } = new();
    }

    public class SetRestaurantMenuItemAvailabilityDto
    {
        public Guid MenuItemId { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? UnavailableUntil { get; set; }
    }

    public class RestaurantMenuEditorDataDto
    {
        public List<RestaurantMenuCategoryDto> Categories { get; set; } = new();
        public List<RestaurantMenuItemDto> MenuItems { get; set; } = new();
        public List<RestaurantModifierGroupDto> ModifierGroups { get; set; } = new();
        public List<RestaurantStationDto> Stations { get; set; } = new();
    }

    public class RestaurantRecipeLineDto : EntityDto<Guid?>
    {
        public Guid RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public decimal Quantity { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal WastagePercentage { get; set; }
        public decimal CostRate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveRestaurantRecipeDto
    {
        public Guid ProductId { get; set; }
        public List<RestaurantRecipeLineDto> Lines { get; set; } = new();
    }

    public class RestaurantRecipeCostDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TotalCost { get; set; }
        public List<RestaurantRecipeLineDto> Lines { get; set; } = new();
    }

    public class CreateOrEditRestaurantOrderDto : EntityDto<Guid?>
    {
        public RestaurantOrderType OrderType { get; set; }
        public Guid? TableId { get; set; }
        public Guid? DeviceId { get; set; }
        public string Source { get; set; }
        public string ClientRequestId { get; set; }
        public long? WaiterUserId { get; set; }
        public int GuestCount { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhoneNo { get; set; }
        public string Notes { get; set; }
        public List<CreateOrEditRestaurantOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrEditRestaurantOrderItemDto : EntityDto<Guid?>
    {
        public Guid? MenuItemId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? StationId { get; set; }
        public Guid TaxId { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Notes { get; set; }
        public List<CreateOrEditRestaurantOrderItemModifierDto> Modifiers { get; set; } = new();
    }

    public class CreateOrEditRestaurantOrderItemModifierDto : EntityDto<Guid?>
    {
        public Guid ModifierId { get; set; }
        public decimal Qty { get; set; } = 1;
    }

    public class RestaurantOrderDto : EntityDto<Guid>
    {
        public string OrderNo { get; set; }
        public RestaurantOrderType OrderType { get; set; }
        public RestaurantOrderStatus Status { get; set; }
        public Guid? TableId { get; set; }
        public string TableName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhoneNo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? TableSessionOpenedAt { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal BilledGrossAmount { get; set; }
        public decimal BilledDiscountAmount { get; set; }
        public decimal BilledTaxAmount { get; set; }
        public decimal BilledNetAmount { get; set; }
        public decimal BilledGrandTotal { get; set; }
        public decimal RemainingGrossAmount { get; set; }
        public decimal RemainingDiscountAmount { get; set; }
        public decimal RemainingTaxAmount { get; set; }
        public decimal RemainingNetAmount { get; set; }
        public decimal RemainingGrandTotal { get; set; }
        public Guid? SalesMasterId { get; set; }
        public List<RestaurantOrderItemDto> Items { get; set; } = new();
    }

    public class RestaurantOrderItemDto : EntityDto<Guid>
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid? MenuItemId { get; set; }
        public Guid? VariantId { get; set; }
        public string ItemNameSnapshot { get; set; }
        public string VariantNameSnapshot { get; set; }
        public string ModifierSummary { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public Guid? StationId { get; set; }
        public string StationName { get; set; }
        public Guid TaxId { get; set; }
        public decimal Qty { get; set; }
        public decimal BilledQty { get; set; }
        public decimal UnbilledQty { get; set; }
        public decimal Rate { get; set; }
        public decimal ModifierTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Amount { get; set; }
        public RestaurantOrderItemStatus Status { get; set; }
        public string Notes { get; set; }
        public string CancelReason { get; set; }
        public List<RestaurantOrderItemModifierDto> Modifiers { get; set; } = new();
    }

    public class RestaurantOrderItemModifierDto : EntityDto<Guid>
    {
        public Guid ModifierId { get; set; }
        public string ModifierNameSnapshot { get; set; }
        public decimal PriceDelta { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
    }

    public class VoidRestaurantOrderItemDto
    {
        public Guid OrderItemId { get; set; }
        public string Reason { get; set; }
        public string ApprovalPin { get; set; }
        public string ApprovalNote { get; set; }
    }

    public class CancelRestaurantTicketDto
    {
        public Guid TicketId { get; set; }
        public string Reason { get; set; }
        public string ApprovalPin { get; set; }
        public string ApprovalNote { get; set; }
    }

    public class ReprintRestaurantTicketDto
    {
        public Guid TicketId { get; set; }
        public string ApprovalPin { get; set; }
        public string ApprovalNote { get; set; }
    }

    public class RestaurantTicketDto : EntityDto<Guid>
    {
        public string RestaurantName { get; set; }
        public string TicketNo { get; set; }
        public string OrderNo { get; set; }
        public Guid OrderId { get; set; }
        public RestaurantOrderType OrderType { get; set; }
        public Guid? TableId { get; set; }
        public string TableName { get; set; }
        public string WaiterName { get; set; }
        public Guid StationId { get; set; }
        public string StationName { get; set; }
        public RestaurantTicketType TicketType { get; set; }
        public RestaurantTicketPurpose Purpose { get; set; }
        public RestaurantTicketStatus Status { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? ServedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? PrintedAt { get; set; }
        public DateTime? LastPrintedAt { get; set; }
        public string PrintStatus { get; set; }
        public DateTime? LastPrintConfirmedAt { get; set; }
        public int PrintCount { get; set; }
        public bool IsReprint { get; set; }
        public string OrderNotes { get; set; }
        public string CancelReason { get; set; }
        public List<RestaurantTicketItemDto> Items { get; set; } = new();
    }

    public class RestaurantTicketItemDto : EntityDto<Guid>
    {
        public Guid OrderItemId { get; set; }
        public string ProductName { get; set; }
        public string ItemNameSnapshot { get; set; }
        public string VariantNameSnapshot { get; set; }
        public string ModifierSummary { get; set; }
        public string UnitName { get; set; }
        public decimal Qty { get; set; }
        public RestaurantOrderItemStatus Status { get; set; }
        public string Notes { get; set; }
        public string CancelReason { get; set; }
    }

    public class UpdateRestaurantTicketStatusDto
    {
        public Guid TicketId { get; set; }
        public RestaurantTicketStatus Status { get; set; }
        public string CancelReason { get; set; }
    }

    public class UpdateRestaurantTicketItemStatusDto
    {
        public Guid TicketItemId { get; set; }
        public RestaurantOrderItemStatus Status { get; set; }
        public string CancelReason { get; set; }
    }

    public class BulkUpdateRestaurantTicketItemStatusDto
    {
        public List<Guid> TicketItemIds { get; set; } = new();
        public RestaurantOrderItemStatus Status { get; set; }
    }

    public class BulkUpdateRestaurantTicketItemStatusResultDto
    {
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
    }

    public class RestaurantMaterialRequirementDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal RequiredQty { get; set; }
        public decimal AvailableQty { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class RestaurantStockValidationDto
    {
        public bool IsValid { get; set; }
        public List<RestaurantMaterialRequirementDto> Requirements { get; set; } = new();
    }

    public class ValidateRestaurantBillStockDto : EntityDto<Guid>
    {
        public List<FinalizeRestaurantBillLineDto> BillLines { get; set; } = new();
    }

    public class FinalizeRestaurantBillDto
    {
        public Guid OrderId { get; set; }
        public string DateMiti { get; set; }
        public Guid SalesAccountId { get; set; }
        public Guid LedgerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerVatNo { get; set; }
        public string CustomerPhoneNo { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public Guid? PaymentMethodLedgerId { get; set; }
        public bool IsPrint { get; set; }
        public bool ConfirmNegativeStock { get; set; }
        public decimal? TipAmount { get; set; }
        public decimal? CustomerPaidAmount { get; set; }
        public Guid? CashShiftId { get; set; }
        public string ClientRequestId { get; set; }
        public List<RestaurantBillTenderDto> Tenders { get; set; } = new();
        public List<FinalizeRestaurantBillLineDto> BillLines { get; set; } = new();
    }

    public class FinalizeRestaurantBillLineDto
    {
        public Guid OrderItemId { get; set; }
        public decimal Qty { get; set; }
    }

    public class FinalizeRestaurantBillResultDto
    {
        public Guid OrderId { get; set; }
        public Guid SalesMasterId { get; set; }
        public string OrderNo { get; set; }
        public bool IsFullyBilled { get; set; }
        public decimal RemainingGrandTotal { get; set; }
        public decimal BillAmount { get; set; }
        public decimal TipAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public decimal CustomerPaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public List<RestaurantMaterialRequirementDto> StockWarnings { get; set; } = new();
    }

    public class ApplyRestaurantOrderDiscountDto
    {
        public Guid OrderId { get; set; }
        public decimal DiscountAmount { get; set; }
        public string ApprovalPin { get; set; }
        public string ApprovalNote { get; set; }
    }

    public class TransferRestaurantTableDto
    {
        public Guid OrderId { get; set; }
        public Guid NewTableId { get; set; }
    }

    public class MergeRestaurantOrdersDto
    {
        public Guid TargetOrderId { get; set; }
        public List<Guid> SourceOrderIds { get; set; } = new();
    }

    public class SplitRestaurantOrderDto
    {
        public Guid SourceOrderId { get; set; }
        public Guid? NewTableId { get; set; }
        public List<Guid> OrderItemIds { get; set; } = new();
    }

    public class UploadRestaurantOrderDto
    {
        public Guid? DeviceId { get; set; }
        public string ClientRequestId { get; set; }
        public string PayloadHash { get; set; }
        public CreateOrEditRestaurantOrderDto Order { get; set; }
    }

    public class RestaurantReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? TableId { get; set; }
        public long? WaiterUserId { get; set; }
        public Guid? CategoryId { get; set; }
    }

    public class RestaurantMaterialConsumptionReportDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
    }

    public class RestaurantPosSalesSummaryDto
    {
        public int OrderCount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AverageBill { get; set; }
    }

    public class RestaurantItemSalesReportDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Qty { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class RestaurantTableSalesReportDto
    {
        public Guid? TableId { get; set; }
        public string TableName { get; set; }
        public int OrderCount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class RestaurantWaiterSalesReportDto
    {
        public long? WaiterUserId { get; set; }
        public string WaiterName { get; set; }
        public int OrderCount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class RestaurantDailySalesSummaryReportDto
    {
        public DateTime Date { get; set; }
        public string OutletName { get; set; }
        public string TableName { get; set; }
        public long? UserId { get; set; }
        public string UserName { get; set; }
        public int OrderCount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AverageBill { get; set; }
    }

    public class RestaurantTicketStatusReportDto
    {
        public string TicketNo { get; set; }
        public string OrderNo { get; set; }
        public string TicketType { get; set; }
        public string Status { get; set; }
        public string StationName { get; set; }
        public string TableName { get; set; }
        public string WaiterName { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int PrintCount { get; set; }
        public int ItemCount { get; set; }
        public decimal Qty { get; set; }
        public string CancelReason { get; set; }
    }

    public class RestaurantItemMarginReportDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Qty { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal CostAmount { get; set; }
        public decimal MarginAmount { get; set; }
        public decimal MarginPercent { get; set; }
    }

    public class RestaurantWaiterPerformanceReportDto
    {
        public long? WaiterUserId { get; set; }
        public string WaiterName { get; set; }
        public int OrderCount { get; set; }
        public int ItemCount { get; set; }
        public int GuestCount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AverageBill { get; set; }
    }

    public class RestaurantTableTurnoverReportDto
    {
        public Guid? TableId { get; set; }
        public string TableName { get; set; }
        public int SessionCount { get; set; }
        public int OrderCount { get; set; }
        public int GuestCount { get; set; }
        public decimal TotalMinutes { get; set; }
        public decimal AverageMinutes { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AverageBill { get; set; }
    }

    public class RestaurantVoidAuditReportDto
    {
        public DateTime Date { get; set; }
        public string OrderNo { get; set; }
        public string TicketNo { get; set; }
        public string TableName { get; set; }
        public string WaiterName { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }

    public class RestaurantDiscountReportDto
    {
        public DateTime Date { get; set; }
        public string OrderNo { get; set; }
        public string UserName { get; set; }
        public string TableName { get; set; }
        public string CustomerName { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal OrderDiscountAmount { get; set; }
        public decimal ItemDiscountAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public string Reason { get; set; }
    }

    public class RestaurantSettlementReportDto
    {
        public int PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; }
        public int OrderCount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
    }
}
