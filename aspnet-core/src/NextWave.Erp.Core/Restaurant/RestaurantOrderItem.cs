using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantOrderItem")]
    public class RestaurantOrderItem : Entity<Guid>, IMayHaveTenant
    {
        public Guid OrderId { get; set; }
        [ForeignKey("OrderId")] public RestaurantOrder OrderFk { get; set; }
        public Guid? MenuItemId { get; set; }
        [ForeignKey("MenuItemId")] public RestaurantMenuItem MenuItemFk { get; set; }
        public Guid? VariantId { get; set; }
        [ForeignKey("VariantId")] public RestaurantMenuVariant VariantFk { get; set; }
        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }
        public Guid UnitId { get; set; }
        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }
        public Guid? StationId { get; set; }
        [ForeignKey("StationId")] public RestaurantStation StationFk { get; set; }
        public Guid TaxId { get; set; }
        [ForeignKey("TaxId")] public Tax TaxFk { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        [StringLength(200)] public string ItemNameSnapshot { get; set; }
        [StringLength(100)] public string VariantNameSnapshot { get; set; }
        public RestaurantStationType? StationTypeSnapshot { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal ModifierTotal { get; set; }
        public int FireSequence { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Amount { get; set; }
        public RestaurantOrderItemStatus Status { get; set; } = RestaurantOrderItemStatus.Draft;
        [StringLength(500)] public string Notes { get; set; }
        [StringLength(500)] public string CancelReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
