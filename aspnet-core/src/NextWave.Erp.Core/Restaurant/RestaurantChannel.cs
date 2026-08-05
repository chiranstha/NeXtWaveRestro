using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantChannel")]
    public class RestaurantChannel : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(100)] public string Name { get; set; }
        public RestaurantChannelType ChannelType { get; set; }
        public RestaurantChannelProvider Provider { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal DefaultPriceMarkupPercent { get; set; }
        public int SortOrder { get; set; }
        public bool IsOnline { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
