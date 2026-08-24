using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant;

[Table("tbl_RestaurantPayrollAllowanceHistory")]
public class RestaurantPayrollAllowanceHistory : Entity<Guid>, IMustHaveTenant
{
    public int TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    [StringLength(10)] public string EffectiveFromMiti { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedByUserId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual RestaurantPayrollEmployee EmployeeFk { get; set; }
}
