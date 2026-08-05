using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class GetProductGroupForEditOutput : EntityDto<Guid?>
    {
        public string Name { get; set; }

        public Guid? GroupUnder { get; set; }

        public string Description { get; set; }

        public bool IsDefult { get; set; }
    }
}
