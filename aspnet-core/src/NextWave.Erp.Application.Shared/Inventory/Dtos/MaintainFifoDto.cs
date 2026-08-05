using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class MaintainFifoDto
    {
        public string DateMiti { get; set; }
        public DateTime Date { get; set; }
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public bool IsIn { get; set; }
    }
}
