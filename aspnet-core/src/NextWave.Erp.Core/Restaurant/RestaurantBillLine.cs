using Abp.Domain.Entities;
using NextWave.Erp.Sales;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantBillLine")]
    public class RestaurantBillLine : Entity<Guid>, IMayHaveTenant
    {
        public Guid OrderId { get; set; }
        [ForeignKey("OrderId")] public RestaurantOrder OrderFk { get; set; }

        public Guid OrderItemId { get; set; }
        [ForeignKey("OrderItemId")] public RestaurantOrderItem OrderItemFk { get; set; }

        public Guid SalesMasterId { get; set; }
        [ForeignKey("SalesMasterId")] public SalesMaster SalesMasterFk { get; set; }

        public Guid? SalesDetailId { get; set; }
        [ForeignKey("SalesDetailId")] public SalesDetail SalesDetailFk { get; set; }

        public decimal Qty { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal ModifierTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Amount { get; set; }
        public DateTime BilledAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
