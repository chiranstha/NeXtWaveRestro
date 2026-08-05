using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class TemporaryUnitsForFinanceDto
    {
        public decimal Qty { get; set; }
        public Guid UnitId { get; set; }
        public decimal Rate { get; set; }
    }
}
