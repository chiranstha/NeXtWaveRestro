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
import { finalize, forkJoin, Subscription, timer } from 'rxjs';

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
    styleUrls: ['../restaurant/restaurant-shared.css', './dashboard.component.css'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    imports: [CommonModule, FormsModule, NgSelectModule, NepaliDatepickerModule, SubHeaderComponent, LocalizePipe],
})
export class DashboardComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly refreshIntervalMs = 5 * 60 * 1000;
    private readonly restaurantReportsService = inject(RestaurantReportsServiceProxy);
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
    tables: RestaurantTableDto[] = [];
    categories: RestaurantMenuCategoryDto[] = [];
    waiters: GetUserDropdownDto[] = [];
    lastRefreshedAt: DateTime | null = null;
    refreshSubscription: Subscription | undefined;

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
            summary: this.restaurantReportsService.getPosSalesSummary(fromDate, toDate, tableId, waiterUserId, categoryId),
            itemSales: this.restaurantReportsService.getItemSales(fromDate, toDate, tableId, waiterUserId, categoryId),
            tableSales: this.restaurantReportsService.getTableSales(fromDate, toDate, tableId, waiterUserId, categoryId),
            waiterSales: this.restaurantReportsService.getWaiterSales(fromDate, toDate, tableId, waiterUserId, categoryId),
            kotBotStatus: this.restaurantReportsService.getKotBotStatus(fromDate, toDate, tableId, waiterUserId, categoryId),
            settlements: this.restaurantReportsService.getSettlementReport(fromDate, toDate, tableId, waiterUserId, categoryId),
            wastage: this.restaurantReportsService.getWastageReport(fromDate, toDate, tableId, waiterUserId, categoryId),
            foodCosting: this.restaurantReportsService.getFoodCosting(fromDate, toDate, tableId, waiterUserId, categoryId),
            lowStock: this.restaurantReportsService.getLowStockReport(fromDate, toDate, tableId, waiterUserId, categoryId),
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

    get operationMetrics(): DashboardMetric[] {
        return [
            {
                label: this.l('Pending KOT/BOT'),
                value: this.formatInteger(this.pendingTicketCount),
                accent: 'warning',
                icon: 'fa-kitchen-set',
                subText: `${this.cancelledTicketCount} ${this.l('Cancelled')}`,
            },
            {
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
            },
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
        return Math.max(this.value(this.summary?.grandTotal), 1);
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
            tables: this.restaurantSetupService.getTables(null),
            categories: this.restaurantMenuService.getCategories(),
            waiters: this.reportingServiceProxy.getAllUserDropdown(),
        }).subscribe((result) => {
            this.tables = result.tables || [];
            this.categories = result.categories || [];
            this.waiters = result.waiters || [];
            this.cdr.markForCheck();
        });
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
