using Abp.Application.Services.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Sales.Dtos
{
    public class GetSalesMasterExportWithProductDto : EntityDto
    {
        public string VoucherNo { get; set; }
        public string DateMiti { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal AdditionalCost { get; set; }

        public decimal BillDiscount { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal? TotalAmount { get; set; }

        public decimal TaxableAmount { get; set; }

        public string LedgerName { get; set; }
        public string PanNo { get; set; }
        public List<SalesProductExcelDto> Details { get; set; }
    }

    public class SalesProductExcelDto
    {
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Unit { get; set; }
    }
}
