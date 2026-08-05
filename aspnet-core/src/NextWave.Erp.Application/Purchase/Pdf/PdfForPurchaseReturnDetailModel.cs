using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Pdf
{
    public class PdfForPurchaseReturnDetailModel
    {
        public int SlNo { get; set; }
        public string ProductCode { get; set; }
        public string HsCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Discount { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}
