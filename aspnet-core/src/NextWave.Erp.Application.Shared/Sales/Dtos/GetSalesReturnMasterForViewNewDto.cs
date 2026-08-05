using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Sales.Dtos
{
    public class GetSalesReturnMasterForViewNewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }
        public string AgainstVoucherNo { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrossAmount { get; set; }
        public string LrNo { get; set; }
        public string TransportationCompany { get; set; }
        public string DateMiti { get; set; }
        public decimal NetAmount { get; set; }
        public string ReturnType { get; set; }
        public decimal BillDiscount { get; set; }
        public decimal GrandTotal { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public string BranchName { get; set; }
        public List<GetSalesReturnDetailsForViewNewDto> Details { get; set; }
    }

    public class GetSalesReturnDetailsForViewNewDto
    {
        public decimal? Qty { get; set; }

        public decimal? Rate { get; set; }

        public decimal? TaxAmount { get; set; }
        public decimal? TaxValue { get; set; }
        public decimal? Discount { get; set; }
        public decimal? DiscountPer { get; set; }

        public decimal? GrossAmount { get; set; }

        public decimal? NetAmount { get; set; }

        public decimal? Amount { get; set; }

        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string UnitName { get; set; }

        public Guid UnitId { get; set; }


        public Guid? TaxId { get; set; }
    }
}
