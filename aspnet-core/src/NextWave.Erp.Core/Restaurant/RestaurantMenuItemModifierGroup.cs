using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantMenuItemModifierGroup")]
    public class RestaurantMenuItemModifierGroup : Entity<Guid>, IMayHaveTenant
    {
        public Guid MenuItemId { get; set; }
        [ForeignKey("MenuItemId")] public RestaurantMenuItem MenuItemFk { get; set; }
        public Guid ModifierGroupId { get; set; }
        [ForeignKey("ModifierGroupId")] public RestaurantModifierGroup ModifierGroupFk { get; set; }
        public int SortOrder { get; set; }
        public int? TenantId { get; set; }
    }
}
