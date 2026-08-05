using System;
using System.Collections.Generic;

namespace NextWave.Erp.Purchase.Dtos
{

    public class PdfForPurchaseOrderModelNew
    {
        public string BranchName { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public int Tin { get; set; }

        public string VoucherTypeName { get; set; }

        //new added start
        public string BranchAddress { get; set; }
        public string BranchContact { get; set; }

        public string Pan { get; set; }
        //new added end

        public string OrderNo { get; set; }
        public string LedgerName { get; set; }
        public List<PdfForPurchaseOrderDetailModel> PurchaseOrderDetail { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Description { get; set; }
        public string ApprovedBy { get; set; }
        public string ReceivedBy { get; set; }
        public byte[] Logo1 { get; set; }
        public byte[] Logo2 { get; set; }

        // Added
        public string BranchPhone { get; set; }
        public string InvoiceName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPan { get; set; }
        public string TermsOfPayment { get; set; }
        public string ChalanNo { get; set; }
        public string PrintDate { get; set; }
        public string NoOfCopy { get; set; }
        public string DiscountAmount { get; set; }
        public string TaxableAmount { get; set; }
        public string TaxAmount { get; set; }
        public string GrandTotal { get; set; }
    }
}
