using System.Linq;
using Abp.Authorization.Roles;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Migrations.Seed.Tenants;
using NextWave.Erp.MultiTenancy;
using NextWave.Erp.Test.Base;
using Shouldly;
using Xunit;

namespace NextWave.Erp.Tests.Authorization.Roles;

public class Restaurant_Role_Permission_Tests : AppTestBase
{
    [Fact]
    public void Restaurant_specialist_managers_should_default_to_their_report_category_only()
    {
        var tenantId = UsingDbContext(context =>
        {
            var tenant = context.Tenants.Add(new Tenant("RestaurantManagerProfiles", "Restaurant Manager Profiles")).Entity;
            context.SaveChanges();
            return tenant.Id;
        });

        UsingDbContext(context => new TenantRoleAndUserBuilder(context, tenantId).Create());

        AssertRolePermissions(
            tenantId,
            StaticRoleNames.Tenants.RestaurantSalesManager,
            AppPermissions.PagesRestaurantReportsSales);
        AssertRolePermissions(
            tenantId,
            StaticRoleNames.Tenants.RestaurantOperationsManager,
            AppPermissions.PagesRestaurantReportsOperations);
        AssertRolePermissions(
            tenantId,
            StaticRoleNames.Tenants.RestaurantInventoryManager,
            AppPermissions.PagesRestaurantReportsInventory);
        AssertRolePermissions(
            tenantId,
            StaticRoleNames.Tenants.RestaurantFinanceManager,
            AppPermissions.PagesRestaurantReportsAuditFinance,
            FinanceReportPermissions);
        AssertRolePermissions(
            tenantId,
            StaticRoleNames.Tenants.RestaurantPayrollManager,
            AppPermissions.PagesRestaurantReportsPayroll);
    }

    [Fact]
    public void Restaurant_manager_should_default_to_reports_and_preserve_role_permission_edits()
    {
        var tenantId = UsingDbContext(context =>
        {
            var tenant = context.Tenants.Add(new Tenant("RestaurantRoleTest", "Restaurant Role Test")).Entity;
            context.SaveChanges();
            return tenant.Id;
        });

        UsingDbContext(context => new TenantRoleAndUserBuilder(context, tenantId).Create());

        GetManagerGrantedPermissions(tenantId).ShouldBe(new[]
        {
            AppPermissions.PagesRestaurant,
            AppPermissions.PagesRestaurantReports,
            AppPermissions.PagesRestaurantReportsAuditFinance,
            AppPermissions.PagesRestaurantReportsInventory,
            AppPermissions.PagesRestaurantReportsOperations,
            AppPermissions.PagesRestaurantReportsPayroll,
            AppPermissions.PagesRestaurantReportsSales,
            AppPermissions.PagesFinancialYears
        }.Concat(FinanceReportPermissions).OrderBy(permission => permission, System.StringComparer.Ordinal).ToArray());

        UsingDbContext(context =>
        {
            var managerRole = context.Roles.IgnoreQueryFilters().Single(role =>
                role.TenantId == tenantId && role.Name == StaticRoleNames.Tenants.RestaurantManager);
            var reportPermissions = context.Permissions.IgnoreQueryFilters()
                .OfType<RolePermissionSetting>()
                .Where(permission =>
                    permission.TenantId == tenantId &&
                    permission.RoleId == managerRole.Id &&
                    permission.Name.StartsWith(AppPermissions.PagesRestaurantReports))
                .ToList();
            context.Permissions.RemoveRange(reportPermissions);
            context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = tenantId,
                RoleId = managerRole.Id,
                Name = AppPermissions.PagesRestaurantPos,
                IsGranted = true
            });
        });

        UsingDbContext(context => new TenantRoleAndUserBuilder(context, tenantId).Create());

        GetManagerGrantedPermissions(tenantId).ShouldBe(new[]
        {
            AppPermissions.PagesRestaurant,
            AppPermissions.PagesRestaurantPos,
            AppPermissions.PagesFinancialYears
        }.Concat(FinanceReportPermissions).OrderBy(permission => permission, System.StringComparer.Ordinal).ToArray());
    }

    private string[] GetManagerGrantedPermissions(int tenantId)
    {
        return UsingDbContext(context =>
        {
            var managerRoleId = context.Roles.IgnoreQueryFilters()
                .Where(role => role.TenantId == tenantId && role.Name == StaticRoleNames.Tenants.RestaurantManager)
                .Select(role => role.Id)
                .Single();

            return context.Permissions.IgnoreQueryFilters()
                .OfType<RolePermissionSetting>()
                .Where(permission =>
                    permission.TenantId == tenantId &&
                    permission.RoleId == managerRoleId &&
                    permission.IsGranted)
                .Select(permission => permission.Name)
                .ToArray()
                .OrderBy(permission => permission, System.StringComparer.Ordinal)
                .ToArray();
        });
    }

    private void AssertRolePermissions(
        int tenantId,
        string roleName,
        string reportPermission,
        params string[] additionalPermissions)
    {
        UsingDbContext(context =>
        {
            var role = context.Roles.IgnoreQueryFilters().Single(candidate =>
                candidate.TenantId == tenantId && candidate.Name == roleName);
            role.DisplayName.ShouldNotBeNullOrWhiteSpace();

            var permissions = context.Permissions.IgnoreQueryFilters()
                .OfType<RolePermissionSetting>()
                .Where(permission =>
                    permission.TenantId == tenantId &&
                    permission.RoleId == role.Id &&
                    permission.IsGranted)
                .Select(permission => permission.Name)
                .ToArray()
                .OrderBy(permission => permission, System.StringComparer.Ordinal)
                .ToArray();

            permissions.ShouldBe(new[]
            {
                AppPermissions.PagesRestaurant,
                AppPermissions.PagesRestaurantReports,
                reportPermission,
                AppPermissions.PagesFinancialYears
            }.Concat(additionalPermissions).OrderBy(permission => permission, System.StringComparer.Ordinal).ToArray());
        });
    }

    private static readonly string[] FinanceReportPermissions =
    {
        AppPermissions.PagesReporting,
        AppPermissions.PagesAccountingReporting,
        AppPermissions.PagesAccountGroupReport,
        AppPermissions.PagesAccountLedgerReport,
        AppPermissions.PagesBookReport,
        AppPermissions.PagesFinancialStatement,
        AppPermissions.PagesTrialBalanceReport,
        AppPermissions.PagesProfitAndLossReport,
        AppPermissions.PagesBalanceSheetReport,
        AppPermissions.PageFundFlowReport,
        AppPermissions.PagesCashFlowReport,
        AppPermissions.PagesTaxReporting,
        AppPermissions.PagesTaxSalesRegisterReport,
        AppPermissions.PagesTaxPurchaseRegisterReport,
        AppPermissions.PagesTdsReport,
        AppPermissions.PagesVatSummaryReport,
        AppPermissions.PagesGetPayTaxReport,
        AppPermissions.PagesTaxableCustomerReport,
        AppPermissions.PagesSalesAboveLakhsReport,
        AppPermissions.PagesPurchaseAboveLakhsReport,
        AppPermissions.PagesSalesTaxReport,
        AppPermissions.PagesPurchaseTaxReport
    };
}
