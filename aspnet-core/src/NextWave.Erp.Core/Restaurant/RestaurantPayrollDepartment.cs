using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant;

[Table("tbl_RestaurantPayrollDepartment")]
public class RestaurantPayrollDepartment : Entity<Guid>, IMustHaveTenant
{
    public int TenantId { get; set; }
    [Required, StringLength(80)] public string Name { get; set; }
    [StringLength(300)] public string Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<RestaurantPayrollJobRole> JobRoles { get; set; } = new List<RestaurantPayrollJobRole>();
}
