using System.Collections.Generic;
using NextWave.Erp.DynamicEntityProperties.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.DynamicEntityProperty;

public class CreateEntityDynamicPropertyViewModel
{
    public string EntityFullName { get; set; }

    public List<string> AllEntities { get; set; }

    public List<DynamicPropertyDto> DynamicProperties { get; set; }
}

