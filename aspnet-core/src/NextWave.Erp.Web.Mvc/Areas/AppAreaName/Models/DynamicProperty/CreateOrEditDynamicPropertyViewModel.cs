using System.Collections.Generic;
using NextWave.Erp.DynamicEntityProperties.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.DynamicProperty;

public class CreateOrEditDynamicPropertyViewModel
{
    public DynamicPropertyDto DynamicPropertyDto { get; set; }

    public List<string> AllowedInputTypes { get; set; }
}

