using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantChannelAccount")]
    public class RestaurantChannelAccount : Entity<Guid>, IMayHaveTenant
    {
        public Guid ChannelId { get; set; }
        [ForeignKey("ChannelId")] public RestaurantChannel ChannelFk { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        [StringLength(100)] public string ExternalStoreId { get; set; }
        [StringLength(150)] public string DisplayName { get; set; }
        [StringLength(512)] public string ApiBaseUrl { get; set; }
        public string ApiCredentialsJson { get; set; }
        [StringLength(256)] public string WebhookSecret { get; set; }
        public bool IsOnline { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? LastMenuSyncAt { get; set; }
        public DateTime? LastOrderSyncAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
