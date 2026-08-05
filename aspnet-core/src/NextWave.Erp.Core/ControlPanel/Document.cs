using Abp.Domain.Entities;
using NextWave.Erp.GeneralSetting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.ControlPanel
{
    [Table("tbl_Documents")]
    public class Document : Entity<Guid>, IMayHaveTenant
    {
        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }
        public virtual string VoucherNo { get; set; }

        public virtual string Name { get; set; }
        public byte[] Image { get; set; }

        public int? TenantId { get; set; }
    }
}
