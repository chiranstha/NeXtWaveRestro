using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_PartyBalance")]

    public class PartyBalance : Entity<Guid>, IMayHaveTenant
    {
        public virtual int VoucherNumbering { get; set; }
        public virtual DateTime Date { get; set; }
        public virtual Guid LedgerId { get; set; }
        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }
        public virtual Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }
        public virtual Guid? VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }
        [StringLength(500)] public virtual string VoucherNo { get; set; }
        public virtual Guid? AgainstVoucherTypeId { get; set; }
        [StringLength(500)] public virtual string AgainstVoucherNo { get; set; }
        [StringLength(500)] public virtual string InvoiceNo { get; set; }
        [StringLength(500)] public virtual string AgainstInvoiceNo { get; set; }
        [StringLength(500)] public virtual string ReferenceType { get; set; }
        public virtual decimal Debit { get; set; }
        public virtual decimal Credit { get; set; }
        public virtual int CreditPeriod { get; set; }
        public virtual bool IsAgainst { get; set; }
        //      public virtual int ExchangeRateId { get; set; }
        public virtual Guid BranchId { get; set; }
        [ForeignKey("BranchId")] public Branch BranchFk { get; set; }
        public virtual Guid MasterVoucherTypeId { get; set; }
        public virtual Guid MasterId { get; set; }
        public virtual Guid DetailId { get; set; }
        [StringLength(500)] public virtual string MasterVoucherNo { get; set; }

        public virtual int? TenantId { get; set; } // Nullable to allow for tenant-specific data

        // New fields for efficient aging reporting
        [NotMapped]
        public virtual decimal Balance => Debit - Credit;

        [NotMapped]
        public virtual DateTime DueDate => Date.AddDays(CreditPeriod);

        [NotMapped]
        public virtual int AgingDays { get; private set; } // Changed to private setter for better encapsulation

        [NotMapped]
        public virtual bool IsProcessed { get; set; }

        [NotMapped]
        public virtual int? AgingBucket { get; private set; } // Changed to private setter for better encapsulation

        // Optimized method for calculating aging days and bucket
        public void CalculateAgingDays(DateTime reportDate)
        {
            if (ReferenceType is "New" or "OnAccount")
            {
                // Use TimeSpan.TotalDays for better precision and avoid unnecessary casting
                AgingDays = Math.Max(0, (int)(reportDate - DueDate).TotalDays);

                // Use a switch expression for better performance and readability
                AgingBucket = AgingDays switch
                {
                    <= 30 => 1,  // 0-30 days
                    <= 60 => 2,  // 31-60 days
                    <= 90 => 3,  // 61-90 days
                    <= 180 => 4, // 91-180 days
                    _ => 5       // > 180 days
                };
            }
        }

    }
}