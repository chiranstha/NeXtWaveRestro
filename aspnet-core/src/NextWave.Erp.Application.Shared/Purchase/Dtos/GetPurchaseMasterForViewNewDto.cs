using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class GetPurchaseMasterForViewNewDto
    {
        public Guid Id { get; set; }
        public string BranchName { get; set; }
        public Guid BranchId { get; set; }
        public string VoucherNo { get; set; }
        public string Date { get; set; }
        public string LedgerName { get; set; }
        public string VendorInvoiceNo { get; set; }
        public string InvoiceType { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<GetPurchaseInvoiceForViewDetailDto> Details { get; set; }
    }

    public class GetPurchaseInvoiceForViewDetailDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Qty { get; set; }
        public decimal Discount { get; set; }

        public decimal Rate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string TaxName { get; set; }
        public decimal TotalAmount { get; set; }
        public string UnitName { get; set; }
    }
}
