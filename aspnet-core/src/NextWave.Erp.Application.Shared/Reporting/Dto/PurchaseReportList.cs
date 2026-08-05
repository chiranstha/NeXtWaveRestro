using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class PurchaseReportList
    {

        public Guid MasterId { get; set; }
        public string Date { get; set; }
        public string InvoiceNo { get; set; }
        public string LedgerName { get; set; }
        public string Pan { get; set; }
        public decimal NonTaxable { get; set; }
        public decimal Taxable { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public List<PurchaseReportDetailList> PurchaseDetail { get; set; }
    }
    public class PurchaseReportDetailList
    {
        public Guid PurchaseMasterId { get; set; }
        public string Date { get; set; }
        public string InvoiceNo { get; set; }
        public string LedgerName { get; set; }
        public decimal TaxAmount { get; set; }
        public string ProductName { get; set; }
        public Guid ProductId { get; set; }
        public decimal Rate { get; set; }
        public decimal Qty { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Amount { get; set; }
    }
}
