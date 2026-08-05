using Abp.Domain.Entities;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_PurchaseProductCancelDetail")]
    public class PurchaseProductCancelDetail : Entity<Guid>, IMayHaveTenant
    {
        public virtual Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public decimal Qty { get; set; }

        public virtual Guid PurchaseOrderDetailId { get; set; }
        [ForeignKey("PurchaseOrderDetailId")] public PurchaseOrderDetails PurchaseOrderDetailFk { get; set; }

        public virtual Guid PurchaseProductCancelMasterId { get; set; }

        [ForeignKey("PurchaseProductCancelMasterId")]
        public PurchaseProductCancelMaster PurchaseProductCancelMasterFk { get; set; }

        public int? TenantId { get; set; }
    }
}
