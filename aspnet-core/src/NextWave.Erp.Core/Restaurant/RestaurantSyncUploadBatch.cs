using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantSyncUploadBatch")]
    public class RestaurantSyncUploadBatch : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(100)] public string BatchGuid { get; set; }
        public Guid? DeviceId { get; set; }
        [ForeignKey("DeviceId")] public RestaurantDevice DeviceFk { get; set; }
        public RestaurantSyncUploadStatus Status { get; set; } = RestaurantSyncUploadStatus.Pending;
        public int ItemCount { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        [StringLength(1000)] public string ErrorMessage { get; set; }
        public int? TenantId { get; set; }
    }
}
