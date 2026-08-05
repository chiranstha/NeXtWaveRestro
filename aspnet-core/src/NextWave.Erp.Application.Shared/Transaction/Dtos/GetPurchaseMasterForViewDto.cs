using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPurchaseMasterForViewDto : EntityDto<Guid>
    {
        public string LrNo;
        public string VoucherNo { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public string VendorInvoiceNo { get; set; }
        //public DateTime? VendorInvoiceDate { get; set; }
        public string Narration { get; set; }
        public decimal? TotalTax { get; set; }

        public decimal? TotalAmount { get; set; }

        public decimal? BillDiscount { get; set; }

        public decimal? GrandTotal { get; set; }
        public string LedgerName { get; set; }
    }
}
