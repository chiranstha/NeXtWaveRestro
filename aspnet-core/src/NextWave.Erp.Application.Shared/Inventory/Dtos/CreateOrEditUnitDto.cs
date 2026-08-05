using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class CreateOrEditUnitDto : EntityDto<Guid?>
    {
        public string Name { get; set; }

        public string FormalName { get; set; }
    }
}
