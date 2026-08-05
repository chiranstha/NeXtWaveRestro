using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Pdf
{
    public class PdfForSalesPosDetailModel
    {
        public int SlNo { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Amount { get; set; }
        public string HsCode { get; set; }
    }
}
