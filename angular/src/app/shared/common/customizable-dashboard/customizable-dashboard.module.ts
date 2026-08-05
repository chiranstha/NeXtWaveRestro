import { CommonModule } from '@angular/common';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { CustomizableDashboardComponent } from '@app/shared/common/customizable-dashboard/customizable-dashboard.component';
import { WidgetGeneralStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-general-stats/widget-general-stats.component';
import { DashboardViewConfigurationService } from '@app/shared/common/customizable-dashboard/dashboard-view-configuration.service';
import { Gridster } from 'angular-gridster2';
import { WidgetDailySalesComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-daily-sales/widget-daily-sales.component';
import { WidgetEditionStatisticsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-edition-statistics/widget-edition-statistics.component';
import { WidgetHostTopStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-host-top-stats/widget-host-top-stats.component';
import { WidgetIncomeStatisticsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-income-statistics/widget-income-statistics.component';
import { WidgetMemberActivityComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-member-activity/widget-member-activity.component';


import { WidgetRecentTenantsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-recent-tenants/widget-recent-tenants.component';
import { WidgetRegionalStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-regional-stats/widget-regional-stats.component';
import { WidgetSubscriptionExpiringTenantsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-subscription-expiring-tenants/widget-subscription-expiring-tenants.component';
import { WidgetTopStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-top-stats/widget-top-stats.component';
import { FilterDateRangePickerComponent } from '@app/shared/common/customizable-dashboard/filters/filter-date-range-picker/filter-date-range-picker.component';
import { AddWidgetModalComponent } from '@app/shared/common/customizable-dashboard/add-widget-modal/add-widget-modal.component';
import { AreaChartModule, BarChartModule, LineChartModule, PieChartModule } from '@swimlane/ngx-charts';
import { AgGridModule } from 'ag-grid-angular';
import { UtilsModule } from '@shared/utils/utils.module';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { ModalModule } from 'ngx-bootstrap/modal';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { NgScrollbarModule } from 'ngx-scrollbar';

import { Angular2CountoModule } from '@awaismirza/angular2-counto';
import { TableModule } from '@shared/ui-compat';

import { NepaliDatepickerModule } from '../nepalidatepicker/nepali-datepicker-angular.module';
import { NgSelectModule } from '@ng-select/ng-select';
@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        UtilsModule,
        Gridster,
        PieChartModule,
        AreaChartModule,
        LineChartModule,
        BarChartModule,
        BsDropdownModule,
        ModalModule,
        TabsModule,
        BsDatepickerModule,
        NgScrollbarModule,
        Angular2CountoModule,
        ReactiveFormsModule,
        FormsModule,
        TableModule,
        NepaliDatepickerModule,
        NgSelectModule,
        AgGridModule,
        CustomizableDashboardComponent,
        WidgetGeneralStatsComponent,
        WidgetDailySalesComponent,
        WidgetEditionStatisticsComponent,
        WidgetHostTopStatsComponent,
        WidgetIncomeStatisticsComponent,
        WidgetMemberActivityComponent,
        WidgetRecentTenantsComponent,
        WidgetRegionalStatsComponent,
        WidgetSubscriptionExpiringTenantsComponent,
        WidgetTopStatsComponent,
        FilterDateRangePickerComponent,
        AddWidgetModalComponent,
    ],
    providers: [DashboardViewConfigurationService],
    exports: [
        CustomizableDashboardComponent,
        WidgetGeneralStatsComponent,
        WidgetDailySalesComponent,
        WidgetEditionStatisticsComponent,
        WidgetHostTopStatsComponent,
        WidgetIncomeStatisticsComponent,
        WidgetMemberActivityComponent,
        WidgetRecentTenantsComponent,
        WidgetRegionalStatsComponent,
        WidgetSubscriptionExpiringTenantsComponent,
        WidgetTopStatsComponent,
        FilterDateRangePickerComponent,
        AddWidgetModalComponent,
    ],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA],
})
export class CustomizableDashboardModule {}
