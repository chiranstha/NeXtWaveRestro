using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_VoucherPhotos")]
    public class VoucherPhotos : Entity<Guid>, IMayHaveTenant
    {
        public virtual string VoucherNo { get; set; }
        public virtual int VoucherNumbering { get; set; }
        public virtual string ChangedFileName { get; set; }
        public virtual byte[] Image { get; set; }
        public virtual string FileName { get; set; }
        public virtual string FileType { get; set; }
        public virtual Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }


        public int? TenantId { get; set; }
    }
}
