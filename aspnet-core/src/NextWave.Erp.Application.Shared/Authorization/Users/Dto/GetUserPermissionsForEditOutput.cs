using System.Collections.Generic;
using NextWave.Erp.Authorization.Permissions.Dto;

namespace NextWave.Erp.Authorization.Users.Dto;

public class GetUserPermissionsForEditOutput
{
    public List<FlatPermissionDto> Permissions { get; set; }

    public List<string> GrantedPermissionNames { get; set; }
}

