using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_LedgerPosting")]
    public sealed class LedgerPosting : Entity<Guid>, IMayHaveTenant
    {
        public int VoucherNumbering { get; set; }
        public int? TenantId { get; set; }
        public DateTime Date { get; set; }
        [StringLength(50)] public string DateMiti { get; set; }

        public Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }

        [StringLength(50)] public string VoucherNo { get; set; }

        [StringLength(50)] public string VendorVoucherNo { get; set; } = "";

        public Guid LedgerId { get; set; }
        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public string DetailIds { get; set; } = "";

        public Guid DetailId { get; set; }
        public Guid? MasterId { get; set; }

        public decimal Debit { get; set; }
        public Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public decimal Credit { get; set; }
        [StringLength(500)] public string InvoiceNo { get; set; }

        public decimal? PostingNumber { get; set; }

    }
}
