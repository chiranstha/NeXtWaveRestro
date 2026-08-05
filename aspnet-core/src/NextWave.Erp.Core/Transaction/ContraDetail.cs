using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_ContraDetails")]
    public class ContraDetail : Entity<Guid>, IMayHaveTenant
    {
        public virtual decimal Amount { get; set; }

        [StringLength(500)] public virtual string ChequeNo { get; set; }
        [StringLength(500)] public virtual string ChequeMiti { get; set; }

        public virtual DateTime? ChequeDate { get; set; }

        public virtual Guid ContraMasterId { get; set; }

        [ForeignKey("ContraMasterId")] public ContraMaster ContraMasterFk { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public int? TenantId { get; set; }
    }
}