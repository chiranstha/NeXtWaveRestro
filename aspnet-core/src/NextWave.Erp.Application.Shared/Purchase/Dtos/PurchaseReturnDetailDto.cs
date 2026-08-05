using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseReturnDetailDto : EntityDto<Guid>
    {
        public Guid? PurchaseDetailsId;

        public decimal? Qty { get; set; }

        public decimal? Rate { get; set; }

        public decimal? Discount { get; set; }
        public decimal? DiscountPer { get; set; }

        public decimal? TaxAmount { get; set; }


        public decimal? GrossAmount { get; set; }

        public decimal? NetAmount { get; set; }

        public decimal? Amount { get; set; }

        public Guid ProductId { get; set; }
        public string ProductCode { get; set; }

        public Guid TaxId { get; set; }

        public Guid UnitId { get; set; }

        public List<PurchaseReturnUnitsQtyDto> UnitsList { get; set; }
    }
}
