using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantCashMovement")]
    public class RestaurantCashMovement : Entity<Guid>, IMayHaveTenant
    {
        public Guid CashShiftId { get; set; }
        [ForeignKey(nameof(CashShiftId))] public RestaurantCashShift CashShiftFk { get; set; }
        public bool IsCashIn { get; set; }
        public decimal Amount { get; set; }
        [Required, StringLength(500)] public string Reason { get; set; }
        public long CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
