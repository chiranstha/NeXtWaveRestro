using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseOrderDetailsDto : EntityDto<Guid>
    {
        public decimal? Qty { get; set; }

        public decimal? Rate { get; set; }

        public decimal? Amount { get; set; }

        public Guid ProductId { get; set; }

        public string ProductCode { get; set; }
        public string ProductName { get; set; }

        public Guid UnitId { get; set; }
    }
}
