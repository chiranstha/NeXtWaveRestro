using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantMenuVariant")]
    public class RestaurantMenuVariant : Entity<Guid>, IMayHaveTenant
    {
        public Guid MenuItemId { get; set; }
        [ForeignKey("MenuItemId")] public RestaurantMenuItem MenuItemFk { get; set; }
        [StringLength(100)] public string Name { get; set; }
        public decimal PriceDelta { get; set; }
        public bool IsAbsolutePrice { get; set; }
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
