using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantPrintDevice")]
    public class RestaurantPrintDevice : Entity<Guid>, IMayHaveTenant
    {
        [Required, StringLength(120)] public string ClientDeviceId { get; set; }
        [Required, StringLength(150)] public string Name { get; set; }
        [Required, StringLength(24)] public string Platform { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
        public int? TenantId { get; set; }
    }
}
