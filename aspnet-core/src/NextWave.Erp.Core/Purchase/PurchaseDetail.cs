using Abp.Domain.Entities;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_PurchaseDetails")]
    public class PurchaseDetail : Entity<Guid>, IMayHaveTenant
    {
        public virtual decimal Qty { get; set; }

        public virtual decimal Rate { get; set; }

        public virtual decimal Discount { get; set; }
        public virtual decimal DiscountPercent { get; set; }

        //public decimal? CustomAmount { get; set; }
        //public decimal ImportRate { get; set; }
        public virtual decimal TaxAmount { get; set; }

        public virtual decimal GrossAmount { get; set; }

        public virtual decimal NetAmount { get; set; }

        public virtual decimal Amount { get; set; }


        //public virtual bool IsAllowSerailNo { get; set; }
        public virtual Guid? AgainstDetalId { get; set; }
        public virtual Guid PurchaseMasterId { get; set; }

        [ForeignKey("PurchaseMasterId")] public PurchaseMaster PurchaseMasterFk { get; set; }

        public virtual Guid? PurchaseOrderDetailsId { get; set; }

        [ForeignKey("PurchaseOrderDetailsId")] public PurchaseOrderDetails PurchaseOrderDetailsFk { get; set; }

        public virtual Guid ProductId { get; set; }

        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public virtual Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public virtual Guid TaxId { get; set; }

        [ForeignKey("TaxId")] public Tax TaxFk { get; set; }

        public int? TenantId { get; set; }
    }
}
