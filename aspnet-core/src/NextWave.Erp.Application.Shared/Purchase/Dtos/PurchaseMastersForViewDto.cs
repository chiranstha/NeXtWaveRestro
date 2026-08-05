using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseMastersForViewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }

        public string DateMiti { get; set; }
        public string VendorInvoiceNo { get; set; }

        //public DateTime? VendorInvoiceDate { get; set; }

        public decimal? TotalTax { get; set; }

        public decimal? GrandTotal { get; set; }

        public string LedgerName { get; set; }
        public string BranchName { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
    }
}
