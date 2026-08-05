using System.Linq;
using System;
using System.Data;
using Abp;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.MultiTenancy;
using Abp.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.EntityFrameworkCore;
using NextWave.Erp.Enums;
using NextWave.Erp.Notifications;
using NextWave.Erp.Restaurant;

namespace NextWave.Erp.Migrations.Seed.Tenants;

public class TenantRoleAndUserBuilder
{
    private readonly ErpDbContext _context;
    private readonly int _tenantId;

    public TenantRoleAndUserBuilder(ErpDbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        CreateRolesAndUsers();
    }

    private void CreateRolesAndUsers()
    {
        //Admin role

        var adminRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Admin);
        if (adminRole == null)
        {
            adminRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Admin, StaticRoleNames.Tenants.Admin) { IsStatic = true }).Entity;
            _context.SaveChanges();
        }
        GrantRestaurantPermissionsToAdminRole(adminRole);
        CreateRestaurantOperatingRoles();
        CreateRestaurantBaselineData();

        //User role

        var userRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.User);
        if (userRole == null)
        {
            _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.User, StaticRoleNames.Tenants.User) { IsStatic = true, IsDefault = true });
            _context.SaveChanges();
        }

        //admin user

        var adminUser = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == _tenantId && u.UserName == AbpUserBase.AdminUserName);
        if (adminUser == null)
        {
            adminUser = User.CreateTenantAdminUser(_tenantId, "admin@defaulttenant.com");
            adminUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(adminUser, "123qwe");
            adminUser.IsEmailConfirmed = true;
            adminUser.ShouldChangePasswordOnNextLogin = false;
            adminUser.IsActive = true;

            _context.Users.Add(adminUser);
            _context.SaveChanges();

            //Assign Admin role to admin user
            _context.UserRoles.Add(new UserRole(_tenantId, adminUser.Id, adminRole.Id));
            _context.SaveChanges();

            //User account of admin user
            if (_tenantId == 1)
            {
                _context.UserAccounts.Add(new UserAccount
                {
                    TenantId = _tenantId,
                    UserId = adminUser.Id,
                    UserName = AbpUserBase.AdminUserName,
                    EmailAddress = adminUser.EmailAddress
                });
                _context.SaveChanges();
            }

            //Notification subscription
            _context.NotificationSubscriptions.Add(new NotificationSubscriptionInfo(SequentialGuidGenerator.Instance.Create(), _tenantId, adminUser.Id, AppNotificationNames.NewUserRegistered));
            _context.SaveChanges();
        }
    }

    private void GrantRestaurantPermissionsToAdminRole(Role adminRole)
    {
        GrantPermissionsToRole(adminRole, GetRestaurantManagerPermissions());
    }

    private void CreateRestaurantOperatingRoles()
    {
        GrantPermissionsToRole(
            EnsureTenantRole(StaticRoleNames.Tenants.RestaurantManager, "Restaurant Manager"),
            GetRestaurantManagerPermissions());

        GrantPermissionsToRole(
            EnsureTenantRole(StaticRoleNames.Tenants.RestaurantCashier, "Restaurant Cashier"),
            new[]
            {
                AppPermissions.PagesRestaurant,
                AppPermissions.PagesRestaurantMenu,
                AppPermissions.PagesRestaurantItemAvailability,
                AppPermissions.PagesRestaurantPos,
                AppPermissions.PagesRestaurantPosDiscount,
                AppPermissions.PagesRestaurantPosVoid,
                AppPermissions.PagesRestaurantPosTableTransfer,
                AppPermissions.PagesRestaurantPosSplitMerge,
                AppPermissions.PagesRestaurantBilling,
                AppPermissions.PagesRestaurantKotBot,
                AppPermissions.PagesRestaurantKotBotReprint,
                AppPermissions.PagesRestaurantKds,
                AppPermissions.PagesRestaurantReports
            });

        GrantPermissionsToRole(
            EnsureTenantRole(StaticRoleNames.Tenants.RestaurantWaiter, "Restaurant Waiter"),
            new[]
            {
                AppPermissions.PagesRestaurant,
                AppPermissions.PagesRestaurantMenu,
                AppPermissions.PagesRestaurantPos,
                AppPermissions.PagesRestaurantPosTableTransfer,
                AppPermissions.PagesRestaurantKotBot,
                AppPermissions.PagesRestaurantKds
            });

        GrantPermissionsToRole(
            EnsureTenantRole(StaticRoleNames.Tenants.RestaurantKitchen, "Restaurant Kitchen"),
            new[]
            {
                AppPermissions.PagesRestaurant,
                AppPermissions.PagesRestaurantKotBot,
                AppPermissions.PagesRestaurantKds
            });

        GrantPermissionsToRole(
            EnsureTenantRole(StaticRoleNames.Tenants.RestaurantInventory, "Restaurant Inventory"),
            new[]
            {
                AppPermissions.PagesRestaurant,
                AppPermissions.PagesRestaurantMenu,
                AppPermissions.PagesRestaurantRecipe,
                AppPermissions.PagesRestaurantReports,
                AppPermissions.PagesRestaurantInventory,
                AppPermissions.PagesRestaurantInventorySupplierMapping,
                AppPermissions.PagesRestaurantInventoryStockAdjustment,
                AppPermissions.PagesRestaurantInventoryWastage,
                AppPermissions.PagesRestaurantInventoryReorder
            });
    }

    private Role EnsureTenantRole(string roleName, string displayName)
    {
        var role = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == roleName);
        if (role != null)
            return role;

        role = _context.Roles.Add(new Role(_tenantId, roleName, displayName) { IsStatic = true }).Entity;
        _context.SaveChanges();
        return role;
    }

    private void GrantPermissionsToRole(Role role, string[] permissions)
    {
        foreach (var permission in permissions.Distinct())
        {
            var alreadyGranted = _context.Permissions.IgnoreQueryFilters()
                .OfType<RolePermissionSetting>()
                .Any(p => p.TenantId == _tenantId && p.RoleId == role.Id && p.Name == permission);

            if (alreadyGranted)
                continue;

            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = _tenantId,
                RoleId = role.Id,
                Name = permission,
                IsGranted = true
            });
        }

        _context.SaveChanges();
    }

    private static string[] GetRestaurantManagerPermissions()
    {
        return new[]
        {
            AppPermissions.PagesRestaurant,
            AppPermissions.PagesRestaurantSetup,
            AppPermissions.PagesRestaurantSetupCreate,
            AppPermissions.PagesRestaurantSetupEdit,
            AppPermissions.PagesRestaurantSetupDelete,
            AppPermissions.PagesRestaurantMenu,
            AppPermissions.PagesRestaurantMenuCreate,
            AppPermissions.PagesRestaurantMenuEdit,
            AppPermissions.PagesRestaurantMenuDelete,
            AppPermissions.PagesRestaurantMenuVariants,
            AppPermissions.PagesRestaurantMenuModifiers,
            AppPermissions.PagesRestaurantItemAvailability,
            AppPermissions.PagesRestaurantRecipe,
            AppPermissions.PagesRestaurantPos,
            AppPermissions.PagesRestaurantPosDiscount,
            AppPermissions.PagesRestaurantPosVoid,
            AppPermissions.PagesRestaurantPosTableTransfer,
            AppPermissions.PagesRestaurantPosSplitMerge,
            AppPermissions.PagesRestaurantBilling,
            AppPermissions.PagesRestaurantKotBot,
            AppPermissions.PagesRestaurantKotBotReprint,
            AppPermissions.PagesRestaurantKds,
            AppPermissions.PagesRestaurantSync,
            AppPermissions.PagesRestaurantReports,
            AppPermissions.PagesRestaurantInventory,
            AppPermissions.PagesRestaurantInventorySupplierMapping,
            AppPermissions.PagesRestaurantInventoryStockAdjustment,
            AppPermissions.PagesRestaurantInventoryWastage,
            AppPermissions.PagesRestaurantInventoryReorder,
            AppPermissions.PagesRestaurantChannels,
            AppPermissions.PagesRestaurantChannelsManage,
            AppPermissions.PagesRestaurantAggregators,
            AppPermissions.PagesRestaurantPayouts,
            AppPermissions.PagesRestaurantCustomerOrdering
        };
    }

    private void CreateRestaurantBaselineData()
    {
        if (!RestaurantSeedTablesExist())
            return;

        if (!_context.RestaurantAreas.IgnoreQueryFilters().Any(x => x.TenantId == _tenantId && !x.IsDeleted))
        {
            var mainDining = AddRestaurantArea("Main Dining", "Default dine-in floor", 1);
            var patio = AddRestaurantArea("Patio", "Outdoor or overflow service area", 2);
            var takeaway = AddRestaurantArea("Takeaway Counter", "Counter pickup and quick-service area", 3);

            AddRestaurantTable(mainDining, "T1", "T1", 4, 1);
            AddRestaurantTable(mainDining, "T2", "T2", 4, 2);
            AddRestaurantTable(mainDining, "T3", "T3", 6, 3);
            AddRestaurantTable(mainDining, "T4", "T4", 2, 4);
            AddRestaurantTable(patio, "P1", "P1", 4, 1);
            AddRestaurantTable(patio, "P2", "P2", 4, 2);
            AddRestaurantTable(takeaway, "Counter 1", "C1", 1, 1);
            AddRestaurantTable(takeaway, "Counter 2", "C2", 1, 2);
        }

        if (!_context.RestaurantStations.IgnoreQueryFilters().Any(x => x.TenantId == _tenantId && !x.IsDeleted))
        {
            AddRestaurantStation("Main Kitchen", RestaurantStationType.Kitchen);
            AddRestaurantStation("Bar", RestaurantStationType.Bar);
            AddRestaurantStation("Counter", RestaurantStationType.Counter);
        }

        if (!_context.RestaurantChannels.IgnoreQueryFilters().Any(x => x.TenantId == _tenantId && !x.IsDeleted))
        {
            AddRestaurantChannel("Dine In", RestaurantChannelType.DineIn, RestaurantChannelProvider.Internal, 0, 0, 1);
            AddRestaurantChannel("Take Away", RestaurantChannelType.TakeAway, RestaurantChannelProvider.Internal, 0, 0, 2);
            AddRestaurantChannel("Own Online", RestaurantChannelType.OwnOnline, RestaurantChannelProvider.OwnOnline, 0, 0, 3);
            AddRestaurantChannel("Foodmandu", RestaurantChannelType.Aggregator, RestaurantChannelProvider.Foodmandu, 20, 0, 4);
            AddRestaurantChannel("Pathao", RestaurantChannelType.Aggregator, RestaurantChannelProvider.Pathao, 20, 0, 5);
        }

        _context.SaveChanges();
    }

    private bool RestaurantSeedTablesExist()
    {
        var connection = _context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            if (_context.Database.CurrentTransaction != null)
                command.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();

            var isSqlite = _context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
            command.CommandText = isSqlite
                ? @"
SELECT COUNT(*)
FROM sqlite_master
WHERE type = 'table'
  AND name IN (
      'tbl_RestaurantArea',
      'tbl_RestaurantTable',
      'tbl_RestaurantStation',
      'tbl_RestaurantChannel'
  )"
                : @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME IN (
      'tbl_RestaurantArea',
      'tbl_RestaurantTable',
      'tbl_RestaurantStation',
      'tbl_RestaurantChannel'
  )";

            return Convert.ToInt32(command.ExecuteScalar()) == 4;
        }
        finally
        {
            if (shouldCloseConnection)
                connection.Close();
        }
    }

    private RestaurantArea AddRestaurantArea(string name, string description, int sortOrder)
    {
        var area = new RestaurantArea
        {
            Id = SequentialGuidGenerator.Instance.Create(),
            TenantId = _tenantId,
            Name = name,
            Description = description,
            SortOrder = sortOrder,
            IsActive = true
        };
        _context.RestaurantAreas.Add(area);
        return area;
    }

    private void AddRestaurantTable(RestaurantArea area, string name, string code, int capacity, int sortOrder)
    {
        _context.RestaurantTables.Add(new RestaurantTable
        {
            Id = SequentialGuidGenerator.Instance.Create(),
            TenantId = _tenantId,
            AreaId = area.Id,
            Name = name,
            Code = code,
            Capacity = capacity,
            SortOrder = sortOrder,
            Status = RestaurantTableStatus.Available,
            IsActive = true
        });
    }

    private void AddRestaurantStation(string name, RestaurantStationType stationType)
    {
        _context.RestaurantStations.Add(new RestaurantStation
        {
            Id = SequentialGuidGenerator.Instance.Create(),
            TenantId = _tenantId,
            Name = name,
            StationType = stationType,
            IsActive = true
        });
    }

    private void AddRestaurantChannel(
        string name,
        RestaurantChannelType channelType,
        RestaurantChannelProvider provider,
        decimal commissionPercent,
        decimal defaultPriceMarkupPercent,
        int sortOrder)
    {
        _context.RestaurantChannels.Add(new RestaurantChannel
        {
            Id = SequentialGuidGenerator.Instance.Create(),
            TenantId = _tenantId,
            Name = name,
            ChannelType = channelType,
            Provider = provider,
            CommissionPercent = commissionPercent,
            DefaultPriceMarkupPercent = defaultPriceMarkupPercent,
            SortOrder = sortOrder,
            IsOnline = true,
            IsActive = true,
            CreatedAt = System.DateTime.Now
        });
    }
}

