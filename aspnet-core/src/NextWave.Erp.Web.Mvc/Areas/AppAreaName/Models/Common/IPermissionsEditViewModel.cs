using System.Collections.Generic;
using NextWave.Erp.Authorization.Permissions.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

public interface IPermissionsEditViewModel
{
    List<FlatPermissionDto> Permissions { get; set; }

    List<string> GrantedPermissionNames { get; set; }
}

