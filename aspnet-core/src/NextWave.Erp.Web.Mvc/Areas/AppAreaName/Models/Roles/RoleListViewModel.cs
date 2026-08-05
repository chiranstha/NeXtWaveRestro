using System.Collections.Generic;
using Abp.Application.Services.Dto;
using NextWave.Erp.Authorization.Permissions.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Roles;

public class RoleListViewModel : IPermissionsEditViewModel
{
    public List<FlatPermissionDto> Permissions { get; set; }

    public List<string> GrantedPermissionNames { get; set; }
}

