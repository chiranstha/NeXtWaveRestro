using Abp.Domain.Entities;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_PurchaseReturnDetails")]
    public class PurchaseReturnDetail : Entity<Guid>, IMayHaveTenant
    {
        public virtual decimal Qty { get; set; }

        public virtual decimal Rate { get; set; }

        public virtual decimal Discount { get; set; }
        public virtual decimal DiscountPer { get; set; }

        public virtual decimal TaxAmount { get; set; }

        public virtual decimal GrossAmount { get; set; }

        public virtual decimal NetAmount { get; set; }

        public virtual decimal Amount { get; set; }

        [StringLength(100)]
        public virtual string ProductCode { get; set; }

        public virtual Guid PurchaseReturnId { get; set; }
        [ForeignKey("PurchaseReturnId")] public PurchaseReturn PurchaseReturnFk { get; set; }

        public virtual Guid? PurchaseDetailsId { get; set; }
        [ForeignKey("PurchaseDetailsId")] public PurchaseDetail PurchaseDetailFk { get; set; } //added news

        public virtual Guid ProductId { get; set; }

        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public virtual Guid TaxId { get; set; }

        [ForeignKey("TaxId")] public Tax TaxFk { get; set; }

        public virtual Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }


        public int? TenantId { get; set; }
    }
}
