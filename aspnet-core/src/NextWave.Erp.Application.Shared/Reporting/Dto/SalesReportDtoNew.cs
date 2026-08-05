using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Reporting.Dto
{
    public class SalesReportDtoNew
    {
        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public string VoucherNo { get; set; }
        public string ReturnVoucherNo { get; set; }
        public string PartyName { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Pan { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal NonTaxableAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid MasterId { get; set; }
    }
}
