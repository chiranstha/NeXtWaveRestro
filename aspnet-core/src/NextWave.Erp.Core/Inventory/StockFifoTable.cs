using Abp.Domain.Entities;
using NextWave.Erp.ControlPanel;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_StockFIFOTables")]
    public class StockFifoTable : Entity<Guid>, IMayHaveTenant
    {
        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public virtual Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }
        public virtual Guid UnitId { get; set; }
        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }

        public int? TenantId { get; set; }
    }
}
