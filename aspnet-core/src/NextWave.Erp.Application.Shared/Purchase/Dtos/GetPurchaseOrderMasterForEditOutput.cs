using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class GetPurchaseOrderMasterForEditOutput : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public string DateMiti { get; set; }

        public string DueDateMiti { get; set; }

        public string Description { get; set; }

        public decimal? TotalAmount { get; set; }

        public Guid BranchId { get; set; }

        public Guid LedgerId { get; set; }

        public string BranchName { get; set; }

        public string LedgerName { get; set; }

        public List<PurchaseOrderDetailsDto> PurchaseOrderDetails { get; set; }
    }
}
