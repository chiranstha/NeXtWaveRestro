using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Transaction.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_ContraMaster")]
    public class ContraMaster : Entity<Guid>, IMayHaveTenant
    {
        public virtual int VoucherNumbering { get; set; }
        public virtual ContraType Type { get; set; }
        public virtual string VoucherNo { get; set; }


        public virtual DateTime Date { get; set; }

        public virtual decimal TotalAmount { get; set; }


        public virtual string Narration { get; set; }


        public virtual string DateMiti { get; set; }
        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }


        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public virtual Guid FinancialYearId { get; set; }

        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public virtual long? CreateUserId { get; set; }
        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }

        public virtual long? UpdateUserId { get; set; }

        [ForeignKey("UpdateUserId")] public User UpdateUserFk { get; set; }
        public int? TenantId { get; set; }

        public virtual decimal? PostingNumbering { get; set; }
    }
}