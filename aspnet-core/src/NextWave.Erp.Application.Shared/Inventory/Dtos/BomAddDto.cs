using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class BomAddDto : EntityDto<Guid>
    {
        public Guid RawMaterialId { get; set; }
        public decimal Quantity { get; set; }
        public Guid UnitId { get; set; }
    }
}
