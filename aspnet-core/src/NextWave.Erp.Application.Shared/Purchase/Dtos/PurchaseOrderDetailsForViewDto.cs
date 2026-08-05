using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseOrderDetailsForViewDto : EntityDto<Guid>
    {
        public decimal? Qty { get; set; }

        public decimal? Rate { get; set; }

        public decimal? Amount { get; set; }

        public Guid ProductId { get; set; }

        public string ProductCode { get; set; }
        public string ProductName { get; set; }

        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
    }
}
