using Abp.Domain.Entities;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales
{
    [Table("tbl_SalesReturnDetails")]
    public class SalesReturnDetail : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public virtual decimal Qty { get; set; }

        public virtual decimal Rate { get; set; }

        public virtual decimal TaxAmount { get; set; }
        public virtual decimal TaxValue { get; set; }
        public virtual decimal Discount { get; set; }
        public virtual decimal DiscountPer { get; set; }
        public virtual decimal GrossAmount { get; set; }
        public virtual decimal NetAmount { get; set; }

        public virtual decimal Amount { get; set; }
        public virtual Guid? SalesDetailId { get; set; }

        [ForeignKey("SalesDetailId")] public SalesDetail SalesDetailFk { get; set; }
        public virtual Guid SalesReturnMasterId { get; set; }

        [ForeignKey("SalesReturnMasterId")] public SalesReturnMaster SalesReturnMasterFk { get; set; }

        public virtual Guid ProductId { get; set; }

        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public virtual Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public virtual Guid? TaxId { get; set; }

        [ForeignKey("TaxId")] public Tax TaxFk { get; set; }
    }
}
