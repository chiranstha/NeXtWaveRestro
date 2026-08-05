using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_PurchaseProductCancelMaster")]
    public class PurchaseProductCancelMaster : Entity<Guid>, IMayHaveTenant
    {
        public virtual DateTime? Date { get; set; }

        public virtual string DateMiti { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }

        public virtual string VoucherNo { get; set; }

        public virtual Guid PurchaseOrderId { get; set; }

        [ForeignKey("PurchaseOrderId")] public PurchaseOrderMaster PurchaseOrderFk { get; set; }

        public virtual Guid BranchId { get; set; }

        [ForeignKey("BranchId")] public Branch BranchFk { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }
        public virtual Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public int? TenantId { get; set; }
    }
}
