using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Pdf
{
    public class PdfForSalesInvoiceDetailModel
    {
        public int SlNo { get; set; }
        public string ProductName { get; set; }
        public string HsCode { get; set; }
        public decimal Quantity { get; set; }
        public string AltUnit { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal? AltQty { get; set; }
        public decimal DiscountPer { get; set; }
        public string Packaging { get; set; }
    }
}
