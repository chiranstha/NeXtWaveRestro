using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantAggregatorOrder")]
    public class RestaurantAggregatorOrder : Entity<Guid>, IMayHaveTenant
    {
        public Guid? ChannelId { get; set; }
        [ForeignKey("ChannelId")] public RestaurantChannel ChannelFk { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        [StringLength(120)] public string ExternalOrderId { get; set; }
        public RestaurantExternalOrderStatus Status { get; set; } = RestaurantExternalOrderStatus.Received;
        [StringLength(200)] public string CustomerName { get; set; }
        [StringLength(50)] public string CustomerPhoneNo { get; set; }
        [StringLength(500)] public string DeliveryAddress { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RestaurantDiscountAmount { get; set; }
        public decimal DeliveryFeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string RawPayloadJson { get; set; }
        public Guid? OrderId { get; set; }
        [ForeignKey("OrderId")] public RestaurantOrder OrderFk { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        [StringLength(500)] public string StatusMessage { get; set; }
        public int? TenantId { get; set; }
    }
}
