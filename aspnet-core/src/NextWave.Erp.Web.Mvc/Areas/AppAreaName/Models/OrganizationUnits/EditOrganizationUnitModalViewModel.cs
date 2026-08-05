using Abp.AutoMapper;
using Abp.Organizations;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.OrganizationUnits;

[AutoMapFrom(typeof(OrganizationUnit))]
public class EditOrganizationUnitModalViewModel
{
    public long? Id { get; set; }

    public string DisplayName { get; set; }
}

