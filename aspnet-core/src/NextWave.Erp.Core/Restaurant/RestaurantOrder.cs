using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using NextWave.Erp.Sales;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantOrder")]
    public class RestaurantOrder : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(50)] public string OrderNo { get; set; }
        public RestaurantOrderType OrderType { get; set; }
        public RestaurantOrderStatus Status { get; set; } = RestaurantOrderStatus.Draft;
        public RestaurantGuestOrderApprovalStatus? GuestApprovalStatus { get; set; }
        [StringLength(300)] public string GuestRejectionReason { get; set; }
        public Guid? TableId { get; set; }
        [ForeignKey("TableId")] public RestaurantTable TableFk { get; set; }
        public Guid? TableSessionId { get; set; }
        [ForeignKey("TableSessionId")] public RestaurantTableSession TableSessionFk { get; set; }
        public long? WaiterUserId { get; set; }
        public Guid? DeviceId { get; set; }
        [StringLength(100)] public string Source { get; set; }
        [StringLength(100)] public string ClientRequestId { get; set; }
        [StringLength(100)] public string GuestClientRequestId { get; set; }
        [StringLength(100)] public string PosClientRequestId { get; set; }
        [StringLength(64)] public string ClientPayloadHash { get; set; }
        [StringLength(64)] public string GuestStatusTokenHash { get; set; }
        [StringLength(200)] public string CustomerName { get; set; }
        [StringLength(50)] public string CustomerPhoneNo { get; set; }
        [StringLength(500)] public string Notes { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public Guid? SalesMasterId { get; set; }
        [ForeignKey("SalesMasterId")] public SalesMaster SalesMasterFk { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? SentAt { get; set; }
        public DateTime? BilledAt { get; set; }
        public int? TenantId { get; set; }
    }
}
