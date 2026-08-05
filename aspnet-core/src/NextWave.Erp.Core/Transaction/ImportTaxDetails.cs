using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Purchase;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{

    [Table("tbl_ImportTaxDetails")]
    public class ImportTaxDetails : Entity<Guid>, IMayHaveTenant
    {
        public virtual Guid EffectLedgerId { get; set; }

        public virtual Guid AccountLedgerId { get; set; }
        [ForeignKey("AccountLedgerId")] public AccountLedger AccountLedgerFk { get; set; }
        public decimal Amount { get; set; }
        public decimal? Percent { get; set; }
        public virtual Guid ImportTaxMasterId { get; set; }
        [ForeignKey("ImportTaxMasterId")] public ImportTaxMaster ImportTaxMasterFk { get; set; }

        public virtual Guid PurchaseDetailId { get; set; }
        [ForeignKey("PurchaseDetailId")] public PurchaseDetail PurchaseDetailFk { get; set; }
        public int? TenantId { get; set; }
    }
}
