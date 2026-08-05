using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantOrderItemModifier")]
    public class RestaurantOrderItemModifier : Entity<Guid>, IMayHaveTenant
    {
        public Guid OrderItemId { get; set; }
        [ForeignKey("OrderItemId")] public RestaurantOrderItem OrderItemFk { get; set; }
        public Guid ModifierId { get; set; }
        [ForeignKey("ModifierId")] public RestaurantModifier ModifierFk { get; set; }
        [StringLength(120)] public string ModifierNameSnapshot { get; set; }
        public decimal PriceDelta { get; set; }
        public decimal Qty { get; set; } = 1;
        public decimal Amount { get; set; }
        public int? TenantId { get; set; }
    }
}
