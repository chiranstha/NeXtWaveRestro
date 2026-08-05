using Abp.Domain.Entities;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantStockAdjustmentLine")]
    public class RestaurantStockAdjustmentLine : Entity<Guid>, IMayHaveTenant
    {
        public Guid StockAdjustmentId { get; set; }
        [ForeignKey("StockAdjustmentId")] public RestaurantStockAdjustment StockAdjustmentFk { get; set; }

        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public Guid UnitId { get; set; }
        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        [StringLength(300)] public string Reason { get; set; }
        public Guid? StockPostingId { get; set; }
        [ForeignKey("StockPostingId")] public StockPosting StockPostingFk { get; set; }
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
