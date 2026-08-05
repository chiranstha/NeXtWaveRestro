using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_JournalDetails")]
    public class JournalDetail : Entity<Guid>, IMayHaveTenant
    {
        public virtual decimal Credit { get; set; }

        public virtual decimal Debit { get; set; }

        public virtual string ChequeNo { get; set; }

        public virtual string ChequeMiti { get; set; }
        public virtual DateTime? ChequeDate { get; set; }

        public virtual Guid JournalMasterId { get; set; }

        [ForeignKey("JournalMasterId")] public JournalMaster JournalMasterFk { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public int? TenantId { get; set; }
    }
}