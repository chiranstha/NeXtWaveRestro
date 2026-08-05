using System;
using System.Collections.Generic;

namespace NextWave.Erp.Accounting.Dtos
{
    public class StockForFinancialYearDto
    {
        public Guid ProductId { get; set; }
        public List<TemporaryUnitsForFinanceDto> ProductUnit { get; set; }
    }
}
