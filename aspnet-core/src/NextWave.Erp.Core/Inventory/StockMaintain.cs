using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_StockMaintains")]
    public class StockMaintain : Entity<Guid>, IMayHaveTenant
    {
        public virtual Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public virtual Guid UnitId { get; set; }
        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public virtual Guid FinancialYearId { get; set; }
        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal InwardQty { get; set; }
        public decimal OutwardQty { get; set; }
        public decimal OpeningRate { get; set; }
        public decimal InwardRate { get; set; }
        public decimal OutwardRate { get; set; }
        public decimal OpeningAmt { get; set; }
        public decimal InwardAmt { get; set; }
        public decimal OutwardAmt { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public int? TenantId { get; set; }
    }
}
