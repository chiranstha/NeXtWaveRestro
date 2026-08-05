using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class GetPurchaseReturnForViewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }

        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public decimal TotalDiscount { get; set; }

        public string Description { get; set; }

        public Guid PurchaseAccount { get; set; }

        public decimal TotalTax { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public string LrNo { get; set; }

        public string TransportationCompany { get; set; }

        public string PurchaseMasterVoucherNo { get; set; }

        public string LedgerName { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
    }
}
