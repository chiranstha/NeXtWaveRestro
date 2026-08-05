using Abp.Domain.Entities;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Accounting
{
    [Table("tbl_AccountLedger")]
    public class AccountLedger : Entity<Guid>, IMayHaveTenant
    {
        [Required] public virtual string Name { get; set; }

        public virtual decimal OpeningBalance { get; set; }

        public virtual bool IsDefault { get; set; } = false;

        public virtual DrOrCr CrOrDr { get; set; }


        public virtual string Narration { get; set; }

        public virtual string Address { get; set; }

        public virtual string Phone { get; set; }

        public virtual string Email { get; set; }

        public virtual int? CreditPeriod { get; set; }

        public virtual decimal? CreditLimit { get; set; }

        public virtual bool IsBillByBill { get; set; }

        public virtual string Pan { get; set; }

        public virtual bool Status { get; set; }
        public virtual bool IsDelete { get; set; }
        public virtual bool IsCompany { get; set; } = false;
        public virtual DateTime? OpeningDate { get; set; }

        public virtual long? UserId { get; set; }
        [ForeignKey("UserId")] public User UserFk { get; set; }

        public virtual long? CreateUserId { get; set; }
        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }

        public virtual long? UpdateUserId { get; set; }
        [ForeignKey("UpdateUserId")] public User UpdateUserFk { get; set; }

        public virtual Guid? ParentId { get; set; }
        [ForeignKey("ParentId")] public AccountLedger AccountLedgerFk { get; set; }

        public virtual Guid AccountGroupId { get; set; }
        [ForeignKey("AccountGroupId")] public AccountGroup AccountGroupFk { get; set; }

        public int? TenantId { get; set; }
    }
}
