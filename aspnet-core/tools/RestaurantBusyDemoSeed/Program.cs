using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.EntityFrameworkCore;
using NextWave.Erp.Migrations.Seed.Tenants;

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
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default is empty in the host appsettings.json.");
}

var options = new DbContextOptionsBuilder<ErpDbContext>()
    .UseSqlServer(connectionString)
    .Options;
await using var context = new ErpDbContext(options);

var tenant = await context.Tenants.IgnoreQueryFilters()
    .SingleOrDefaultAsync(item => item.Id == 2);
if (tenant == null)
{
    var maxTenantId = await context.Tenants.IgnoreQueryFilters()
        .Select(item => item.Id)
        .DefaultIfEmpty(0)
        .MaxAsync();

    if (maxTenantId == 0)
    {
        new DefaultTenantBuilder(context).Create();
        maxTenantId = await context.Tenants.IgnoreQueryFilters()
            .Select(item => item.Id)
            .DefaultIfEmpty(0)
            .MaxAsync();
    }

    if (maxTenantId > 1)
    {
        throw new InvalidOperationException($"Tenant 2 does not exist, and the highest existing tenant ID is {maxTenantId}; the seed cannot safely create the requested tenant ID.");
    }

    tenant = new NextWave.Erp.MultiTenancy.Tenant("FutureStar", "FutureStar");
    context.Tenants.Add(tenant);
    await context.SaveChangesAsync();
    if (tenant.Id != 2)
    {
        throw new InvalidOperationException($"FutureStar was created as tenant {tenant.Id}, not tenant 2. No restaurant dataset was applied.");
    }

    Console.WriteLine("Created FutureStar as tenant 2.");
}

if (tenant.IsDeleted)
{
    throw new InvalidOperationException("Tenant 2 is soft deleted. Restore it before applying the restaurant seed.");
}

context.SuppressAutoSetTenantId = true;
new TenantRoleAndUserBuilder(context, tenant.Id).Create();

var seedSqlPath = Path.Combine(AppContext.BaseDirectory, "seed-busy-restaurant.sql");
if (!File.Exists(seedSqlPath))
{
    throw new FileNotFoundException("The busy restaurant SQL seed was not copied to the output folder.", seedSqlPath);
}

var seedSql = await File.ReadAllTextAsync(seedSqlPath);
await context.Database.OpenConnectionAsync();
try
{
    var connection = context.Database.GetDbConnection();
    await using var command = connection.CreateCommand();
    command.CommandText = seedSql;
    command.CommandTimeout = 300;

    await using var reader = await command.ExecuteReaderAsync();
    var manifestReturned = false;
    do
    {
        if (reader.FieldCount < 2 || reader.GetName(0) != "Dataset" || reader.GetName(1) != "Rows")
        {
            continue;
        }

        manifestReturned = true;
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"{reader.GetString(0)}: {reader.GetInt32(1)}");
        }
    }
    while (await reader.NextResultAsync());

    if (!manifestReturned)
    {
        throw new InvalidOperationException("The SQL seed completed without returning its dataset manifest.");
    }
}
finally
{
    await context.Database.CloseConnectionAsync();
}

Console.WriteLine($"Busy restaurant seed completed for tenant 2 ({tenant.TenancyName}).");
