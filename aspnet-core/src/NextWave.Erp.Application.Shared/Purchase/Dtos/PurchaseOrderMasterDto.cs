using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseOrderMasterDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }

        public DateTime? Date { get; set; }

        public DateTime? DueDate { get; set; }

        public bool Cancelled { get; set; }

        public string Description { get; set; }

        public decimal? TotalAmount { get; set; }

        public Guid VoucherTypeId { get; set; }

        public string VoucherName { get; set; }

        public Guid LedgerId { get; set; }
    }
}
