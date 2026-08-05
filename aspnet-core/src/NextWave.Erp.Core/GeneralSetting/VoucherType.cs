using Abp.Domain.Entities;
using NextWave.Erp.ControlPanel;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.GeneralSetting
{
    [Table("tbl_VoucherType")]
    public class VoucherType : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }


        public virtual string Name { get; set; }


        public virtual string TypeOfVoucher { get; set; }

        public virtual int StartIndex { get; set; }


        public virtual string Description { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual bool IsDefault { get; set; }

    }
}
