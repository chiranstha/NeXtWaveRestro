
using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Accounting
{
    [Table("tbl_AccountGroup")]
    public class AccountGroup : Entity<Guid>, IMayHaveTenant
    {
        public virtual string Name { get; set; }
        public virtual string Narration { get; set; }

        public virtual bool IsDefault { get; set; }

        public virtual bool AffectGrossProfit { get; set; }

        public virtual AccountGroupNature Nature { get; set; }


        public virtual Guid? GroupUnder { get; set; }

        [ForeignKey("GroupUnder")] public AccountGroup AccountGroupFk { get; set; }

        public int? TenantId { get; set; }
    }
}
