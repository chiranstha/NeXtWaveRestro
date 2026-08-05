using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantAggregatorPayout")]
    public class RestaurantAggregatorPayout : Entity<Guid>, IMayHaveTenant
    {
        public Guid? ChannelId { get; set; }
        [ForeignKey("ChannelId")] public RestaurantChannel ChannelFk { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        [StringLength(120)] public string ExternalPayoutId { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal NetPaidAmount { get; set; }
        [StringLength(500)] public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
