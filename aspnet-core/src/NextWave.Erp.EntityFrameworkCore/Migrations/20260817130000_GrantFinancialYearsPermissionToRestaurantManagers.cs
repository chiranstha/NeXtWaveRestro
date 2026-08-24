using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextWave.Erp.EntityFrameworkCore;

#nullable disable

namespace NextWave.Erp.Migrations
{
    [DbContext(typeof(ErpDbContext))]
    [Migration("20260817130000_GrantFinancialYearsPermissionToRestaurantManagers")]
    public partial class GrantFinancialYearsPermissionToRestaurantManagers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @PermissionName NVARCHAR(128) = N'Pages.FinancialYears';

UPDATE existing
SET existing.IsGranted = 1
FROM AbpPermissions existing
INNER JOIN AbpRoles role ON role.Id = existing.RoleId
WHERE existing.Discriminator = N'RolePermissionSetting'
  AND existing.[Name] = @PermissionName
  AND role.TenantId IS NOT NULL
  AND role.[Name] IN
  (
      N'RestaurantManager',
      N'RestaurantSalesManager',
      N'RestaurantOperationsManager',
      N'RestaurantInventoryManager',
      N'RestaurantFinanceManager',
      N'RestaurantPayrollManager'
  );

INSERT INTO AbpPermissions
    (CreationTime, IsGranted, [Name], TenantId, RoleId, Discriminator)
SELECT
    GETDATE(),
    CAST(1 AS BIT),
    @PermissionName,
    role.TenantId,
    role.Id,
    N'RolePermissionSetting'
FROM AbpRoles role
WHERE role.TenantId IS NOT NULL
  AND role.[Name] IN
  (
      N'RestaurantManager',
      N'RestaurantSalesManager',
      N'RestaurantOperationsManager',
      N'RestaurantInventoryManager',
      N'RestaurantFinanceManager',
      N'RestaurantPayrollManager'
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM AbpPermissions existing
      WHERE existing.Discriminator = N'RolePermissionSetting'
        AND existing.RoleId = role.Id
        AND existing.TenantId = role.TenantId
        AND existing.[Name] = @PermissionName
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Role permissions remain tenant-editable after deployment. Removing this
            // grant during rollback could erase an administrator's later explicit choice.
        }
    }
}
