using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Pdf
{
    public class PdfForSalesInvoiceModel
    {
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public string BranchPan { get; set; }
        public string Province { get; set; }
        public string Address { get; set; }
        public string BranchPhone { get; set; }

        public string InvoiceName { get; set; }

        //new added
        public string TermsOfPayment { get; set; }
        public string ChalanNo { get; set; }
        public string PrintDate { get; set; }
        public decimal Fright { get; set; }
        public FreightTerm FrightTerms { get; set; }
        public string PrintUser { get; set; }
        public DateTime PrintedDateTime { get; set; }
        public string LrNo { get; set; }

        public string SalesAddition { get; set; }

        public string LedgerBalance { get; internal set; }

        //customer details 
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPan { get; set; }
        public string TransportName { get; set; }
        public string VehicleNumber { get; set; }
        public string VoucherTypeName { get; set; }
        public string OrderNo { get; set; }
        public string LedgerName { get; set; }
        public string PiNumber { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal TotalAmount { get; set; }
        public string Description { get; set; }
        public string ApprovedBy { get; set; }
        public string ReceivedBy { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public byte[] Logo1 { get; set; }
        public byte[] Logo2 { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string NoOfCopy { get; set; }
        public string CompanyEmail { get; internal set; }
        public string WebSite { get; internal set; }
        public List<Guid> AgainstId { get; set; }

        public List<PdfForSalesInvoiceDetailModel> SalesInvoiceDetail { get; set; }
    }
}
