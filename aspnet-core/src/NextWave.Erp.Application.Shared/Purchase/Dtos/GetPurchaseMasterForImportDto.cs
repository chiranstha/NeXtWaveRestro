using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class GetPurchaseMasterForImportDto : EntityDto
    {
        public string BranchName { get; set; }

        //public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public string VoucherNo { get; set; }
        public string VendorInvoiceNo { get; set; }
        public string CashOrParty { get; set; }

        public string Exception { get; set; }

        //public DateTime? VendorInvoiceDate { get; set; }
        public double? TotalAmount { get; set; }
        public double? TaxAmount { get; set; }
        public double? TaxableAmount { get; set; }
        public double? BillDiscount { get; set; }
        public double? GrandTotal { get; set; }
    }
}
