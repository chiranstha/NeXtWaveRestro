using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantChangeLog")]
    public class RestaurantChangeLog : Entity<Guid>, IMayHaveTenant
    {
        public long Seq { get; set; }
        public RestaurantSyncEntityType EntityType { get; set; }
        public RestaurantSyncOperation Operation { get; set; }
        [StringLength(100)] public string EntityId { get; set; }
        public string PayloadJson { get; set; }
        public Guid? ChangedByDeviceId { get; set; }
        [ForeignKey("ChangedByDeviceId")] public RestaurantDevice ChangedByDeviceFk { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
