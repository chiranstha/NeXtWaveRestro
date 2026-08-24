using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant;

[Table("tbl_RestaurantPayrollJobRole")]
public class RestaurantPayrollJobRole : Entity<Guid>, IMustHaveTenant
{
    public int TenantId { get; set; }
    public Guid DepartmentId { get; set; }
    [Required, StringLength(80)] public string Name { get; set; }
    [StringLength(300)] public string Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DepartmentId))]
    public virtual RestaurantPayrollDepartment DepartmentFk { get; set; }
}
