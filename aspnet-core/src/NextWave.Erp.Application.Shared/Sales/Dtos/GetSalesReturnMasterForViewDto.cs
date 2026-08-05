using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class GetSalesReturnMasterForViewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }
        public decimal TaxAmount { get; set; }
        public string LrNo { get; set; }
        public string TransportationCompany { get; set; }
        public string DateMiti { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BillDiscount { get; set; }
        public decimal GrandTotal { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
        public string Description { get; set; }
    }
}
