using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantPushToken")]
    public class RestaurantPushToken : Entity<Guid>, IMayHaveTenant
    {
        public Guid DeviceId { get; set; }
        [ForeignKey("DeviceId")] public RestaurantDevice DeviceFk { get; set; }
        [StringLength(80)] public string Platform { get; set; }
        [StringLength(512)] public string Token { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public DateTime? LastSeenAt { get; set; }
        public int? TenantId { get; set; }
    }
}
