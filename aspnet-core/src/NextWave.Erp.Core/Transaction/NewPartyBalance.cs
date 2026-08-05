using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_NewPartyBalance")]
    public class NewPartyBalance : Entity<Guid>, IMayHaveTenant
    {
        public DateTime Date { get; set; }
        public DateTime DueDate { get; set; }

        public Guid LedgerId { get; set; }
        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public string VoucherNo { get; set; }
        public int VoucherNumbering { get; set; }

        public Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }

        public string AgainstVoucherNo { get; set; }
        public int AgainstVoucherNumbering { get; set; }
        public Guid? AgainstVoucherTypeId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public bool IsMain { get; set; }
        public bool IsFullySettled { get; set; }
        public bool IsPartiallySettled { get; set; }
        public Guid MasterId { get; set; }
        public Guid DetailId { get; set; }
        public Guid? MasterPartyBalanceId { get; set; }
        [ForeignKey("MasterPartyBalanceId")] public NewPartyBalance NewPartyBalanceFk { get; set; }

        public virtual int? TenantId { get; set; } // Nullable to allow for tenant-specific data

    }
}
