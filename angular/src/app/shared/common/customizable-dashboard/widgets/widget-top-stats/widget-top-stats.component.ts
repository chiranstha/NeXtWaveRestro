import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ReportingServiceProxy, TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { DashboardChartBase } from '../dashboard-chart-base';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { curveBasis } from 'd3-shape';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
class DashboardTopStats extends DashboardChartBase {}
type StatCardVariant = 'primary' | 'success' | 'info' | 'warning' | 'danger' | 'neutral';
interface StatCard {
    label: string;
    value?: string;
    icon: string;
    paths: string[];
    variant: StatCardVariant;
    chip: string;
    description?: string;
    trend?: number;
    trendDisplay?: string;
    progress?: number;
}
@Component({
    selector: 'app-widget-top-stats',
    templateUrl: './widget-top-stats.component.html',
    styleUrls: ['./widget-top-stats.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [NgClass],
    schemas: [NO_ERRORS_SCHEMA],
})
export class WidgetTopStatsComponent extends WidgetComponentBaseComponent implements OnInit {
    private router = inject(Router);
    private _tenantDashboardServiceProxy = inject(TenantDashboardServiceProxy);
    private _proxy = inject(ReportingServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    dashboardTopStats: DashboardTopStats;
    change: any[];
    view: any[] = [700, 400];
    customColors = ['#00c5dc', '#f4516c', '#34bfa3', '#ffb822'];
    cardColor = '#232837';
    curve: any = curveBasis;
    expensesData: string;

    todayFeeData: string;
    cashData: string;
    bankData: string;
    totalFeeData: string;
    studentData: string;
    checkFinancialYearStatus: number;
    checkAcademicYearStatus: number;
    statCards: StatCard[] = [];
    readonly skeletonSlots = Array.from({ length: 6 }, (_, index) => index);
    constructor() {
        super();
        this.dashboardTopStats = new DashboardTopStats();
    }
    ngOnInit() {
        this._proxy.getFinancialYears().subscribe({
            next: (financialYear) => {
                if (!financialYear?.fromDate || !financialYear?.toDate) {
                    this.handleMissingFinancialYear();
                    return;
                }
            },
            error: () => {
                this.handleDashboardLoadError();
            },
        });
    }
    private handleDashboardLoadError(): void {
        this.statCards = [];
        this.dashboardTopStats.hideLoading();
        this._cdr.markForCheck();
        this.notify.error(this.l('ErrorLoadingDashboardData'));
    }
    private handleMissingFinancialYear(): void {
        this.statCards = [];
        this.dashboardTopStats.hideLoading();
        this._cdr.markForCheck();
        this.message.confirm('Please create Financial Year', this.l('Financial Year not found'), (isConfirmed) => {
            if (isConfirmed) {
                void this.router.navigate(['app/main/accounting/financialYears']);
            }
        });
    }
    private buildStatCards(): void {
        this.statCards = [
            {
                label: 'Today Fee Collection',
                value: this.todayFeeData,
                icon: 'fa-money-bill-simple-wave',
                paths: ['path1', 'path2'],
                variant: 'success',
                chip: 'Daily',
                trend: 12, // Example trend
                progress: 75,
            },
            {
                label: 'Total Fee Collection',
                value: this.totalFeeData,
                icon: 'fa-chart-simple',
                paths: ['path1', 'path2', 'path3', 'path4'],
                variant: 'primary',
                chip: 'Cumulative',
                trend: 5,
                progress: 60,
            },
            {
                label: 'Total Student',
                value: this.studentData,
                icon: 'fa-user',
                paths: ['path1', 'path2'],
                variant: 'warning',
                chip: 'Active',
                trend: 2.5,
            },
            {
                label: 'Total Cash',
                value: this.cashData,
                icon: 'fa-map',
                paths: ['path1', 'path2', 'path3'],
                variant: 'info',
                chip: 'Cash',
                trend: -1.2,
            },
            {
                label: 'Total Bank',
                value: this.bankData,
                icon: 'fa-cards-blank',
                paths: ['path1', 'path2'],
                variant: 'danger',
                chip: 'Banked',
                trend: 0,
            },
        ];
    }
    trackByLabel(_: number, card: StatCard): string {
        return card.label;
    }
}
