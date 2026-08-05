import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    OnDestroy,
    OnInit,
    ViewChild,
    inject,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import {
    Chart,
    ChartConfiguration,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    BarElement,
    Title,
    Tooltip,
    Legend,
    LineController,
    BarController,
    Filler,
} from 'chart.js';
import { DateTime } from 'luxon';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { NepaliDatepickerComponent } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.component';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef } from 'ag-grid-community';

// Register Chart.js components
Chart.register(
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    BarElement,
    Title,
    Tooltip,
    Legend,
    LineController,
    BarController,
    Filler,
);
@Component({
    selector: 'app-widget-member-activity',
    templateUrl: './widget-member-activity.component.html',
    styleUrls: ['./widget-member-activity.component.css'],
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [FormsModule, DecimalPipe, NepaliDatepickerComponent, AgGridAngular],
    schemas: [NO_ERRORS_SCHEMA],
})
export class WidgetMemberActivityComponent extends WidgetComponentBaseComponent implements OnInit, OnDestroy {
    private _tenantDashboardServiceProxy = inject(TenantDashboardServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('attendanceTrendChart', { static: false }) attendanceTrendChartRef: ElementRef;
    @ViewChild('classWiseChart', { static: false }) classWiseChartRef: ElementRef;
    @ViewChild('dailyAttendanceChart', { static: false }) dailyAttendanceChartRef: ElementRef;
    dashboardData: any;
    loading = false;
    // Charts
    attendanceTrendChart: Chart;
    classWiseChart: Chart;
    dailyAttendanceChart: Chart;
    // Filters
    selectedCourseId: string;
    selectedSectionId: string;
    startDate: DateTime;
    endDate: DateTime;
    dateRangeType = 'thisMonth';
    pageSize = 10;
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 120,
        flex: 1,
    };
    private attendanceRenderer = (params: any) => {
        const value = Number(params.value || 0);
        return `<span class="${this.getStatusColor(value)}">${this.formatPercent(value)}</span>`;
    };
    private alertTypeRenderer = (params: any) => {
        const percentage = Number(params.data?.attendancePercentage || 0);
        return `<span class="${this.getStatusBadgeClass(percentage)}">${params.value || ''}</span>`;
    };
    classSummaryColumnDefs: ColDef[] = [
        { headerName: this.l('Course'), field: 'courseName', minWidth: 180 },
        {
            headerName: this.l('Section'),
            field: 'sectionName',
            valueFormatter: (params) => params.value || '-',
        },
        { headerName: this.l('TotalStudents'), field: 'totalStudents', type: 'rightAligned' },
        { headerName: this.l('PresentToday'), field: 'presentToday', type: 'rightAligned' },
        { headerName: this.l('AbsentToday'), field: 'absentToday', type: 'rightAligned' },
        { headerName: this.l('LateToday'), field: 'lateToday', type: 'rightAligned' },
        {
            headerName: this.l('AttendancePercentage'),
            field: 'attendancePercentage',
            cellRenderer: this.attendanceRenderer,
            type: 'rightAligned',
        },
    ];
    criticalAlertColumnDefs: ColDef[] = [
        { headerName: this.l('StudentName'), field: 'studentName', minWidth: 180 },
        { headerName: this.l('RollNo'), field: 'rollNo' },
        { headerName: this.l('Course'), field: 'courseName', minWidth: 180 },
        { headerName: this.l('Section'), field: 'sectionName' },
        {
            headerName: this.l('AttendancePercentage'),
            field: 'attendancePercentage',
            cellRenderer: this.attendanceRenderer,
            type: 'rightAligned',
        },
        { headerName: this.l('ConsecutiveAbsents'), field: 'consecutiveAbsents', type: 'rightAligned' },
        {
            headerName: this.l('AlertType'),
            field: 'alertType',
            cellRenderer: this.alertTypeRenderer,
            minWidth: 150,
        },
    ];

    ngOnInit(): void {
        this.initializeFilters();
        this.loadDashboardData();
    }
    ngOnDestroy(): void {
        this.destroyCharts();
    }
    initializeFilters(): void {
        const now = DateTime.now();
        this.endDate = now.endOf('month');
        this.startDate = now.startOf('month');
    }
    loadDashboardData(): void {
        this.loading = true;
    }
    renderCharts(): void {
        this.destroyCharts();
        setTimeout(() => {
            this.renderAttendanceTrendChart();
            this.renderClassWiseChart();
            this.renderDailyAttendanceChart();
        }, 100);
    }
    renderAttendanceTrendChart(): void {
        if (!this.attendanceTrendChartRef?.nativeElement || !this.dashboardData?.trends?.monthlyTrends) {
            return;
        }
        const ctx = this.attendanceTrendChartRef.nativeElement.getContext('2d');
        const monthlyData = this.dashboardData.trends.monthlyTrends;
        const config: ChartConfiguration = {
            type: 'line',
            data: {
                labels: monthlyData.map((d) => d.monthName),
                datasets: [
                    {
                        label: this.l('AttendancePercentage'),
                        data: monthlyData.map((d) => d.attendancePercentage),
                        borderColor: '#3e95cd',
                        backgroundColor: 'rgba(62, 149, 205, 0.1)',
                        tension: 0.4,
                        fill: true,
                    },
                ],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: this.l('MonthlyAttendanceTrend'),
                    },
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100,
                        title: {
                            display: true,
                            text: this.l('AttendancePercentage'),
                        },
                    },
                },
            },
        };
        this.attendanceTrendChart = new Chart(ctx, config);
    }
    renderClassWiseChart(): void {
        if (!this.classWiseChartRef?.nativeElement || !this.dashboardData?.classSummaries) {
            return;
        }
        const ctx = this.classWiseChartRef.nativeElement.getContext('2d');
        const classData = this.dashboardData.classSummaries.slice(0, 10); // Top 10 classes
        const config: ChartConfiguration = {
            type: 'bar',
            data: {
                labels: classData.map((c) => c.courseName),
                datasets: [
                    {
                        label: this.l('AttendancePercentage'),
                        data: classData.map((c) => c.attendancePercentage),
                        backgroundColor: 'rgba(54, 162, 235, 0.6)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1,
                    },
                ],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false,
                    },
                    title: {
                        display: true,
                        text: this.l('CourseWiseAttendance'),
                    },
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100,
                        title: {
                            display: true,
                            text: this.l('AttendancePercentage'),
                        },
                    },
                },
            },
        };
        this.classWiseChart = new Chart(ctx, config);
    }
    renderDailyAttendanceChart(): void {
        if (!this.dailyAttendanceChartRef?.nativeElement || !this.dashboardData?.trends?.dailyTrends) {
            return;
        }
        const ctx = this.dailyAttendanceChartRef.nativeElement.getContext('2d');
        const dailyData = this.dashboardData.trends.dailyTrends.slice(-30); // Last 30 days
        const config: ChartConfiguration = {
            type: 'line',
            data: {
                labels: dailyData.map((d) => d.date.toFormat('MMM dd')),
                datasets: [
                    {
                        label: this.l('Present'),
                        data: dailyData.map((d) => d.presentCount),
                        borderColor: '#28a745',
                        backgroundColor: 'rgba(40, 167, 69, 0.1)',
                        tension: 0.4,
                    },
                    {
                        label: this.l('Absent'),
                        data: dailyData.map((d) => d.absentCount),
                        borderColor: '#dc3545',
                        backgroundColor: 'rgba(220, 53, 69, 0.1)',
                        tension: 0.4,
                    },
                ],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: this.l('DailyAttendanceCount'),
                    },
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: this.l('StudentCount'),
                        },
                    },
                },
            },
        };
        this.dailyAttendanceChart = new Chart(ctx, config);
    }
    destroyCharts(): void {
        if (this.attendanceTrendChart) {
            this.attendanceTrendChart.destroy();
            this.attendanceTrendChart = null;
        }
        if (this.classWiseChart) {
            this.classWiseChart.destroy();
            this.classWiseChart = null;
        }
        if (this.dailyAttendanceChart) {
            this.dailyAttendanceChart.destroy();
            this.dailyAttendanceChart = null;
        }
    }
    onDateRangeChange(): void {
        // Update start and end dates based on selected range
        const now = DateTime.now();
        switch (this.dateRangeType) {
            case 'today':
                this.startDate = now;
                this.endDate = now;
                break;
            case 'yesterday':
                this.startDate = now.minus({ days: 1 });
                this.endDate = now.minus({ days: 1 });
                break;
            case 'thisWeek':
                this.startDate = now.startOf('week');
                this.endDate = now.endOf('week');
                break;
            case 'thisMonth':
                this.startDate = now.startOf('month');
                this.endDate = now.endOf('month');
                break;
            case 'custom':
                // Keep existing dates
                break;
        }
        this.loadDashboardData();
    }
    setCustomDate(target: 'start' | 'end', date: string): void {
        const parsedDate = DateTime.fromISO(date.replace(/\//g, '-'));
        if (!parsedDate.isValid) {
            return;
        }
        if (target === 'start') {
            this.startDate = parsedDate.startOf('day');
        } else {
            this.endDate = parsedDate.endOf('day');
        }
        this.loadDashboardData();
    }
    refreshData(): void {
        this.loadDashboardData();
    }
    getStatusColor(percentage: number): string {
        if (percentage >= 90) {
            return 'text-success';
        }
        if (percentage >= 75) {
            return 'text-warning';
        }
        if (percentage >= 60) {
            return 'text-orange';
        }
        return 'text-danger';
    }
    getStatusBadgeClass(percentage: number): string {
        if (percentage >= 90) {
            return 'badge badge-success';
        }
        if (percentage >= 75) {
            return 'badge badge-warning';
        }
        if (percentage >= 60) {
            return 'badge badge-info';
        }
        return 'badge badge-danger';
    }
    private formatPercent(value: number): string {
        return `${value.toLocaleString(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
    }
}
