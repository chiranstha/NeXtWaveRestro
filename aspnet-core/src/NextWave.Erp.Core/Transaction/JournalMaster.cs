using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_JournalMaster")]
    public class JournalMaster : Entity<Guid>, IMayHaveTenant
    {
        public virtual int VoucherNumbering { get; set; }
        public virtual string VoucherNo { get; set; }

        public virtual string ReferenceNo { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual decimal DebitTotal { get; set; }
        public virtual decimal CreditTotal { get; set; }

        public virtual string Description { get; set; }

        public virtual string DateMiti { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }

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