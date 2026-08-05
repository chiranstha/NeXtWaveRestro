using Abp.AutoMapper;
using NextWave.Erp.Authorization.Roles.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Roles;

[AutoMapFrom(typeof(GetRoleForEditOutput))]
public class CreateOrEditRoleModalViewModel : GetRoleForEditOutput, IPermissionsEditViewModel
{
    public bool IsEditMode => Role.Id.HasValue;
}

