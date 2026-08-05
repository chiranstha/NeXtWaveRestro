using Abp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;

namespace NextWave.Erp.Authorization.Roles;

/// <summary>
/// Represents a role in the system.
/// </summary>
public class Role : AbpRole<User>
{
    //Can add application specific role properties here

    public Role()
    {

    }

    public Role(int? tenantId, string displayName)
        : base(tenantId, displayName)
    {

    }

    public Role(int? tenantId, string name, string displayName)
        : base(tenantId, name, displayName)
    {

    }
}

