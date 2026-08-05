using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class SalesReportDtoMaster
    {
        public decimal GrandTotal { get; set; }

        public decimal TaxableAmount { get; set; }
        public decimal NonTaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<SalesReportDtoNew> Details { get; set; }
    }
}
