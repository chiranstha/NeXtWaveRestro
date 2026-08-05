using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class StockPostingCreateDto : EntityDto<Guid?>
    {
        public Guid UnitId { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal Rate { get; set; }
    }
}
