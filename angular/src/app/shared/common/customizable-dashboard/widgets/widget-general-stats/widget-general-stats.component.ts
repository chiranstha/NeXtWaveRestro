import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Injector, OnInit, inject } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { DashboardChartBase } from '../dashboard-chart-base';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { PieChartModule } from '@swimlane/ngx-charts';
class GeneralStatsPieChart extends DashboardChartBase {
    public data = [];
    constructor(
        private _dashboardService: TenantDashboardServiceProxy,
        private _cdr: ChangeDetectorRef,
    ) {
        super();
    }
    init(transactionPercent, newVisitPercent, bouncePercent) {
        this.data = [
            {
                name: 'Operations',
                value: transactionPercent,
            },
            {
                name: 'New Visits',
                value: newVisitPercent,
            },
            {
                name: 'Bounce',
                value: bouncePercent,
            },
        ];
        this.hideLoading();
        this._cdr.markForCheck();
    }
    reload() {
        this.showLoading();
        this._dashboardService.getGeneralStats().subscribe((result) => {
            this.init(result.transactionPercent, result.newVisitPercent, result.bouncePercent);
        });
    }
}
@Component({
    selector: 'app-widget-general-stats',
    templateUrl: './widget-general-stats.component.html',
    styleUrls: ['./widget-general-stats.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [PieChartModule],
    schemas: [NO_ERRORS_SCHEMA],
})
export class WidgetGeneralStatsComponent extends WidgetComponentBaseComponent implements OnInit {
    private _dashboardService = inject(TenantDashboardServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    generalStatsPieChart: GeneralStatsPieChart;
    constructor() {
        const injector = inject(Injector);
        super();
        this.generalStatsPieChart = new GeneralStatsPieChart(this._dashboardService, this._cdr);
    }
    ngOnInit() {
        this.generalStatsPieChart.reload();
    }
}
