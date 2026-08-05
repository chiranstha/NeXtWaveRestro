using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_ReceiptDetails")]
    public class ReceiptDetail : Entity<Guid>, IMayHaveTenant
    {
        public virtual decimal Amount { get; set; }


        public virtual string ChequeNo { get; set; }


        public virtual string ChequeMiti { get; set; }
        public virtual DateTime? ChequeDate { get; set; }

        public virtual decimal? Forex { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }


        public virtual Guid ReceiptMasterId { get; set; }

        [ForeignKey("ReceiptMasterId")] public ReceiptMaster ReceiptMasterFk { get; set; }

        public int? TenantId { get; set; }
    }
}
