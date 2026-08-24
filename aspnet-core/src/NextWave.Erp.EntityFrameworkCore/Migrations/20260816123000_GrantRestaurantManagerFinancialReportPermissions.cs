using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextWave.Erp.EntityFrameworkCore;

#nullable disable

namespace NextWave.Erp.Migrations
{
    [DbContext(typeof(ErpDbContext))]
    [Migration("20260816123000_GrantRestaurantManagerFinancialReportPermissions")]
    public partial class GrantRestaurantManagerFinancialReportPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @FinanceReportPermissions TABLE ([Name] NVARCHAR(128) NOT NULL PRIMARY KEY);

INSERT INTO @FinanceReportPermissions ([Name])
VALUES
    (N'Pages.Reporting'),
    (N'Pages.AccountingReport'),
    (N'Pages.AccountGroupReport'),
    (N'Pages.AccountLedgerReport'),
    (N'Pages.BookReport'),
    (N'Pages.FinancialStatement'),
    (N'Pages.TrialBalanceReport'),
    (N'Pages.ProfitAndLossReport'),
    (N'Pages.PagesBalanceSheetReport'),
    (N'Pages.PageFundFlowReport'),
    (N'Pages.PagesCashFlowReport'),
    (N'Pages.TaxReporting'),
    (N'Pages.TaxSalesRegisterReport'),
    (N'Pages.TaxPurchaseRegisterReport'),
    (N'Pages.TdsReport'),
    (N'Pages.VatSummaryReport'),
    (N'Pages.GetPayTaxReport'),
    (N'Pages.TaxableCustomerReport'),
    (N'Pages.SalesAboveLakhsReport'),
    (N'Pages.PurchaseAboveLakhsReport'),
    (N'Pages.SalesTaxReport'),
    (N'Pages.PurchaseTaxReport');

UPDATE existing
SET existing.IsGranted = 1
FROM AbpPermissions existing
INNER JOIN AbpRoles role ON role.Id = existing.RoleId
INNER JOIN @FinanceReportPermissions permission ON permission.[Name] = existing.[Name]
WHERE existing.Discriminator = N'RolePermissionSetting'
  AND role.TenantId IS NOT NULL
  AND role.[Name] IN (N'RestaurantManager', N'RestaurantFinanceManager');

INSERT INTO AbpPermissions
    (CreationTime, IsGranted, [Name], TenantId, RoleId, Discriminator)
SELECT
    GETDATE(),
    CAST(1 AS BIT),
    permission.[Name],
    role.TenantId,
    role.Id,
    N'RolePermissionSetting'
FROM AbpRoles role
CROSS JOIN @FinanceReportPermissions permission
WHERE role.TenantId IS NOT NULL
  AND role.[Name] IN (N'RestaurantManager', N'RestaurantFinanceManager')
  AND NOT EXISTS
  (
      SELECT 1
      FROM AbpPermissions existing
      WHERE existing.Discriminator = N'RolePermissionSetting'
        AND existing.RoleId = role.Id
        AND existing.TenantId = role.TenantId
        AND existing.[Name] = permission.[Name]
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Permission assignments can be edited after deployment, so rollback must not
            // remove grants that may now be intentional tenant configuration.
        }
    }
}
