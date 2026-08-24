using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant;

[Table("tbl_RestaurantPayrollRun")]
public class RestaurantPayrollRun : Entity<Guid>, IMustHaveTenant
{
    public int TenantId { get; set; }
    [Required, StringLength(40)] public string RunNumber { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public RestaurantPayrollRunStatus Status { get; set; }
    public decimal TipsPool { get; set; }
    public decimal ServiceChargePool { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal TotalNet { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTime? PaidAt { get; set; }
    public long? PaidByUserId { get; set; }
}
