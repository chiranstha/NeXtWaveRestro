using System.Collections.Generic;
using NextWave.Erp.Organizations.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

public interface IOrganizationUnitsEditViewModel
{
    List<OrganizationUnitDto> AllOrganizationUnits { get; set; }

    List<string> MemberedOrganizationUnits { get; set; }
}

