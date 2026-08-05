using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace NextWave.Erp.Authorization.Roles;

public static class AppRoleConfig
{
    public static void Configure(IRoleManagementConfig roleManagementConfig)
    {
        //Static host roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.Admin,
                MultiTenancySides.Host,
                grantAllPermissionsByDefault: true)
            );

        //Static tenant roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Admin,
                MultiTenancySides.Tenant,
                grantAllPermissionsByDefault: true)
            );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.User,
                MultiTenancySides.Tenant)
            );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.RestaurantManager,
                MultiTenancySides.Tenant)
            );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.RestaurantCashier,
                MultiTenancySides.Tenant)
            );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.RestaurantWaiter,
                MultiTenancySides.Tenant)
            );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.RestaurantKitchen,
                MultiTenancySides.Tenant)
            );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.RestaurantInventory,
                MultiTenancySides.Tenant)
            );
    }
}

