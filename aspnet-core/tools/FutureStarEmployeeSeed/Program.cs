using Abp.Configuration;
using Abp.Authorization.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NextWave.Erp;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Configuration;
using NextWave.Erp.EntityFrameworkCore;
using NextWave.Erp.Restaurant;
using System.Text.Json;

var hostSettingsPath = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "../../../../../src/NextWave.Erp.Web.Host/appsettings.json"));
if (!File.Exists(hostSettingsPath))
{
    throw new FileNotFoundException("Host appsettings.json was not found.", hostSettingsPath);
}

using var settingsJson = JsonDocument.Parse(await File.ReadAllTextAsync(hostSettingsPath));
var connectionString = settingsJson.RootElement
    .GetProperty("ConnectionStrings")
    .GetProperty("Default")
    .GetString();

var options = new DbContextOptionsBuilder<ErpDbContext>()
    .UseSqlServer(connectionString)
    .Options;
await using var context = new ErpDbContext(options);

var tenant = await context.Tenants.IgnoreQueryFilters()
    .SingleAsync(item => item.TenancyName == "FutureStar" && !item.IsDeleted);
var tenantId = tenant.Id;

var passwordSetting = await context.Settings.IgnoreQueryFilters().FirstOrDefaultAsync(item =>
    item.TenantId == tenantId &&
    item.UserId == null &&
    item.Name == AppSettings.UserManagement.EmployeeDefaultPassword);
if (passwordSetting == null)
{
    context.Settings.Add(new Setting(
        tenantId,
        null,
        AppSettings.UserManagement.EmployeeDefaultPassword,
        ErpConsts.DefaultRestaurantEmployeePassword));
}
else
{
    passwordSetting.Value = ErpConsts.DefaultRestaurantEmployeePassword;
}
await context.SaveChangesAsync();

var employeeSeeds = new[]
{
    new EmployeeSeed("FS-MGR-001", "FutureStar Manager", "futurestar.manager", StaticRoleNames.Tenants.RestaurantManager, "Management", 45000m),
    new EmployeeSeed("FS-CASH-001", "FutureStar Cashier", "futurestar.cashier", StaticRoleNames.Tenants.RestaurantCashier, "Cash Counter", 30000m),
    new EmployeeSeed("FS-WAIT-001", "FutureStar Waiter", "futurestar.waiter", StaticRoleNames.Tenants.RestaurantWaiter, "Service", 25000m),
    new EmployeeSeed("FS-KIT-001", "FutureStar Kitchen", "futurestar.kitchen", StaticRoleNames.Tenants.RestaurantKitchen, "Kitchen", 32000m),
    new EmployeeSeed("FS-INV-001", "FutureStar Inventory", "futurestar.inventory", StaticRoleNames.Tenants.RestaurantInventory, "Inventory", 30000m),
    new EmployeeSeed("FS-PAY-001", "FutureStar Payroll", "futurestar.payroll", StaticRoleNames.Tenants.RestaurantPayroll, "Payroll", 35000m),
};

var operatingRoleNames = new[]
{
    StaticRoleNames.Tenants.RestaurantManager,
    StaticRoleNames.Tenants.RestaurantCashier,
    StaticRoleNames.Tenants.RestaurantWaiter,
    StaticRoleNames.Tenants.RestaurantKitchen,
    StaticRoleNames.Tenants.RestaurantInventory,
    StaticRoleNames.Tenants.RestaurantPayroll,
};
var roles = await context.Roles.IgnoreQueryFilters()
    .Where(item => item.TenantId == tenantId && operatingRoleNames.Contains(item.Name))
    .ToDictionaryAsync(item => item.Name);
var missingRoles = operatingRoleNames.Where(roleName => !roles.ContainsKey(roleName)).ToArray();
if (missingRoles.Length > 0)
{
    throw new InvalidOperationException($"Missing FutureStar restaurant roles: {string.Join(", ", missingRoles)}");
}

var hasher = new PasswordHasher<User>(
    new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions()));
var createdUsers = 0;
var createdEmployees = 0;

foreach (var seed in employeeSeeds)
{
    var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item =>
        item.TenantId == tenantId && item.UserName == seed.UserName && !item.IsDeleted);
    if (user == null)
    {
        var nameParts = seed.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        user = new User
        {
            TenantId = tenantId,
            UserName = seed.UserName,
            Name = nameParts[0],
            Surname = nameParts.Length > 1 ? nameParts[1] : "Employee",
            EmailAddress = $"{seed.UserName}@tenant{tenantId}.restaurant.local",
            IsActive = true,
            IsEmailConfirmed = true,
            IsTwoFactorEnabled = false,
            IsLockoutEnabled = true,
            ShouldChangePasswordOnNextLogin = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            Roles = new List<UserRole>(),
            OrganizationUnits = new List<UserOrganizationUnit>(),
        };
        user.SetNormalizedNames();
        user.Password = hasher.HashPassword(user, ErpConsts.DefaultRestaurantEmployeePassword);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        createdUsers++;
    }

    var operatingRoleIds = roles.Values.Select(role => role.Id).ToArray();
    var staleAssignments = await context.UserRoles.IgnoreQueryFilters()
        .Where(item => item.TenantId == tenantId && item.UserId == user.Id && operatingRoleIds.Contains(item.RoleId))
        .ToListAsync();
    context.UserRoles.RemoveRange(staleAssignments);
    context.UserRoles.Add(new UserRole(tenantId, user.Id, roles[seed.RoleName].Id));

    var employee = await context.RestaurantPayrollEmployees.IgnoreQueryFilters().FirstOrDefaultAsync(item =>
        item.TenantId == tenantId && item.StaffCode == seed.StaffCode);
    if (employee == null)
    {
        employee = new RestaurantPayrollEmployee
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StaffCode = seed.StaffCode,
            Name = seed.Name,
            JobRole = roles[seed.RoleName].DisplayName,
            Department = seed.Department,
            EmploymentType = RestaurantEmploymentType.Monthly,
            BasicSalary = seed.BasicSalary,
            OvertimeRate = 250m,
            ServiceChargeWeight = seed.RoleName == StaticRoleNames.Tenants.RestaurantManager ? 1.5m : 1m,
            JoinedOn = DateTime.Today,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        context.RestaurantPayrollEmployees.Add(employee);
        createdEmployees++;
    }
    employee.UserId = user.Id;
    employee.LoginManagedByRestaurant = true;
    employee.IsActive = true;
}

await context.SaveChangesAsync();

Console.WriteLine($"FutureStar tenant: {tenantId}");
Console.WriteLine($"Created users: {createdUsers}");
Console.WriteLine($"Created payroll employees: {createdEmployees}");
Console.WriteLine($"Managed payroll employees now: {await context.RestaurantPayrollEmployees.IgnoreQueryFilters().CountAsync(item => item.TenantId == tenantId && item.LoginManagedByRestaurant)}");
Console.WriteLine("All seeded employees use the tenant default employee password and must change it on first sign in.");

internal sealed record EmployeeSeed(
    string StaffCode,
    string Name,
    string UserName,
    string RoleName,
    string Department,
    decimal BasicSalary);
