using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class CreateOrEditSalesMasterDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public Guid SalesAccountId { get; set; }

        public string DateMiti { get; set; }

        public int CreditPeriod { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerVatNo { get; set; }
        public string CustomerPhoneNo { get; set; }
        public string Description { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal BillDiscount { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public bool IsPrint { get; set; }


        public decimal SubTotalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        public Guid? PaymentMethodLedgerId { get; set; }

        public List<SalesPaymentAllocationDto> PaymentAllocations { get; set; } = new();
        public decimal RestaurantTipAmount { get; set; }
        public Guid? RestaurantTipLedgerId { get; set; }

        public decimal? VatRefundAmount { get; set; }

        public string LrNo { get; set; }
        public string PiNumber { get; set; }

        public string VehicleNo { get; set; }
        public Guid LedgerId { get; set; }

        public SalesModeType SalesModeType { get; set; }
        public List<Guid> AgainstId { get; set; }
        public string AgainstVoucherNo { get; set; }

        public InvoiceTypeEnum InvoiceType { get; set; }

        public string SourceModule { get; set; }

        public Guid? SourceDocumentId { get; set; }


        public List<SalesDetailDto> SalesDetails { get; set; }
    }
}
