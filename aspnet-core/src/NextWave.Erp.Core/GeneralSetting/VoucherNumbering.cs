using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.GeneralSetting
{
    [Table("tbl_VoucherNumbering")]
    public class VoucherNumbering : Entity<Guid>, IMayHaveTenant
    {
        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public virtual VoucherType VoucherTypeFk { get; set; }
        public virtual int StartingIndex { get; set; }
        public virtual string Prefix { get; set; }
        public virtual string Postfix { get; set; }
        public VoucherGenerateType VoucherGenerateType { get; set; }
        public virtual Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public virtual FinancialYear FinancialYearFk { get; set; }
        public int? TenantId { get; set; }
    }
}
