using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Purchase;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_ImportTaxMaster")]
    public class ImportTaxMaster : Entity<Guid>, IMayHaveTenant
    {
        public string DateMiti { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalImportTax { get; set; }
        public virtual string PurchaseVoucherNo { get; set; }
        public virtual Guid BranchId { get; set; }
        [ForeignKey("BranchId")] public Branch BranchFk { get; set; }

        public virtual Guid PurchaseMasterId { get; set; }
        [ForeignKey("PurchaseMasterId")] public PurchaseMaster PurchaseMasterFk { get; set; }

        public virtual Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public int? TenantId { get; set; }
    }
}
