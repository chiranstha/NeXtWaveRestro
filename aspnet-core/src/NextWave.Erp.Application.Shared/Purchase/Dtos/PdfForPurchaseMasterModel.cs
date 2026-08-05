using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{

    public class PdfForPurchaseMasterModel
    {
        public string BranchName { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public int Tin { get; set; }

        public string VoucherTypeName { get; set; }

        //new added start
        public string BranchAddress { get; set; }
        public string BranchContact { get; set; }

        public string Pan { get; set; }

        //new added end
        public string VendorInvoiceNo { get; set; }
        public string OrderNo { get; set; }
        public string LedgerName { get; set; }
        public List<PdfForPurchaseMasterDetailModel> PurchaseDetail { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? BillDiscount { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? GrandTotal { get; set; }
        public string Description { get; set; }
        public string ApprovedBy { get; set; }
        public string ReceivedBy { get; set; }
        public byte[] Logo1 { get; set; }
        public byte[] Logo2 { get; set; }
        public string BranchPhone { get; set; }
        public string BranchPan { get; set; }
    }
}
