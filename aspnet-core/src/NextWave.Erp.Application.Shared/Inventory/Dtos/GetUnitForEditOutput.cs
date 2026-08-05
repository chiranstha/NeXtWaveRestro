using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class GetUnitForEditOutput : EntityDto<Guid>
    {
        public string Name { get; set; }
        public string FormalName { get; set; }
    }
}
