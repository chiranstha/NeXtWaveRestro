using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class FinancialYearSelectDto
    {
        public Guid FinancialYearId { get; set; }

        public string FinancialYear { get; set; }

        public bool Active { get; set; }
    }
}
