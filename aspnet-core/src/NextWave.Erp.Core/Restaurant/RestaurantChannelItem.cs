using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantChannelItem")]
    public class RestaurantChannelItem : Entity<Guid>, IMayHaveTenant
    {
        public Guid ChannelId { get; set; }
        [ForeignKey("ChannelId")] public RestaurantChannel ChannelFk { get; set; }
        public Guid MenuItemId { get; set; }
        [ForeignKey("MenuItemId")] public RestaurantMenuItem MenuItemFk { get; set; }
        [StringLength(100)] public string ExternalItemId { get; set; }
        [StringLength(100)] public string ExternalSku { get; set; }
        public decimal ChannelPrice { get; set; }
        public bool IsOnline { get; set; } = true;
        public RestaurantChannelSyncStatus SyncStatus { get; set; } = RestaurantChannelSyncStatus.PendingPush;
        public DateTime? LastSyncedAt { get; set; }
        [StringLength(500)] public string LastSyncMessage { get; set; }
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
