using Abp.Domain.Entities;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_PurchaseOrderDetails")]
    public class PurchaseOrderDetails : Entity<Guid>, IMayHaveTenant
    {
        public virtual decimal Qty { get; set; }

        public virtual decimal Rate { get; set; }

        public virtual decimal Amount { get; set; }


        public virtual string ProductCode { get; set; }
        public bool IsDeleted { get; set; } = false;

        public virtual Guid PurchaseOrderMasterId { get; set; }

        [ForeignKey("PurchaseOrderMasterId")] public PurchaseOrderMaster PurchaseOrderMasterFk { get; set; }

        public virtual Guid ProductId { get; set; }

        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public virtual Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public int? TenantId { get; set; }
    }
}
