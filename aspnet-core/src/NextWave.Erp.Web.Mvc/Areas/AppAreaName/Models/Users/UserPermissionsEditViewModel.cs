using Abp.AutoMapper;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Authorization.Users.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Users;

[AutoMapFrom(typeof(GetUserPermissionsForEditOutput))]
public class UserPermissionsEditViewModel : GetUserPermissionsForEditOutput, IPermissionsEditViewModel
{
    public User User { get; set; }
}

