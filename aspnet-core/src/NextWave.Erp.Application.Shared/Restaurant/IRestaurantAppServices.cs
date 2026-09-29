using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    public interface IRestaurantSetupAppService : IApplicationService
    {
        Task<List<RestaurantAreaDto>> GetAreas();
        Task<Guid> CreateOrEditArea(CreateOrEditRestaurantAreaDto input);
        Task DeleteArea(EntityDto<Guid> input);
        Task<List<RestaurantTableDto>> GetTables(Guid? areaId);
        Task<Guid> CreateOrEditTable(CreateOrEditRestaurantTableDto input);
        Task<List<RestaurantStationDto>> GetStations();
        Task<Guid> CreateOrEditStation(CreateOrEditRestaurantStationDto input);
        Task<List<RestaurantDeviceDto>> GetDevices();
        Task<RestaurantDeviceDto> RegisterDevice(RegisterRestaurantDeviceDto input);
        Task<RestaurantOperationalSettingsDto> GetOperationalSettings();
        Task UpdateOperationalSettings(RestaurantOperationalSettingsDto input);
    }

    public interface IRestaurantMenuAppService : IApplicationService
    {
        Task<RestaurantMenuEditorDataDto> GetMenuEditorData();
        Task<List<UniversalDropdownDto>> GetMenuProducts();
        Task<List<RestaurantMenuCategoryDto>> GetCategories();
        Task<Guid> CreateOrEditCategory(CreateOrEditRestaurantMenuCategoryDto input);
        Task<List<RestaurantMenuItemDto>> GetMenuItems(Guid? categoryId);
        Task<List<RestaurantMenuItemDto>> GetPosMenu(Guid? categoryId, string search);
        Task<Guid> CreateOrEditMenuItem(CreateOrEditRestaurantMenuItemDto input);
        Task<Guid> CreateOrEditVariant(CreateOrEditRestaurantMenuVariantDto input);
        Task DeleteVariant(EntityDto<Guid> input);
        Task<Guid> CreateOrEditModifierGroup(CreateOrEditRestaurantModifierGroupDto input);
        Task<Guid> CreateOrEditModifier(CreateOrEditRestaurantModifierDto input);
        Task SaveMenuItemModifierGroups(SaveRestaurantMenuItemModifierGroupsDto input);
        Task SaveMenuItemTags(SaveRestaurantMenuItemTagsDto input);
        Task SetItemAvailability(SetRestaurantMenuItemAvailabilityDto input);
        Task<List<RestaurantRecipeLineDto>> GetRecipe(Guid productId);
        Task SaveRecipe(SaveRestaurantRecipeDto input);
        Task<RestaurantRecipeCostDto> GetRecipeCost(Guid productId);
    }

    public interface IRestaurantOrderAppService : IApplicationService
    {
        Task<RestaurantOrderDto> GetOrder(Guid id);
        Task<RestaurantOrderDto> GetOrderForPos(Guid id);
        Task<List<RestaurantOrderDto>> GetOpenOrders();
        Task<List<RestaurantOrderDto>> GetOpenOrdersForPos();
        Task<Guid> CreateOrEditOrder(CreateOrEditRestaurantOrderDto input);
        Task ApplyOrderDiscount(ApplyRestaurantOrderDiscountDto input);
        Task SendToKitchen(EntityDto<Guid> input);
        Task CancelItem(EntityDto<Guid> input);
        Task CancelTicket(CancelRestaurantTicketDto input);
        Task VoidOrderItem(VoidRestaurantOrderItemDto input);
        Task<List<RestaurantTicketDto>> GetTicketsForOrder(EntityDto<Guid> input);
        Task<RestaurantTicketDto> GetTicketForPrint(EntityDto<Guid> input);
        Task MarkTicketPrinted(EntityDto<Guid> input);
        Task<RestaurantTicketDto> ReprintTicket(ReprintRestaurantTicketDto input);
        Task TransferTable(TransferRestaurantTableDto input);
        Task<Guid> SplitOrder(SplitRestaurantOrderDto input);
        Task MergeOrders(MergeRestaurantOrdersDto input);
    }

    public interface IRestaurantBillingAppService : IApplicationService
    {
        Task<RestaurantStockValidationDto> ValidateOrderStock(ValidateRestaurantBillStockDto input);
        Task<FinalizeRestaurantBillResultDto> GetBillStatus(string clientRequestId);
        Task<FinalizeRestaurantBillResultDto> FinalizeBill(FinalizeRestaurantBillDto input);
    }

    public interface IRestaurantCashShiftAppService : IApplicationService
    {
        Task<RestaurantCashShiftDto> GetCurrent(string registerName = "Main");
        Task<RestaurantCashShiftDto> Open(OpenRestaurantCashShiftDto input);
        Task<RestaurantCashShiftDto> AddMovement(MoveRestaurantCashDto input);
        Task<RestaurantCashShiftDto> Close(CloseRestaurantCashShiftDto input);
    }

    public interface IRestaurantInventoryAppService : IApplicationService
    {
        Task<List<UniversalDropdownDto>> GetRawMaterials();
        Task<List<UniversalDropdownDto>> GetUnits();
        Task<List<RestaurantSupplierItemMappingDto>> GetSupplierItemMappings(Guid? productId);
        Task<Guid> CreateOrEditSupplierItemMapping(CreateOrEditRestaurantSupplierItemMappingDto input);
        Task DeleteSupplierItemMapping(EntityDto<Guid> input);
        Task<List<RestaurantLowStockSuggestionDto>> GetLowStockSuggestions();
        Task<GenerateDraftPurchaseOrdersResultDto> GenerateDraftPurchaseOrders(GenerateDraftPurchaseOrdersDto input);
        Task<Guid> CreateStockAdjustment(CreateRestaurantStockAdjustmentDto input);
        Task<List<RestaurantStockAdjustmentDto>> GetStockAdjustments(RestaurantReportFilterDto input);
        Task<List<RestaurantConsumptionLedgerDto>> GetConsumptionLedger(RestaurantConsumptionFilterDto input);
        Task<List<RestaurantRecipeCoverageDto>> GetRecipeCoverage(RestaurantReportFilterDto input);
    }

    public interface IRestaurantKdsAppService : IApplicationService
    {
        Task<List<RestaurantTicketDto>> GetOpenTickets(Guid? stationId);
        Task UpdateTicketStatus(UpdateRestaurantTicketStatusDto input);
        Task UpdateTicketItemStatus(UpdateRestaurantTicketItemStatusDto input);
        Task<BulkUpdateRestaurantTicketItemStatusResultDto> UpdateTicketItemStatuses(BulkUpdateRestaurantTicketItemStatusDto input);
    }

    public interface IRestaurantSyncAppService : IApplicationService
    {
        Task<RestaurantDeviceDto> RegisterDevice(RegisterRestaurantDeviceDto input);
        Task RegisterPushToken(RegisterRestaurantPushTokenDto input);
        Task<PullRestaurantChangesResultDto> PullChanges(PullRestaurantChangesDto input);
        Task<Guid> UploadOrder(UploadRestaurantOrderDto input);
        Task<UploadRestaurantSyncBatchResultDto> UploadBatch(UploadRestaurantSyncBatchDto input);
        Task AcknowledgeChanges(AcknowledgeRestaurantChangesDto input);
    }

    public interface IRestaurantChannelAppService : IApplicationService
    {
        Task<List<RestaurantChannelDto>> GetChannels();
        Task<Guid> CreateOrEditChannel(CreateOrEditRestaurantChannelDto input);
        Task<List<RestaurantChannelAccountDto>> GetChannelAccounts(Guid? channelId);
        Task<Guid> CreateOrEditChannelAccount(CreateOrEditRestaurantChannelAccountDto input);
        Task<List<RestaurantChannelItemDto>> GetChannelItems(Guid? channelId);
        Task<Guid> SaveChannelItem(SaveRestaurantChannelItemDto input);
        Task<List<RestaurantMenuSyncLogDto>> QueueMenuPublish(Guid channelId);
        Task<RestaurantAggregatorOrderDto> IngestAggregatorOrder(IngestRestaurantAggregatorOrderDto input);
        Task<RestaurantAggregatorOrderDto> AcceptAggregatorOrder(AcceptRestaurantAggregatorOrderDto input);
        Task UpdateAggregatorOrderStatus(UpdateRestaurantAggregatorOrderStatusDto input);
        Task<List<RestaurantAggregatorOrderDto>> GetAggregatorOrders(RestaurantReportFilterDto input);
        Task<RestaurantAggregatorPayoutDto> ImportPayout(ImportRestaurantAggregatorPayoutDto input);
        Task<List<RestaurantAggregatorPayoutDto>> GetPayouts(RestaurantReportFilterDto input);
    }

    public interface IRestaurantCustomerOrderingAppService : IApplicationService
    {
        Task<RestaurantCustomerMenuDto> GetMenu(RestaurantCustomerMenuRequestDto input);
        Task<RestaurantCustomerQuoteDto> Quote(QuoteRestaurantCustomerOrderDto input);
        Task<CreateRestaurantCustomerOrderResultDto> CreateOrder(CreateRestaurantCustomerOrderDto input);
        Task<RestaurantCustomerOrderStatusDto> GetOrderStatus(GetRestaurantCustomerOrderStatusDto input);
    }

    public interface IRestaurantGuestOperationsAppService : IApplicationService
    {
        Task<RestaurantTableQrDto> GenerateTableQr(EntityDto<Guid> input);
        Task RevokeTableQr(EntityDto<Guid> input);
        Task<List<RestaurantGuestOrderQueueDto>> GetPendingGuestOrders();
        Task ReviewGuestOrder(ReviewRestaurantGuestOrderDto input);
        Task OpenTableSession(EntityDto<Guid> input);
        Task CloseTableSession(EntityDto<Guid> input);
        Task<List<RestaurantReservationDto>> GetReservations(DateTime? from, DateTime? to);
        Task<Guid> AddWalkIn(CreateRestaurantWalkInDto input);
        Task UpdateReservation(UpdateRestaurantReservationDto input);
        Task<RestaurantPrintJobDto> ClaimPrintJob(ClaimRestaurantPrintJobDto input);
        Task<List<RestaurantPrintJobDto>> GetPrintJobs();
        Task<RestaurantPrintJobDto> ReportPrintJob(ReportRestaurantPrintJobDto input);
        Task RetryPrintJob(RetryRestaurantPrintJobDto input);
    }

    public interface IRestaurantReservationPublicAppService : IApplicationService
    {
        Task<RequestRestaurantReservationOtpResultDto> RequestOtp(RequestRestaurantReservationOtpDto input);
        Task<CreateRestaurantReservationResultDto> CreateReservation(CreateRestaurantReservationRequestDto input);
        Task<RestaurantReservationStatusDto> GetStatus(GetRestaurantReservationStatusDto input);
    }

    public interface IRestaurantReportsAppService : IApplicationService
    {
        Task<RestaurantPosSalesSummaryDto> GetPosSalesSummary(RestaurantReportFilterDto input);
        Task<List<RestaurantMaterialConsumptionReportDto>> GetMaterialConsumption(RestaurantReportFilterDto input);
        Task<List<RestaurantItemSalesReportDto>> GetItemSales(RestaurantReportFilterDto input);
        Task<List<RestaurantTableSalesReportDto>> GetTableSales(RestaurantReportFilterDto input);
        Task<List<RestaurantWaiterSalesReportDto>> GetWaiterSales(RestaurantReportFilterDto input);
        Task<List<RestaurantDailySalesSummaryReportDto>> GetDailySalesSummary(RestaurantReportFilterDto input);
        Task<List<RestaurantTicketStatusReportDto>> GetKotBotStatus(RestaurantReportFilterDto input);
        Task<List<RestaurantItemMarginReportDto>> GetItemSalesWithMargin(RestaurantReportFilterDto input);
        Task<List<RestaurantWaiterPerformanceReportDto>> GetWaiterPerformance(RestaurantReportFilterDto input);
        Task<List<RestaurantTableTurnoverReportDto>> GetTableTurnover(RestaurantReportFilterDto input);
        Task<List<RestaurantVoidAuditReportDto>> GetVoidCancelledAudit(RestaurantReportFilterDto input);
        Task<List<RestaurantDiscountReportDto>> GetDiscountReport(RestaurantReportFilterDto input);
        Task<List<RestaurantSettlementReportDto>> GetSettlementReport(RestaurantReportFilterDto input);
        Task<List<RestaurantRecipeCostingReportDto>> GetRecipeCosting(RestaurantReportFilterDto input);
        Task<List<RestaurantFoodCostingReportDto>> GetFoodCosting(RestaurantReportFilterDto input);
        Task<List<RestaurantWastageReportDto>> GetWastageReport(RestaurantReportFilterDto input);
        Task<List<RestaurantLowStockSuggestionDto>> GetLowStockReport(RestaurantReportFilterDto input);
        Task<RestaurantPayrollReportBundleDto> GetPayrollReport(RestaurantReportFilterDto input);
    }
}
