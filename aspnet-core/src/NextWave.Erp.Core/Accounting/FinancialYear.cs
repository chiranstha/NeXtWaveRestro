using Abp.Domain.Entities;
using NextWave.Erp.ControlPanel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting
{
    [Table("tbl_FinancialYear")]
    public class FinancialYear : Entity<Guid>, IMayHaveTenant
    {
        public virtual DateTime FromDate { get; set; }

        public virtual DateTime ToDate { get; set; }


        public virtual string FromMiti { get; set; }


        public virtual string ToMiti { get; set; }

        public virtual bool Status { get; set; }
        public virtual bool IsOldYear { get; set; }

        //public virtual Guid BranchId { get; set; }

        //[ForeignKey("BranchId")] public Branch BranchFk { get; set; }

        public virtual Guid? OldFinancialYearId { get; set; }

        public int? TenantId { get; set; }
        public string Name { get; set; }
    }
}
