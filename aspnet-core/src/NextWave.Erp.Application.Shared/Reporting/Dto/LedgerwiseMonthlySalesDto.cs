using System;

namespace NextWave.Erp.Reporting.Dto
{
    public class LedgerwiseMonthlySalesDto
    {
        public Guid Id { get; set; }
        public string LedgerName { get; set; }
        public MonthlySalesReportDto Details { get; set; }
    }
}
