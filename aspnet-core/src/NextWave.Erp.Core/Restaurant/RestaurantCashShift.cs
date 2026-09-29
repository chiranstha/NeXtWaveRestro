using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantCashShift")]
    public class RestaurantCashShift : Entity<Guid>, IMayHaveTenant
    {
        [Required, StringLength(100)] public string RegisterName { get; set; }
        public long OpenedByUserId { get; set; }
        public DateTime OpenedAt { get; set; } = DateTime.Now;
        public decimal OpeningCash { get; set; }
        public bool IsClosed { get; set; }
        public long? ClosedByUserId { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal? CountedClosingCash { get; set; }
        public decimal? ExpectedClosingCash { get; set; }
        public decimal? CashVariance { get; set; }
        [StringLength(500)] public string CloseNote { get; set; }
        public int? TenantId { get; set; }
    }
}
