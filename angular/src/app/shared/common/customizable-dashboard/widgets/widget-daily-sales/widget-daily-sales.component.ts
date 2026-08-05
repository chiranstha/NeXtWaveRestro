import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { AgCharts } from 'ag-charts-angular';
@Component({
    selector: 'app-widget-daily-sales',
    templateUrl: './widget-daily-sales.component.html',
    styleUrls: ['./widget-daily-sales.component.css'],
    imports: [AgCharts],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class WidgetDailySalesComponent extends WidgetComponentBaseComponent implements OnInit {
    private _dashboardService = inject(TenantDashboardServiceProxy);
    chartData: ChartData[] = [];
    chartOptions: any;
    loading = true;
    chartReady = false;
    // Flag to indicate if chart data and options are fully prepared

    // Theme support
    isDarkMode(): boolean {
        return this.currentTheme.baseSettings.layout.darkMode;
    }
    getChartColors() {
        return {
            backgroundFill: this.isDarkMode() ? '#1e1e2d' : '#ffffff',
            titleColor: this.isDarkMode() ? '#ffffff' : '#181C32',
            labelColor: this.isDarkMode() ? '#a1a5b7' : '#5E6278',
            axisColor: this.isDarkMode() ? '#3f4254' : '#e4e6ef',
            gridColor: this.isDarkMode() ? '#2b2b40' : '#f1f3ff',
            tooltipBackground: this.isDarkMode() ? '#1e1e2d' : '#ffffff',
            tooltipTextColor: this.isDarkMode() ? '#ffffff' : '#181C32',
            maleColor: this.isDarkMode() ? '#3699ff' : '#4285F4',
            femaleColor: this.isDarkMode() ? '#f1416c' : '#EA4335',
            totalColor: this.isDarkMode() ? '#50cd89' : '#34A853',
        };
    }
    totalStudents: number = 0;
    totalMale: number = 0;
    totalFemale: number = 0;
    ngOnInit() {
        // Initialize chart options with empty series array to prevent "undefined series" error
        this.chartOptions = {
            series: [],
            padding: {
                top: 20,
                right: 20,
                bottom: 20,
                left: 20,
            },
        };
        this.loading = true; // Set loading to true until data is loaded
        this.loadChartData();
    }
    loadChartData(): void {
        // Ensure chartOptions has series initialized before data loads
        if (!this.chartOptions) {
            this.chartOptions = { series: [] };
        }
    }
    prepareChartData(): void {
        // Calculate total students
        this.totalStudents = this.chartData.reduce((sum, item) => sum + item.value, 0);
        this.totalMale = this.chartData
            .filter((item) => item.name === 'Male')
            .reduce((sum, item) => sum + item.value, 0);
        this.totalFemale = this.chartData
            .filter((item) => item.name === 'Female')
            .reduce((sum, item) => sum + item.value, 0);
        // Get theme-based colors
        const colors = this.getChartColors();
        const isDark = this.isDarkMode();
        // Group data by class
        const classes = [...new Set(this.chartData.map((item) => item.class))];
        // Define class order for proper sorting
        const classOrder = [
            'NURSERY',
            'L.K.G',
            'U.K.G',
            'ONE',
            'TWO',
            'THREE',
            'FOUR',
            'FIVE',
            'SIX',
            'SEVEN',
            'EIGHT',
            'NINE',
            'TEN',
        ];
        // Transform data for chart consumption
        let chartData = classes.map((className) => {
            const classData = this.chartData.filter((item) => item.class === className);
            const maleData = classData.find((item) => item.name === 'Male')?.value || 0;
            const femaleData = classData.find((item) => item.name === 'Female')?.value || 0;
            return {
                class: className,
                male: maleData,
                female: femaleData,
                total: maleData + femaleData,
            };
        });
        // Sort data by class according to our defined order
        chartData = chartData.sort((a, b) => {
            return classOrder.indexOf(a.class) - classOrder.indexOf(b.class);
        });
        // Create series data for Male and Female with theme-aware colors and gradients
        // Create series data for Male and Female with theme-aware colors and premium effects
        const maleSeries = {
            type: 'bar',
            xKey: 'class',
            yKey: 'male',
            yName: 'Male',
            fill: colors.maleColor,
            strokeWidth: 0,
            cornerRadius: 6,
            shadow: {
                enabled: true,
                color: isDark ? 'rgba(0,0,0,0.5)' : 'rgba(54, 153, 255, 0.4)',
                xOffset: 0,
                yOffset: 8,
                blur: 12,
            },
            tooltip: {
                renderer: (params: any) => {
                    return {
                        title: params.datum.class,
                        content: `Male: ${params.datum.male}`,
                        backgroundColor: isDark ? '#1e1e2d' : '#ffffff',
                        color: isDark ? '#ffffff' : '#3F4254',
                    };
                },
            },
        };
        const femaleSeries = {
            type: 'bar',
            xKey: 'class',
            yKey: 'female',
            yName: 'Female',
            fill: colors.femaleColor,
            strokeWidth: 0,
            cornerRadius: 6,
            shadow: {
                enabled: true,
                color: isDark ? 'rgba(0,0,0,0.5)' : 'rgba(241, 65, 108, 0.4)',
                xOffset: 0,
                yOffset: 8,
                blur: 12,
            },
            tooltip: {
                renderer: (params: any) => {
                    return {
                        title: params.datum.class,
                        content: `Female: ${params.datum.female}`,
                        backgroundColor: isDark ? '#1e1e2d' : '#ffffff',
                        color: isDark ? '#ffffff' : '#3F4254',
                    };
                },
            },
        };
        const totalSeries = {
            type: 'line',
            xKey: 'class',
            yKey: 'total',
            yName: 'Total',
            interpolation: { type: 'smooth' }, // Smooth curve
            stroke: colors.totalColor,
            strokeWidth: 4,
            marker: {
                enabled: true,
                size: 8,
                fill: isDark ? '#1e1e2d' : '#ffffff',
                stroke: colors.totalColor,
                strokeWidth: 3,
                shape: 'circle',
            },
            tooltip: {
                renderer: (params: any) => {
                    return {
                        title: params.datum.class,
                        content: `Total: ${params.datum.total}`,
                        backgroundColor: isDark ? '#1e1e2d' : '#ffffff',
                        color: isDark ? '#ffffff' : '#3F4254',
                    };
                },
            },
        };
        // Update chartOptions with the proper structure
        this.chartOptions = {
            data: chartData,
            series: [maleSeries, femaleSeries, totalSeries],
            height: 350,
            padding: {
                top: 20,
                right: 30,
                bottom: 20,
                left: 20,
            },
            background: {
                fill: 'transparent', // Let container background show through
            },
            title: {
                enabled: false, // We will use a custom HTML header
            },
            subtitle: {
                enabled: false,
            },
            legend: {
                enabled: true,
                position: 'top',
                spacing: 20,
                item: {
                    marker: {
                        shape: 'circle',
                        size: 10,
                        strokeWidth: 0,
                    },
                    label: {
                        color: colors.labelColor,
                        fontSize: 13,
                        fontFamily: 'Inter, sans-serif',
                        fontWeight: 500,
                    },
                },
            },
            axes: {
                bottom: {
                    type: 'category',
                    position: 'bottom',
                    label: {
                        color: colors.labelColor,
                        fontSize: 11,
                        fontFamily: 'Inter, sans-serif',
                        spacing: 10,
                    },
                    line: {
                        width: 1,
                        stroke: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.05)',
                    },
                    crosshair: {
                        enabled: true,
                        stroke: isDark ? 'rgba(255,255,255,0.2)' : 'rgba(0,0,0,0.1)',
                    },
                },
                left: {
                    type: 'number',
                    position: 'left',
                    label: {
                        color: colors.labelColor,
                        fontSize: 11,
                        fontFamily: 'Inter, sans-serif',
                    },
                    line: {
                        width: 0,
                    },
                },
            },
            tooltip: {
                enabled: true,
                delay: 0,
            },
            theme: isDark ? 'ag-default-dark' : 'ag-default',
        };
        this.chartReady = true;
    }
}
export interface ChartData {
    name: string;
    class: string;
    value: number;
}
