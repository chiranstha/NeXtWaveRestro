using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class GetSalesMasterForGetAllDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }


        public DateTime Date { get; set; }
        public string DateMiti { get; set; }


        public decimal TaxAmount { get; set; }


        public decimal BillDiscount { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal TaxableAmount { get; set; }

        public decimal SubTotalAmount { get; set; }


        public PaymentMethod PaymentMethod { get; set; }


        public decimal? VatRefundAmount { get; set; }

        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }

        public string LedgerName { get; set; }

        public string PanNo { get; set; }
        public string BranchName { get; set; }
        public decimal NoTaxableAmount { get; set; }
        public bool IsCancle { get; set; }
        public string Minute { get; set; }
    }
}
