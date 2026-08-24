import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    Injector,
    OnDestroy,
    OnInit,
    ViewEncapsulation,
    inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    GetUserDropdownDto,
    ReportingServiceProxy,
    RestaurantFoodCostingReportDto,
    RestaurantItemSalesReportDto,
    RestaurantMenuCategoryDto,
    RestaurantMenuServiceProxy,
    RestaurantPosSalesSummaryDto,
    RestaurantReportsServiceProxy,
    RestaurantSettlementReportDto,
    RestaurantSetupServiceProxy,
    RestaurantTableDto,
    RestaurantTableSalesReportDto,
    RestaurantTicketStatusReportDto,
    RestaurantWaiterSalesReportDto,
    RestaurantWastageReportDto,
    RestaurantLowStockSuggestionDto,
} from '@shared/service-proxies/service-proxies';
import { NgSelectModule } from '@ng-select/ng-select';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { DateTime } from 'luxon';
import { finalize, forkJoin, of, Subscription, timer } from 'rxjs';
import { RestaurantStylesComponent } from '../restaurant/restaurant-styles.component';
import {
    RestaurantPayrollReportBundleDto,
    RestaurantReportsApiService,
} from '../restaurant/restaurant-reports/restaurant-reports-api.service';

interface RestaurantDashboardFilter {
    fromDate: string;
    toDate: string;
    tableId: string | null;
    waiterUserId: number | null;
    categoryId: string | null;
}

interface DashboardMetric {
    label: string;
    value: string;
    accent: string;
    icon: string;
    subText?: string;
}

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './dashboard.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    imports: [
        CommonModule,
        FormsModule,
        NgSelectModule,
        NepaliDatepickerModule,
        SubHeaderComponent,
        LocalizePipe,
        RestaurantStylesComponent,
    ],
    providers: [RestaurantReportsApiService],
})
export class DashboardComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly refreshIntervalMs = 5 * 60 * 1000;
    private readonly restaurantReportsService = inject(RestaurantReportsServiceProxy);
    private readonly restaurantReportsApiService = inject(RestaurantReportsApiService);
    private readonly restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private readonly restaurantMenuService = inject(RestaurantMenuServiceProxy);
    private readonly reportingServiceProxy = inject(ReportingServiceProxy);
    private readonly cdr = inject(ChangeDetectorRef);

    loading = false;
    filter: RestaurantDashboardFilter = this.createDefaultFilter();
    summary = new RestaurantPosSalesSummaryDto();
    itemSales: RestaurantItemSalesReportDto[] = [];
    tableSales: RestaurantTableSalesReportDto[] = [];
    waiterSales: RestaurantWaiterSalesReportDto[] = [];
    kotBotStatus: RestaurantTicketStatusReportDto[] = [];
    settlements: RestaurantSettlementReportDto[] = [];
    wastage: RestaurantWastageReportDto[] = [];
    foodCosting: RestaurantFoodCostingReportDto[] = [];
    lowStock: RestaurantLowStockSuggestionDto[] = [];
    payrollReport: RestaurantPayrollReportBundleDto = this.emptyPayrollReport();
    tables: RestaurantTableDto[] = [];
    categories: RestaurantMenuCategoryDto[] = [];
    waiters: GetUserDropdownDto[] = [];
    lastRefreshedAt: DateTime | null = null;
    refreshSubscription: Subscription | undefined;

    get canUseTableFilter(): boolean {
        return this.isGranted('Pages.Restaurant.Setup');
    }

    get canUseCategoryFilter(): boolean {
        return this.isGranted('Pages.Restaurant.Menu');
    }

    get canViewSalesReports(): boolean {
        return this.canViewReportCategory('Pages.Restaurant.Reports.Sales');
    }

    get canViewOperationsReports(): boolean {
        return this.canViewReportCategory('Pages.Restaurant.Reports.Operations');
    }

    get canViewInventoryReports(): boolean {
        return this.canViewReportCategory('Pages.Restaurant.Reports.Inventory');
    }

    get canViewPayrollReports(): boolean {
        return this.canViewReportCategory('Pages.Restaurant.Reports.Payroll');
    }

    get canViewAuditReports(): boolean {
        return this.canViewReportCategory('Pages.Restaurant.Reports.AuditFinance');
    }

    constructor(injector: Injector) {
        super(injector);
    }

    ngOnInit(): void {
        this.loadLookups();
        this.refresh();
        this.refreshSubscription = timer(this.refreshIntervalMs, this.refreshIntervalMs).subscribe(() => this.refresh());
    }

    ngOnDestroy(): void {
        this.refreshSubscription?.unsubscribe();
    }

    refresh(): void {
        this.loading = true;
        this.cdr.markForCheck();

        const fromDate = this.toDateTime(this.filter.fromDate);
        const toDate = this.toDateTime(this.filter.toDate);
        const tableId = this.filter.tableId || null;
        const waiterUserId = this.filter.waiterUserId || null;
        const categoryId = this.filter.categoryId || null;

        forkJoin({
            summary: this.canViewSalesReports ? this.restaurantReportsService.getPosSalesSummary(fromDate, toDate, tableId, waiterUserId, categoryId) : of(new RestaurantPosSalesSummaryDto()),
            itemSales: this.canViewSalesReports ? this.restaurantReportsService.getItemSales(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            tableSales: this.canViewOperationsReports ? this.restaurantReportsService.getTableSales(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            waiterSales: this.canViewOperationsReports ? this.restaurantReportsService.getWaiterSales(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            kotBotStatus: this.canViewOperationsReports ? this.restaurantReportsService.getKotBotStatus(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            settlements: this.canViewAuditReports ? this.restaurantReportsService.getSettlementReport(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            wastage: this.canViewInventoryReports ? this.restaurantReportsService.getWastageReport(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            foodCosting: this.canViewInventoryReports ? this.restaurantReportsService.getFoodCosting(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            lowStock: this.canViewInventoryReports ? this.restaurantReportsService.getLowStockReport(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            payrollReport: this.canViewPayrollReports
                ? this.restaurantReportsApiService.getPayrollReport(fromDate, toDate, tableId, waiterUserId, categoryId)
                : of(this.emptyPayrollReport()),
        })
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((result) => {
                this.summary = result.summary || new RestaurantPosSalesSummaryDto();
                this.itemSales = result.itemSales || [];
                this.tableSales = result.tableSales || [];
                this.waiterSales = result.waiterSales || [];
                this.kotBotStatus = result.kotBotStatus || [];
                this.settlements = result.settlements || [];
                this.wastage = result.wastage || [];
                this.foodCosting = result.foodCosting || [];
                this.lowStock = result.lowStock || [];
                this.payrollReport = result.payrollReport || this.emptyPayrollReport();
                this.lastRefreshedAt = DateTime.local();
                this.cdr.markForCheck();
            });
    }

    clearFilters(): void {
        this.filter = this.createDefaultFilter();
        this.refresh();
    }

    setFilterDate(field: 'fromDate' | 'toDate', dateInAd: string): void {
        this.filter[field] = this.normalizeDateValue(dateInAd);
    }

    get kpiMetrics(): DashboardMetric[] {
        if (!this.canViewSalesReports) {
            return [];
        }
        return [
            {
                label: this.l('Orders'),
                value: this.formatInteger(this.summary?.orderCount),
                accent: 'primary',
                icon: 'fa-receipt',
                subText: this.l('Today'),
            },
            {
                label: this.l('Sales'),
                value: this.formatMoney(this.summary?.grandTotal),
                accent: 'success',
                icon: 'fa-chart-line',
                subText: `${this.l('Net')}: ${this.formatMoney(this.summary?.netAmount)}`,
            },
            {
                label: this.l('Average Bill'),
                value: this.formatMoney(this.summary?.averageBill),
                accent: 'warning',
                icon: 'fa-calculator',
                subText: `${this.l('Tax')}: ${this.formatMoney(this.summary?.taxAmount)}`,
            },
            {
                label: this.l('Discounts'),
                value: this.formatMoney(this.summary?.discountAmount),
                accent: 'danger',
                icon: 'fa-tag',
                subText: `${this.l('Gross')}: ${this.formatMoney(this.summary?.grossAmount)}`,
            },
        ];
    }

    get decisionMetrics(): DashboardMetric[] {
        return [
            ...(this.canViewOperationsReports ? [{
                label: this.l('Pending KOT/BOT'),
                value: this.formatInteger(this.pendingTicketCount),
                accent: 'warning',
                icon: 'fa-kitchen-set',
                subText: `${this.cancelledTicketCount} ${this.l('Cancelled')}`,
            }] : []),
            ...(this.canViewInventoryReports ? [{
                label: this.l('Low Stock'),
                value: this.formatInteger(this.lowStock.length),
                accent: this.lowStock.length ? 'danger' : 'success',
                icon: 'fa-boxes-stacked',
                subText: `${this.missingSupplierMappingCount} ${this.l('Missing mappings')}`,
            },
            {
                label: this.l('Wastage'),
                value: this.formatMoney(this.totalWastageAmount),
                accent: 'danger',
                icon: 'fa-trash-can',
                subText: `${this.wastage.length} ${this.l('Entries')}`,
            },
            {
                label: this.l('Food Cost'),
                value: `${this.formatNumber(this.averageFoodCostPercent)}%`,
                accent: 'primary',
                icon: 'fa-bowl-food',
                subText: `${this.foodCosting.length} ${this.l('Items')}`,
            }] : []),
            ...(this.canViewPayrollReports ? [{
                label: this.l('Active Employees'),
                value: this.formatInteger(this.payrollReport.summary.activeEmployeeCount),
                accent: 'primary',
                icon: 'fa-users',
                subText: `${this.payrollReport.summary.attendanceRecordCount} ${this.l('Attendance records')}`,
            }, {
                label: this.l('Net Payroll'),
                value: this.formatMoney(this.payrollReport.summary.totalNet),
                accent: 'success',
                icon: 'fa-money-check-dollar',
                subText: `${this.payrollReport.summary.payrollRunCount} ${this.l('Pay runs')}`,
            }] : []),
            ...(this.canViewAuditReports ? [{
                label: this.l('Settled Sales'),
                value: this.formatMoney(this.totalSettledSales),
                accent: 'success',
                icon: 'fa-money-bill-transfer',
                subText: `${this.settledOrderCount} ${this.l('Orders')}`,
            }, {
                label: this.l('Settlement Discounts'),
                value: this.formatMoney(this.totalSettlementDiscounts),
                accent: 'danger',
                icon: 'fa-tags',
                subText: `${this.settlements.length} ${this.l('Payment methods')}`,
            }] : []),
        ];
    }

    get topItems(): RestaurantItemSalesReportDto[] {
        return [...this.itemSales].sort((a, b) => this.value(b.grandTotal) - this.value(a.grandTotal)).slice(0, 5);
    }

    get topTables(): RestaurantTableSalesReportDto[] {
        return [...this.tableSales].sort((a, b) => this.value(b.grandTotal) - this.value(a.grandTotal)).slice(0, 5);
    }

    get topWaiters(): RestaurantWaiterSalesReportDto[] {
        return [...this.waiterSales].sort((a, b) => this.value(b.grandTotal) - this.value(a.grandTotal)).slice(0, 5);
    }

    get topPayments(): RestaurantSettlementReportDto[] {
        return [...this.settlements].sort((a, b) => this.value(b.grandTotal) - this.value(a.grandTotal)).slice(0, 5);
    }

    get urgentLowStock(): RestaurantLowStockSuggestionDto[] {
        return [...this.lowStock]
            .sort((a, b) => Number(b.missingSupplierMapping) - Number(a.missingSupplierMapping) || this.value(a.availableQty) - this.value(b.availableQty))
            .slice(0, 6);
    }

    get totalSalesForShare(): number {
        return Math.max(this.totalSettledSales, 1);
    }

    get totalSettledSales(): number {
        return this.settlements.reduce((sum, row) => sum + this.value(row.grandTotal), 0);
    }

    get totalSettlementDiscounts(): number {
        return this.settlements.reduce((sum, row) => sum + this.value(row.discountAmount), 0);
    }

    get settledOrderCount(): number {
        return this.settlements.reduce((sum, row) => sum + this.value(row.orderCount), 0);
    }

    get pendingTicketCount(): number {
        return this.kotBotStatus.filter((ticket) => (ticket.status || '').toLowerCase() === 'pending').length;
    }

    get cancelledTicketCount(): number {
        return this.kotBotStatus.filter((ticket) => (ticket.status || '').toLowerCase() === 'cancelled').length;
    }

    get missingSupplierMappingCount(): number {
        return this.lowStock.filter((item) => item.missingSupplierMapping).length;
    }

    get totalWastageAmount(): number {
        return this.wastage.reduce((sum, row) => sum + this.value(row.amount), 0);
    }

    get averageFoodCostPercent(): number {
        if (!this.foodCosting.length) {
            return 0;
        }

        return this.foodCosting.reduce((sum, row) => sum + this.value(row.foodCostPercent), 0) / this.foodCosting.length;
    }

    paymentShare(payment: RestaurantSettlementReportDto): number {
        return Math.min(100, Math.max(0, (this.value(payment.grandTotal) / this.totalSalesForShare) * 100));
    }

    private loadLookups(): void {
        forkJoin({
            tables: this.canUseTableFilter ? this.restaurantSetupService.getTables(null) : of([]),
            categories: this.canUseCategoryFilter ? this.restaurantMenuService.getCategories() : of([]),
            waiters: this.reportingServiceProxy.getAllUserDropdown(),
        }).subscribe((result) => {
            this.tables = result.tables || [];
            this.categories = result.categories || [];
            this.waiters = result.waiters || [];
            this.cdr.markForCheck();
        });
    }

    private canViewReportCategory(permission: string): boolean {
        const categoryPermissions = [
            'Pages.Restaurant.Reports.Sales',
            'Pages.Restaurant.Reports.Operations',
            'Pages.Restaurant.Reports.Inventory',
            'Pages.Restaurant.Reports.Payroll',
            'Pages.Restaurant.Reports.AuditFinance',
        ];
        const hasSpecificCategory = categoryPermissions.some((item) => this.isGranted(item));
        return !hasSpecificCategory || this.isGranted(permission);
    }

    private emptyPayrollReport(): RestaurantPayrollReportBundleDto {
        return {
            summary: {
                activeEmployeeCount: 0,
                payrollRunCount: 0,
                attendanceRecordCount: 0,
                totalGross: 0,
                totalDeduction: 0,
                totalNet: 0,
                totalOvertimeHours: 0,
            },
            runs: [],
            employeeCosts: [],
            attendance: [],
        };
    }

    private createDefaultFilter(): RestaurantDashboardFilter {
        const today = DateTime.local().toISODate() || '';
        return {
            fromDate: today,
            toDate: today,
            tableId: null,
            waiterUserId: null,
            categoryId: null,
        };
    }

    private toDateTime(value: string | null | undefined): DateTime | null {
        return value ? DateTime.fromISO(value) : null;
    }

    private normalizeDateValue(value: string | null | undefined): string {
        return value ? value.replace(/\//g, '-') : '';
    }

    private finishLoading(): void {
        this.loading = false;
        this.cdr.markForCheck();
    }

    formatMoney(value: number | null | undefined): string {
        return this.formatNumber(value, 2);
    }

    formatInteger(value: number | null | undefined): string {
        return this.formatNumber(value, 0);
    }

    formatNumber(value: number | null | undefined, fractionDigits = 2): string {
        return this.value(value).toLocaleString(undefined, {
            minimumFractionDigits: fractionDigits,
            maximumFractionDigits: fractionDigits,
        });
    }

    private value(value: number | null | undefined): number {
        const numberValue = Number(value || 0);
        return Number.isNaN(numberValue) ? 0 : numberValue;
    }
}
