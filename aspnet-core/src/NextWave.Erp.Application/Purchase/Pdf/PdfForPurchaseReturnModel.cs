using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Pdf
{

    public class PdfForPurchaseReturnModel
    {
        public string BranchName { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public int Tin { get; set; }
        //public string VoucherTypeName { get; set; }
        public string Address { get; set; }
        //public string Province { get; set; }
        public string OrderNo { get; set; }

        //new added start
        public string BranchAddress { get; set; }
        public string BranchContact { get; set; }
        public string Pan { get; set; }
        public string PurchaseVoucherNo { get; set; }

        public string CustomerName { get; set; }
        public List<PdfForPurchaseReturnDetailModel> PurchaseReturnDetail { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal? TotalAmount { get; set; }
        public bool DebitOrCreditNote { get; set; }
        public decimal? TaxAmount { get; set; }
        public string Description { get; set; }

        public decimal TotalDiscount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string ApprovedBy { get; set; }
        public string ReceivedBy { get; set; }
        public byte[] Logo1 { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public string BranchContactNo { get; set; }
        public byte[] Logo2 { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPan { get; set; }
    }
}
