using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class CreateOrEditSalesReturnMasterDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }
        public Guid SalesAccountId { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BillDiscount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ValueAddedTax { get; set; }
        public decimal GrandTotal { get; set; }
        public string LrNo { get; set; }
        public string TransportationCompany { get; set; }
        public string DateMiti { get; set; }
        public Guid LedgerId { get; set; }
        public bool DebitOrCreditNote { get; set; }
        public Guid? SalesMasterId { get; set; }
        public ReturnType ReturnType { get; set; }
        public Guid ReturnTaxId { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal ReturnTaxAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public InvoiceTypeEnum InvoiceType { get; set; }
        public List<SalesReturnDetailDto> SalesReturnDetail { get; set; }
        public decimal NetAmount { get; set; }
    }
}
