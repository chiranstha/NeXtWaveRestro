using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class CreateOrEditPurchaseMasterDto : EntityDto<Guid?>
    {
        //public Guid ReferenceId { get; set; }
        //public UpDownType UpDownType { get; set; }
        public string VoucherNo { get; set; }

        public string DateMiti { get; set; }

        public string VendorInvoiceNo { get; set; }

        //public string VendorInvoiceMiti { get; set; }

        public int? CreditPeriod { get; set; }

        public string Narration { get; set; }


        public decimal TotalTax { get; set; }

        public decimal TaxableAmount { get; set; }

        //new added start
        //public string PpdNo { get; set; }

        // new added end
        public decimal TotalAmount { get; set; }

        public decimal BillDiscount { get; set; }

        public decimal GrandTotal { get; set; }

        //public virtual InvoiceTypeEnum InvoiceTypeEnum { get; set; }
        public string LrNo { get; set; }

        //public string TransportationCompany { get; set; }
        public Guid PurchaseAccountId { get; set; }
        public Guid LedgerId { get; set; }
        //public Guid? CustomLedgerId { get; set; }
        public PurchaseModeType AgainstId { get; set; }

        public Guid? OrderOrReceiptId { get; set; }
        //public Guid BranchId { get; set; }
        public List<PurchaseDetailDto> PurchaseDetail { get; set; }
        //public decimal? TotalCustomAmount { get; set; }
    }
}
