using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Purchase.Dtos
{
    public class GetPurchaseOrderMasterForViewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }

        public bool Cancelled { get; set; }

        public string Description { get; set; }

        public decimal? TotalAmount { get; set; }
        public Guid LedgerId { get; set; }
        public int RemainingDays { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
        public PurchaseStatus PurchaseStatus { get; set; }
        public string LedgerName { get; set; }
        public string DateMiti { get; set; }
        public string DueDateMiti { get; set; }
        public List<PurchaseOrderDetailsForViewDto> PurchaseOrderDetails { get; set; }
    }
}
