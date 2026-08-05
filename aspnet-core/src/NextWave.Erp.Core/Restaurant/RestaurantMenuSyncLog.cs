using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantMenuSyncLog")]
    public class RestaurantMenuSyncLog : Entity<Guid>, IMayHaveTenant
    {
        public Guid? ChannelId { get; set; }
        [ForeignKey("ChannelId")] public RestaurantChannel ChannelFk { get; set; }
        public Guid? ChannelItemId { get; set; }
        [ForeignKey("ChannelItemId")] public RestaurantChannelItem ChannelItemFk { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        [StringLength(80)] public string Operation { get; set; }
        public RestaurantChannelSyncStatus Status { get; set; }
        public string RequestJson { get; set; }
        public string ResponseJson { get; set; }
        [StringLength(1000)] public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public int? TenantId { get; set; }
    }
}
