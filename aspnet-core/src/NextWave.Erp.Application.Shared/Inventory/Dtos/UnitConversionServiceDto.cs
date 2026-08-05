using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class UnitConversionServiceDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Rate { get; set; }
    }
}
