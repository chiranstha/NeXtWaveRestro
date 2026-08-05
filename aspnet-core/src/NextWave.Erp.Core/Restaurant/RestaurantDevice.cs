using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantDevice")]
    public class RestaurantDevice : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(100)] public string DeviceCode { get; set; }
        [StringLength(150)] public string Name { get; set; }
        public long? UserId { get; set; }
        public RestaurantDeviceStatus Status { get; set; } = RestaurantDeviceStatus.Active;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public DateTime? LastSeenAt { get; set; }
        public long LastPulledSeq { get; set; }
        public long LastAcknowledgedSeq { get; set; }
        public DateTime? LastSyncAt { get; set; }
        [StringLength(1000)] public string LastSyncError { get; set; }
        public bool HasConflict { get; set; }
        public int? TenantId { get; set; }
    }
}
