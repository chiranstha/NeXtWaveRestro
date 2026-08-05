using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class CreateOrEditProductGroupDto : EntityDto<Guid?>
    {
        public string Name { get; set; }

        public Guid? GroupUnder { get; set; }
        public string Description { get; set; }
    }
}
