using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class CreateOrEditPurchaseReturnDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public string DateMiti { get; set; }

        public string Description { get; set; }

        public Guid PurchaseAccount { get; set; }

        public decimal TotalTax { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public string LrNo { get; set; }

        public string TransportationCompany { get; set; }

        public string VendorInvoiceNo { get; set; }
        public string VoucherName { get; set; }

        public Guid PurchaseMasterId { get; set; }

        public Guid LedgerId { get; set; }
        public decimal NetAmount { get; set; }
        public InvoiceTypeEnum InvoiceType { get; set; }
        public ReturnType ReturnType { get; set; }

        public bool DebitOrCreditNote { get; set; }
        public Guid ReturnTaxId { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal ReturnTaxAmount { get; set; }
        public List<PurchaseReturnDetailDto> PurchaseReturnDetail { get; set; }
        public decimal TotalTaxableAmount { get; set; }
        public decimal TotalDiscount { get; set; }
    }
}
