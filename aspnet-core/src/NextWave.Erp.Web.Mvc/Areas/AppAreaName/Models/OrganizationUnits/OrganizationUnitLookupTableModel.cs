using System.Collections.Generic;
using NextWave.Erp.Organizations.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.OrganizationUnits;

public class OrganizationUnitLookupTableModel : IOrganizationUnitsEditViewModel
{
    public List<OrganizationUnitDto> AllOrganizationUnits { get; set; }

    public List<string> MemberedOrganizationUnits { get; set; }

    public OrganizationUnitLookupTableModel()
    {
        AllOrganizationUnits = new List<OrganizationUnitDto>();
        MemberedOrganizationUnits = new List<string>();
    }
}

