using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.GeneralSetting
{
    [Table("tbl_FinancialYearSelect")]
    public class FinancialYearSelect : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual long UserId { get; set; }

        [ForeignKey("UserId")] public User UserFk { get; set; }

        public virtual Guid FinancialYearId { get; set; }

        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }
    }
}
