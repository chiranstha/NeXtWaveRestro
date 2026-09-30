using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantPrintRoute")]
    public class RestaurantPrintRoute : Entity<Guid>, IMayHaveTenant
    {
        [Required, StringLength(128)] public string Name { get; set; }
        [Required, StringLength(150)] public string DisplayName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int? TenantId { get; set; }
    }
}
