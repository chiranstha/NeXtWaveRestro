using Abp.Authorization;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;

namespace NextWave.Erp.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {

    }
}

