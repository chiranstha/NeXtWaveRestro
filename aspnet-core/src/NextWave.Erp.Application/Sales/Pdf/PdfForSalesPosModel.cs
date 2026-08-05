using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Pdf
{

    public class PdfForSalesPosModel
    {
        public string BranchName { get; set; }
        public string DateMiti { get; set; }
        public string BranchPan { get; set; }
        public string Address { get; set; }
        public string BranchPhone { get; set; }
        public string TermsOfPayment { get; set; }
        public string PrintDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPan { get; set; }
        public string OrderNo { get; set; }

        public string TotalAmountInWord { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string NoOfCopy { get; set; }
        public bool IsInvoice { get; set; }
        public string SalesAdditional { get; set; }
        public decimal LoyaltyAmount { get; set; }
        public bool IsLoyatlyPoint { get; set; }
        public List<PdfForSalesPosDetailModel> SalesInvoiceDetail { get; set; }
    }
}
