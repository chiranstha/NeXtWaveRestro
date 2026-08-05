using System.Linq;
using Abp.Authorization.Users;
using Abp.AutoMapper;
using NextWave.Erp.Authorization.Users.Dto;
using NextWave.Erp.Security;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Users;

[AutoMapFrom(typeof(GetUserForEditOutput))]
public class CreateOrEditUserModalViewModel : GetUserForEditOutput, IOrganizationUnitsEditViewModel
{
    public bool CanChangeUserName => User.UserName != AbpUserBase.AdminUserName;

    public int AssignedRoleCount
    {
        get { return Roles.Count(r => r.IsAssigned); }
    }

    public int AssignedOrganizationUnitCount => MemberedOrganizationUnits.Count;

    public bool IsEditMode => User.Id.HasValue;

    public PasswordComplexitySetting PasswordComplexitySetting { get; set; }
}

