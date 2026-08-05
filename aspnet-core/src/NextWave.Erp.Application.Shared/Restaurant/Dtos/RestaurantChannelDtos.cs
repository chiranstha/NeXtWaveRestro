using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Restaurant.Dtos
{
    public class RestaurantChannelDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public RestaurantChannelType ChannelType { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal DefaultPriceMarkupPercent { get; set; }
        public int SortOrder { get; set; }
        public bool IsOnline { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrEditRestaurantChannelDto : EntityDto<Guid?>
    {
        public string Name { get; set; }
        public RestaurantChannelType ChannelType { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal DefaultPriceMarkupPercent { get; set; }
        public int SortOrder { get; set; }
        public bool IsOnline { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantChannelAccountDto : EntityDto<Guid>
    {
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public string ExternalStoreId { get; set; }
        public string DisplayName { get; set; }
        public string ApiBaseUrl { get; set; }
        public string ApiCredentialsJson { get; set; }
        public string WebhookSecret { get; set; }
        public bool IsOnline { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastMenuSyncAt { get; set; }
        public DateTime? LastOrderSyncAt { get; set; }
    }

    public class CreateOrEditRestaurantChannelAccountDto : EntityDto<Guid?>
    {
        public Guid ChannelId { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public string ExternalStoreId { get; set; }
        public string DisplayName { get; set; }
        public string ApiBaseUrl { get; set; }
        public string ApiCredentialsJson { get; set; }
        public string WebhookSecret { get; set; }
        public bool IsOnline { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantChannelItemDto : EntityDto<Guid>
    {
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public string ExternalItemId { get; set; }
        public string ExternalSku { get; set; }
        public decimal BasePrice { get; set; }
        public decimal ChannelPrice { get; set; }
        public bool IsOnline { get; set; }
        public RestaurantChannelSyncStatus SyncStatus { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public string LastSyncMessage { get; set; }
    }

    public class SaveRestaurantChannelItemDto : EntityDto<Guid?>
    {
        public Guid ChannelId { get; set; }
        public Guid MenuItemId { get; set; }
        public string ExternalItemId { get; set; }
        public string ExternalSku { get; set; }
        public decimal ChannelPrice { get; set; }
        public bool IsOnline { get; set; } = true;
    }

    public class RestaurantMenuSyncLogDto : EntityDto<Guid>
    {
        public Guid? ChannelId { get; set; }
        public string ChannelName { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public string Operation { get; set; }
        public RestaurantChannelSyncStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class RestaurantAggregatorOrderDto : EntityDto<Guid>
    {
        public Guid? ChannelId { get; set; }
        public string ChannelName { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public string ExternalOrderId { get; set; }
        public RestaurantExternalOrderStatus Status { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhoneNo { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RestaurantDiscountAmount { get; set; }
        public decimal DeliveryFeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public Guid? OrderId { get; set; }
        public string OrderNo { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public string StatusMessage { get; set; }
    }

    public class IngestRestaurantAggregatorOrderDto
    {
        public Guid? ChannelId { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public string ExternalOrderId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhoneNo { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RestaurantDiscountAmount { get; set; }
        public decimal DeliveryFeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string RawPayloadJson { get; set; }
        public List<RestaurantCustomerOrderLineDto> Lines { get; set; } = new();
    }

    public class AcceptRestaurantAggregatorOrderDto
    {
        public Guid AggregatorOrderId { get; set; }
        public List<RestaurantCustomerOrderLineDto> Lines { get; set; } = new();
        public bool SendToKitchen { get; set; } = true;
    }

    public class UpdateRestaurantAggregatorOrderStatusDto
    {
        public Guid AggregatorOrderId { get; set; }
        public RestaurantExternalOrderStatus Status { get; set; }
        public string Message { get; set; }
    }

    public class RestaurantAggregatorPayoutDto : EntityDto<Guid>
    {
        public Guid? ChannelId { get; set; }
        public string ChannelName { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public string ExternalPayoutId { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal NetPaidAmount { get; set; }
        public string Notes { get; set; }
        public List<RestaurantAggregatorPayoutLineDto> Lines { get; set; } = new();
    }

    public class RestaurantAggregatorPayoutLineDto : EntityDto<Guid>
    {
        public Guid PayoutId { get; set; }
        public Guid? AggregatorOrderId { get; set; }
        public string ExternalOrderId { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RestaurantDiscountAmount { get; set; }
        public decimal DeliveryFeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public RestaurantPayoutMatchStatus MatchStatus { get; set; }
        public string MatchMessage { get; set; }
    }

    public class ImportRestaurantAggregatorPayoutDto
    {
        public Guid? ChannelId { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public string ExternalPayoutId { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Notes { get; set; }
        public List<ImportRestaurantAggregatorPayoutLineDto> Lines { get; set; } = new();
    }

    public class ImportRestaurantAggregatorPayoutLineDto
    {
        public string ExternalOrderId { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RestaurantDiscountAmount { get; set; }
        public decimal DeliveryFeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
    }
}
