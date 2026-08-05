using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantModifier")]
    public class RestaurantModifier : Entity<Guid>, IMayHaveTenant
    {
        public Guid ModifierGroupId { get; set; }
        [ForeignKey("ModifierGroupId")] public RestaurantModifierGroup ModifierGroupFk { get; set; }
        [StringLength(120)] public string Name { get; set; }
        public decimal PriceDelta { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
