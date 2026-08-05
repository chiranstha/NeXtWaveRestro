import { Injectable } from '@angular/core';
import { WidgetFilterViewDefinition, WidgetViewDefinition } from './definitions';
import { DashboardCustomizationConst } from './DashboardCustomizationConsts';
import { WidgetGeneralStatsComponent } from './widgets/widget-general-stats/widget-general-stats.component';
import { WidgetDailySalesComponent } from './widgets/widget-daily-sales/widget-daily-sales.component';
import { WidgetMemberActivityComponent } from './widgets/widget-member-activity/widget-member-activity.component';
import { WidgetRegionalStatsComponent } from './widgets/widget-regional-stats/widget-regional-stats.component';
import { WidgetIncomeStatisticsComponent } from './widgets/widget-income-statistics/widget-income-statistics.component';
import { WidgetRecentTenantsComponent } from './widgets/widget-recent-tenants/widget-recent-tenants.component';
import { WidgetEditionStatisticsComponent } from './widgets/widget-edition-statistics/widget-edition-statistics.component';
import { WidgetSubscriptionExpiringTenantsComponent } from './widgets/widget-subscription-expiring-tenants/widget-subscription-expiring-tenants.component';
import { WidgetHostTopStatsComponent } from './widgets/widget-host-top-stats/widget-host-top-stats.component';
import { FilterDateRangePickerComponent } from './filters/filter-date-range-picker/filter-date-range-picker.component';
import { WidgetTopStatsComponent } from './widgets/widget-top-stats/widget-top-stats.component';
@Injectable({
    providedIn: 'root',
})
export class DashboardViewConfigurationService {
    public WidgetViewDefinitions: WidgetViewDefinition[] = [];
    public widgetFilterDefinitions: WidgetFilterViewDefinition[] = [];
    constructor() {
        this.initializeConfiguration();
    }
    private initializeConfiguration() {
        const filterDateRangePicker = new WidgetFilterViewDefinition(
            DashboardCustomizationConst.filters.filterDateRangePicker,
            FilterDateRangePickerComponent,
        );
        //add your filters here
        this.widgetFilterDefinitions.push(filterDateRangePicker);
        const generalStats = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.generalStats,
            WidgetGeneralStatsComponent,
            6,
            4,
        );
        const dailySales = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.genderClassStats,
            WidgetDailySalesComponent,
        );

        const memberActivity = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.memberActivity,
            WidgetMemberActivityComponent,
        );
        const regionalStats = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.regionalStats,
            WidgetRegionalStatsComponent,
        );
        const topStats = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.topStats,
            WidgetTopStatsComponent,
        );
        //add your tenant side widgets here

        const incomeStatistics = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.host.incomeStatistics,
            WidgetIncomeStatisticsComponent,
        );
        const editionStatistics = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.host.editionStatistics,
            WidgetEditionStatisticsComponent,
        );
        const recentTenants = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.host.recentTenants,
            WidgetRecentTenantsComponent,
        );
        const subscriptionExpiringTenants = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.host.subscriptionExpiringTenants,
            WidgetSubscriptionExpiringTenantsComponent,
        );
        const hostTopStats = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.host.topStats,
            WidgetHostTopStatsComponent,
        );
        //add your host side widgets here
        this.WidgetViewDefinitions.push(generalStats);
        this.WidgetViewDefinitions.push(dailySales);
        this.WidgetViewDefinitions.push(memberActivity);
        this.WidgetViewDefinitions.push(regionalStats);
        this.WidgetViewDefinitions.push(topStats);
        //add your tenant side widgets here
        this.WidgetViewDefinitions.push(incomeStatistics);
        this.WidgetViewDefinitions.push(editionStatistics);
        this.WidgetViewDefinitions.push(recentTenants);
        this.WidgetViewDefinitions.push(subscriptionExpiringTenants);
        this.WidgetViewDefinitions.push(hostTopStats);
    }
}
