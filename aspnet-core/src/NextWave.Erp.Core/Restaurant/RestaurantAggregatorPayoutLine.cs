using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantAggregatorPayoutLine")]
    public class RestaurantAggregatorPayoutLine : Entity<Guid>, IMayHaveTenant
    {
        public Guid PayoutId { get; set; }
        [ForeignKey("PayoutId")] public RestaurantAggregatorPayout PayoutFk { get; set; }
        public Guid? AggregatorOrderId { get; set; }
        [ForeignKey("AggregatorOrderId")] public RestaurantAggregatorOrder AggregatorOrderFk { get; set; }
        [StringLength(120)] public string ExternalOrderId { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RestaurantDiscountAmount { get; set; }
        public decimal DeliveryFeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public RestaurantPayoutMatchStatus MatchStatus { get; set; } = RestaurantPayoutMatchStatus.Pending;
        [StringLength(500)] public string MatchMessage { get; set; }
        public int? TenantId { get; set; }
    }
}
