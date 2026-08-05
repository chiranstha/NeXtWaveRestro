using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantSyncUpload")]
    public class RestaurantSyncUpload : Entity<Guid>, IMayHaveTenant
    {
        public Guid? DeviceId { get; set; }
        [ForeignKey("DeviceId")] public RestaurantDevice DeviceFk { get; set; }
        [StringLength(100)] public string ClientRequestId { get; set; }
        [StringLength(128)] public string PayloadHash { get; set; }
        public Guid? ServerReferenceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
