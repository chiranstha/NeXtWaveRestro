using Abp.AutoMapper;
using NextWave.Erp.Organizations.Dto;

namespace NextWave.Erp.Maui.Models.User;

[AutoMapFrom(typeof(OrganizationUnitDto))]
public class OrganizationUnitModel : OrganizationUnitDto
{
    public bool IsAssigned { get; set; }
}