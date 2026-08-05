using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class StockMaintainDto : Entity<Guid>
    {
        public string DateMiti { get; set; }
        public virtual Guid ProductId { get; set; }
        public virtual Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public StockMaintainTypeEnum Type { get; set; }
        public Guid FinancialYearId { get; set; }
    }
}
