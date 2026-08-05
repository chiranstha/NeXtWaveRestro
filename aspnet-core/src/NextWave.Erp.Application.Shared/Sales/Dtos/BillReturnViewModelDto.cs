using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class BillReturnViewModelDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string SellerPan { get; set; }
        public string BuyerPan { get; set; }
        public string FiscalYear { get; set; }
        public string BuyerName { get; set; }
        public string RefInvoiceNumber { get; set; }
        public string CreditNoteNumber { get; set; }
        public string CreditNoteDate { get; set; }
        public string ReasonForReturn { get; set; }
        public decimal? TotalSales { get; set; }
        public decimal? TaxableSalesVat { get; set; }
        public decimal? Vat { get; set; }
        public decimal? ExcisableAmount { get; set; }
        public decimal? Excise { get; set; }
        public decimal? TaxableSalesHst { get; set; }
        public decimal? Hst { get; set; }
        public decimal? AmountForEsf { get; set; }
        public decimal? Esf { get; set; }
        public decimal? ExportSales { get; set; }
        public decimal? TaxExemptedSales { get; set; }
        public bool Isrealtime { get; set; }
        public DateTime DatetimeClient { get; set; }
    }
}
