using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Purchase.Dtos
{
    public class CreateOrEditPurchaseOrderMasterDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public string DateMiti { get; set; }

        public string DueDateMiti { get; set; }

        public bool Cancelled { get; set; }

        public string Description { get; set; }

        public decimal? TotalAmount { get; set; }


        public Guid LedgerId { get; set; }

        public List<PurchaseOrderDetailsDto> PurchaseOrderDetails { get; set; }
    }
}
