using Abp.Zero.Ldap.Authentication;
using Abp.Zero.Ldap.Configuration;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.MultiTenancy;

namespace NextWave.Erp.Authorization.Ldap;

public class AppLdapAuthenticationSource : LdapAuthenticationSource<Tenant, User>
{
    public AppLdapAuthenticationSource(ILdapSettings settings, IAbpZeroLdapModuleConfig ldapModuleConfig)
        : base(settings, ldapModuleConfig)
    {
    }
}

