using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class PurchaseReportMasterDto
    {
        public string FromMiti { get; set; }
        public string ToMiti { get; set; }
        public string PhoneNo { get; set; }
        public string Address { get; set; }
        public string FinancialYear { get; set; }
        public string Pan { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal NonTaxable { get; set; }
        public decimal Taxable { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public List<PurchaseReportList> Details { get; set; }
    }
}
