using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class SeedRestaurantAdminPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @RestaurantPermissions TABLE ([Name] NVARCHAR(128) NOT NULL);

INSERT INTO @RestaurantPermissions ([Name])
VALUES
    (N'Pages.Restaurant'),
    (N'Pages.Restaurant.Setup'),
    (N'Pages.Restaurant.Setup.Create'),
    (N'Pages.Restaurant.Setup.Edit'),
    (N'Pages.Restaurant.Setup.Delete'),
    (N'Pages.Restaurant.Menu'),
    (N'Pages.Restaurant.Menu.Create'),
    (N'Pages.Restaurant.Menu.Edit'),
    (N'Pages.Restaurant.Menu.Delete'),
    (N'Pages.Restaurant.Recipe'),
    (N'Pages.Restaurant.Pos'),
    (N'Pages.Restaurant.Billing'),
    (N'Pages.Restaurant.KotBot'),
    (N'Pages.Restaurant.Kds'),
    (N'Pages.Restaurant.Sync'),
    (N'Pages.Restaurant.Reports');

INSERT INTO AbpPermissions
    (CreationTime, IsGranted, [Name], TenantId, RoleId, Discriminator)
SELECT
    GETDATE(),
    CAST(1 AS BIT),
    p.[Name],
    r.TenantId,
    r.Id,
    N'RolePermissionSetting'
FROM AbpRoles r
CROSS JOIN @RestaurantPermissions p
WHERE r.[Name] = N'Admin'
  AND r.TenantId IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM AbpPermissions existing
      WHERE existing.Discriminator = N'RolePermissionSetting'
        AND existing.RoleId = r.Id
        AND existing.TenantId = r.TenantId
        AND existing.[Name] = p.[Name]
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE existing
FROM AbpPermissions existing
INNER JOIN AbpRoles r ON r.Id = existing.RoleId
WHERE existing.Discriminator = N'RolePermissionSetting'
  AND r.[Name] = N'Admin'
  AND r.TenantId IS NOT NULL
  AND existing.[Name] IN
  (
      N'Pages.Restaurant',
      N'Pages.Restaurant.Setup',
      N'Pages.Restaurant.Setup.Create',
      N'Pages.Restaurant.Setup.Edit',
      N'Pages.Restaurant.Setup.Delete',
      N'Pages.Restaurant.Menu',
      N'Pages.Restaurant.Menu.Create',
      N'Pages.Restaurant.Menu.Edit',
      N'Pages.Restaurant.Menu.Delete',
      N'Pages.Restaurant.Recipe',
      N'Pages.Restaurant.Pos',
      N'Pages.Restaurant.Billing',
      N'Pages.Restaurant.KotBot',
      N'Pages.Restaurant.Kds',
      N'Pages.Restaurant.Sync',
      N'Pages.Restaurant.Reports'
  );
");
        }
    }
}
