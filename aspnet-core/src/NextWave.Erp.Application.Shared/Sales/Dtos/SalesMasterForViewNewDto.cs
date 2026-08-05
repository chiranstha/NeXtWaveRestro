using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class SalesMasterForViewNewDto
    {
        public Guid Id { get; set; }
        public string Date { get; set; }
        public string PaymentMethod { get; set; }
        public string VoucherNo { get; set; }
        public string LedgerName { get; set; }
        public Guid VoucherTypeId { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal BillDiscount { get; set; }
        public decimal GrossAmount { get; set; }

        public decimal TaxableAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public List<SalesMasterForViewDetailDto> Details { get; set; }
    }

    public class SalesMasterForViewDetailDto
    {
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public string TaxName { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string UnitName { get; set; }
    }
}
