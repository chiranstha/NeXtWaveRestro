using Abp.Domain.Entities;
using NextWave.Erp.Sales;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantBillPayment")]
    public class RestaurantBillPayment : Entity<Guid>, IMayHaveTenant
    {
        public Guid OrderId { get; set; }
        [ForeignKey("OrderId")] public RestaurantOrder OrderFk { get; set; }

        public Guid SalesMasterId { get; set; }
        [ForeignKey("SalesMasterId")] public SalesMaster SalesMasterFk { get; set; }

        public decimal BillAmount { get; set; }
        public decimal TipAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public decimal CustomerPaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
