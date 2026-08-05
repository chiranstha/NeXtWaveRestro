using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_AdditionalCost")]
    public class AdditionalCost : Entity<Guid>, IMayHaveTenant
    {
        public string VoucherNo { get; set; }
        public virtual int VoucherNumbering { get; set; }
        public virtual Guid LedgerId { get; set; }
        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public Guid CashOrBankId { get; set; }

        public virtual Guid BranchId { get; set; }
        [ForeignKey("BranchId")] public Branch BranchFk { get; set; }

        public Guid FinancialYearId { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }

        public int? TenantId { get; set; }
    }
}
