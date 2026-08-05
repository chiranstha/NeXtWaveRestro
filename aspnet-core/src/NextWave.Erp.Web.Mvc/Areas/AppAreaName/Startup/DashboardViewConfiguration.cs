using System.Collections.Generic;
using NextWave.Erp.Web.DashboardCustomization;


namespace NextWave.Erp.Web.Areas.AppAreaName.Startup;

public class DashboardViewConfiguration
{
    public Dictionary<string, WidgetViewDefinition> WidgetViewDefinitions { get; } = new Dictionary<string, WidgetViewDefinition>();

    public Dictionary<string, WidgetFilterViewDefinition> WidgetFilterViewDefinitions { get; } = new Dictionary<string, WidgetFilterViewDefinition>();

    public DashboardViewConfiguration()
    {
        var jsAndCssFileRoot = "/Areas/AppAreaName/Views/CustomizableDashboard/Widgets/";
        var viewFileRoot = "AppAreaName/Widgets/";

        #region FilterViewDefinitions

        WidgetFilterViewDefinitions.Add(ErpDashboardCustomizationConsts.Filters.FilterDateRangePicker,
            new WidgetFilterViewDefinition(
                ErpDashboardCustomizationConsts.Filters.FilterDateRangePicker,
                "~/Areas/AppAreaName/Views/Shared/Components/CustomizableDashboard/Widgets/DateRangeFilter.cshtml",
                jsAndCssFileRoot + "DateRangeFilter/DateRangeFilter.min.js",
                jsAndCssFileRoot + "DateRangeFilter/DateRangeFilter.min.css")
        );

        //add your filters iew definitions here
        #endregion

        #region WidgetViewDefinitions

        #region TenantWidgets

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Tenant.DailySales,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Tenant.DailySales,
                viewFileRoot + "DailySales",
                jsAndCssFileRoot + "DailySales/DailySales.min.js",
                jsAndCssFileRoot + "DailySales/DailySales.min.css"));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Tenant.GeneralStats,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Tenant.GeneralStats,
                viewFileRoot + "GeneralStats",
                jsAndCssFileRoot + "GeneralStats/GeneralStats.min.js",
                jsAndCssFileRoot + "GeneralStats/GeneralStats.min.css"));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Tenant.ProfitShare,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Tenant.ProfitShare,
                viewFileRoot + "ProfitShare",
                jsAndCssFileRoot + "ProfitShare/ProfitShare.min.js",
                jsAndCssFileRoot + "ProfitShare/ProfitShare.min.css"));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Tenant.MemberActivity,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Tenant.MemberActivity,
                viewFileRoot + "MemberActivity",
                jsAndCssFileRoot + "MemberActivity/MemberActivity.min.js",
                jsAndCssFileRoot + "MemberActivity/MemberActivity.min.css"));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Tenant.RegionalStats,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Tenant.RegionalStats,
                viewFileRoot + "RegionalStats",
                jsAndCssFileRoot + "RegionalStats/RegionalStats.min.js",
                jsAndCssFileRoot + "RegionalStats/RegionalStats.min.css",
                12,
                10));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Tenant.SalesSummary,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Tenant.SalesSummary,
                viewFileRoot + "SalesSummary",
                jsAndCssFileRoot + "SalesSummary/SalesSummary.min.js",
                jsAndCssFileRoot + "SalesSummary/SalesSummary.min.css",
                6,
                10));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Tenant.TopStats,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Tenant.TopStats,
                viewFileRoot + "TopStats",
                jsAndCssFileRoot + "TopStats/TopStats.min.js",
                jsAndCssFileRoot + "TopStats/TopStats.min.css",
                12,
                10));

        //add your tenant side widget definitions here
        #endregion

        #region HostWidgets

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Host.IncomeStatistics,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Host.IncomeStatistics,
                viewFileRoot + "IncomeStatistics",
                jsAndCssFileRoot + "IncomeStatistics/IncomeStatistics.min.js",
                jsAndCssFileRoot + "IncomeStatistics/IncomeStatistics.min.css"));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Host.TopStats,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Host.TopStats,
                viewFileRoot + "HostTopStats",
                jsAndCssFileRoot + "HostTopStats/HostTopStats.min.js",
                jsAndCssFileRoot + "HostTopStats/HostTopStats.min.css"));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Host.EditionStatistics,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Host.EditionStatistics,
                viewFileRoot + "EditionStatistics",
                jsAndCssFileRoot + "EditionStatistics/EditionStatistics.min.js",
                jsAndCssFileRoot + "EditionStatistics/EditionStatistics.min.css"));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Host.SubscriptionExpiringTenants,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Host.SubscriptionExpiringTenants,
                viewFileRoot + "SubscriptionExpiringTenants",
                jsAndCssFileRoot + "SubscriptionExpiringTenants/SubscriptionExpiringTenants.min.js",
                jsAndCssFileRoot + "SubscriptionExpiringTenants/SubscriptionExpiringTenants.min.css",
                6,
                10));

        WidgetViewDefinitions.Add(ErpDashboardCustomizationConsts.Widgets.Host.RecentTenants,
            new WidgetViewDefinition(
                ErpDashboardCustomizationConsts.Widgets.Host.RecentTenants,
                viewFileRoot + "RecentTenants",
                jsAndCssFileRoot + "RecentTenants/RecentTenants.min.js",
                jsAndCssFileRoot + "RecentTenants/RecentTenants.min.css"));

        //add your host side widgets definitions here
        #endregion

        #endregion
    }
}

