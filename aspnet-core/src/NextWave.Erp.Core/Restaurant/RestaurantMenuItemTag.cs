using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantMenuItemTag")]
    public class RestaurantMenuItemTag : Entity<Guid>, IMayHaveTenant
    {
        public Guid MenuItemId { get; set; }
        [ForeignKey("MenuItemId")] public RestaurantMenuItem MenuItemFk { get; set; }
        [StringLength(60)] public string Name { get; set; }
        [StringLength(20)] public string ColorHex { get; set; }
        public int SortOrder { get; set; }
        public int? TenantId { get; set; }
    }
}
