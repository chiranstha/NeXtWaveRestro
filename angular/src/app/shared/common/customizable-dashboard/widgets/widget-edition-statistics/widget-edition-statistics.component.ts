import { Component, ElementRef, Injector, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { GetEditionTenantStatisticsOutput, HostDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { WidgetOnResizeEventHandler, WIDGETONRESIZEEVENTHANDLERTOKEN } from '../../customizable-dashboard.component';
import { PieChartModule } from '@swimlane/ngx-charts';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'app-widget-edition-statistics',
    templateUrl: './widget-edition-statistics.component.html',
    styleUrls: ['./widget-edition-statistics.component.css'],
    imports: [PieChartModule, LuxonFormatPipe, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class WidgetEditionStatisticsComponent extends WidgetComponentBaseComponent implements OnInit {
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    private _widgetOnResizeEventHandler = inject<WidgetOnResizeEventHandler>(WIDGETONRESIZEEVENTHANDLERTOKEN);
    @ViewChild('EditionStatisticsChart', { static: true }) editionStatisticsChart: ElementRef;
    selectedDateRange: DateTime[] = [];
    editionStatisticsHasData = false;
    editionStatisticsData;
    constructor() {
        const injector = inject(Injector);
        super();
        const { _widgetOnResizeEventHandler } = this;
        _widgetOnResizeEventHandler.onResize.subscribe(() => {
            this.runDelayed(this.showChart);
        });
    }
    ngOnInit(): void {
        this.subDateRangeFilter();
        this.runDelayed(this.showChart);
        this.selectedDateRange = [this._dateTimeService.getStartOfDay(), this._dateTimeService.getEndOfDay()];
    }
    showChart = () => {
        this._hostDashboardServiceProxy
            .getEditionTenantStatistics(this.selectedDateRange[0], this.selectedDateRange[1])
            .subscribe((editionTenantStatistics) => {
                this.editionStatisticsData = this.normalizeEditionStatisticsData(editionTenantStatistics);
                this.editionStatisticsHasData = this.editionStatisticsData.filter((data) => data.value > 0).length > 0;
            });
    };
    normalizeEditionStatisticsData(data: GetEditionTenantStatisticsOutput): Array<any> {
        if (!data?.editionStatistics || data.editionStatistics.length === 0) {
            return [];
        }
        const chartData = new Array(data.editionStatistics.length);
        for (let i = 0; i < data.editionStatistics.length; i++) {
            chartData[i] = {
                name: data.editionStatistics[i].label,
                value: data.editionStatistics[i].value,
            };
        }
        return chartData;
    }
    onDateRangeFilterChange = (dateRange) => {
        if (
            dateRange?.length !== 2 ||
            (this.selectedDateRange[0] === dateRange[0] && this.selectedDateRange[1] === dateRange[1])
        ) {
            return;
        }
        this.selectedDateRange[0] = dateRange[0];
        this.selectedDateRange[1] = dateRange[1];
        this.runDelayed(this.showChart);
    };
    subDateRangeFilter() {
        this.subscribeToEvent('app.dashboardFilters.dateRangePicker.onDateChange', this.onDateRangeFilterChange);
    }
}
