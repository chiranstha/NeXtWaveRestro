using Abp.Application.Services.Dto;
using NextWave.Erp.Purchase.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class GetSalesMasterExportDto : EntityDto
    {
        public string VoucherNo { get; set; }
        public string DateMiti { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal BillDiscount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string LedgerName { get; set; }
        public string PanNo { get; set; }
        public List<ProductDetailsForExcelExport> Details { get; set; }
    }
}
