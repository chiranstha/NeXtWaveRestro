using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class GetPurchaseMasterExcelExportDetailDto : EntityDto<Guid>
    {
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
        public decimal NonTaxable { get; set; }
        public decimal TotalTaxableAmount { get; set; }
        public string LedgerName { get; set; }
        public string LedgerPan { get; set; }
        public string BranchName { get; set; }
        public List<ProductDetailsForExcelExport> Details { get; set; }
    }

    public class ProductDetailsForExcelExport
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}
