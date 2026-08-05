using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Pdf
{

    public class PdfForSalesReturnModel
    {
        public string BranchName { get; set; }
        public string BranchContactNo { get; set; }
        //    public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public bool DebitOrCreditNote { get; set; }
        public string Pan { get; set; }
        public string? SalesVoucherNo { get; set; }
        //    public DateTime PrintedDateTime { get; set; }

        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        //    public string CustomerPhone { get; set; }
        public string CustomerPan { get; set; }
        public SalesType SalesType { get; set; }
        public string Address { get; set; }
        public string OrderNo { get; set; }
        public string LedgerName { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? Discount { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? GrandTotal { get; set; }
        public string Description { get; set; }
        public string ApprovedBy { get; set; }
        public string ReceivedBy { get; set; }
        public byte[]? Logo1 { get; set; }
        public byte[]? Logo2 { get; set; }
        public ReturnType ReturnType { get; set; }


        public List<PdfForSalesReturnDetailModel> SalesReturnDetail { get; set; }
    }
}
