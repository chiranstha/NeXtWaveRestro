using System;

namespace NextWave.Erp.Reporting.Dto
{
    public class TaxReportDto
    {
        public string DateMiti { get; set; }
        public string VoucherNo { get; set; }
        public string VendorInvoiceNo { get; set; }
        public string LedgerName { get; set; }
        public string Pan { get; set; }
        public string ProductCategoryName { get; set; } // added
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public string UnitsName { get; set; } // s is added after unit
        public decimal Amount { get; set; }
        public decimal GrossAmount { get; set; } //added
        public decimal Discount { get; set; } //added
        public decimal NetAmount { get; set; } //added
        public decimal NonTaxableAmount { get; set; } //added
        public decimal LocalTaxableAmount { get; set; }
        public decimal LocalTaxAmount { get; set; }       
        public DateTime Date { get; set; }
    }
}
