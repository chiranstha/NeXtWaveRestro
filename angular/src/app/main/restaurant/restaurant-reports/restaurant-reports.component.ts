import {
    AfterViewInit,
    ChangeDetectorRef,
    Component,
    Injector,
    OnInit,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetUserDropdownDto,
    ReportingServiceProxy,
    RestaurantItemSalesReportDto,
    RestaurantMaterialConsumptionReportDto,
    RestaurantMenuCategoryDto,
    RestaurantMenuServiceProxy,
    RestaurantPosSalesSummaryDto,
    RestaurantReportsServiceProxy,
    RestaurantSetupServiceProxy,
    RestaurantTableDto,
    RestaurantTableSalesReportDto,
    RestaurantWaiterSalesReportDto,
} from '@shared/service-proxies/service-proxies';
import { ColDef, ValueFormatterParams, ValueGetterParams } from 'ag-grid-community';
import { DateTime } from 'luxon';
import { finalize, forkJoin, of } from 'rxjs';
import {
    RestaurantDailySalesSummaryReportDto,
    RestaurantDiscountReportDto,
    RestaurantItemMarginReportDto,
    RestaurantFoodCostingReportDto,
    RestaurantLowStockReportDto,
    RestaurantPayrollReportBundleDto,
    RestaurantRecipeCostingReportDto,
    RestaurantReportsApiService,
    RestaurantSettlementReportDto,
    RestaurantTableTurnoverReportDto,
    RestaurantTicketStatusReportDto,
    RestaurantVoidAuditReportDto,
    RestaurantWastageReportDto,
    RestaurantWaiterPerformanceReportDto,
} from './restaurant-reports-api.service';

interface RestaurantReportFilter {
    fromDate: string;
    toDate: string;
    tableId: string | null;
    waiterUserId: number | null;
    categoryId: string | null;
}

interface RestaurantAgGridReport {
    type: RestaurantReportType;
    title: string;
    columnDefs: ColDef[];
    rowData: () => object[];
    height: number;
}

interface RestaurantReportKpi {
    label: string;
    value: string;
}

type RestaurantReportType = 'sales' | 'operations' | 'inventory' | 'payroll' | 'audit';

@Component({
    selector: 'restaurant-reports',
    templateUrl: './restaurant-reports.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    providers: [RestaurantReportsApiService],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantReportsComponent extends AppComponentBase implements OnInit, AfterViewInit {
    loading = false;
    filter: RestaurantReportFilter = this.createEmptyFilter();
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 120,
    };
    noRowsOverlayTemplate = '';
    reportGridSections: RestaurantAgGridReport[] = [];
    readonly reportTypes: Array<{ key: RestaurantReportType; label: string; icon: string }> = [
        { key: 'sales', label: 'Sales', icon: 'fa-chart-line' },
        { key: 'operations', label: 'Operations', icon: 'fa-people-group' },
        { key: 'inventory', label: 'Inventory & Cost', icon: 'fa-boxes-stacked' },
        { key: 'payroll', label: 'Payroll & Attendance', icon: 'fa-money-check-dollar' },
        { key: 'audit', label: 'Audit & Finance', icon: 'fa-shield-halved' },
    ];
    private readonly reportPermissionByType: Record<RestaurantReportType, string> = {
        sales: 'Pages.Restaurant.Reports.Sales',
        operations: 'Pages.Restaurant.Reports.Operations',
        inventory: 'Pages.Restaurant.Reports.Inventory',
        payroll: 'Pages.Restaurant.Reports.Payroll',
        audit: 'Pages.Restaurant.Reports.AuditFinance',
    };
    activeReportType: RestaurantReportType = 'sales';
    primaryChartTitle = '';
    secondaryChartTitle = '';
    primaryChartOptions: any = { data: [], series: [] };
    secondaryChartOptions: any = { data: [], series: [] };
    dateFiltersVisible = true;
    summary: RestaurantPosSalesSummaryDto | undefined;
    materialConsumption: RestaurantMaterialConsumptionReportDto[] = [];
    itemSales: RestaurantItemSalesReportDto[] = [];
    tableSales: RestaurantTableSalesReportDto[] = [];
    waiterSales: RestaurantWaiterSalesReportDto[] = [];
    dailySalesSummary: RestaurantDailySalesSummaryReportDto[] = [];
    kotBotStatus: RestaurantTicketStatusReportDto[] = [];
    itemSalesWithMargin: RestaurantItemMarginReportDto[] = [];
    waiterPerformance: RestaurantWaiterPerformanceReportDto[] = [];
    tableTurnover: RestaurantTableTurnoverReportDto[] = [];
    voidCancelledAudit: RestaurantVoidAuditReportDto[] = [];
    discountReport: RestaurantDiscountReportDto[] = [];
    settlementReport: RestaurantSettlementReportDto[] = [];
    recipeCosting: RestaurantRecipeCostingReportDto[] = [];
    foodCosting: RestaurantFoodCostingReportDto[] = [];
    wastageReport: RestaurantWastageReportDto[] = [];
    lowStockReport: RestaurantLowStockReportDto[] = [];
    payrollReport: RestaurantPayrollReportBundleDto = this.createEmptyPayrollReport();
    tables: RestaurantTableDto[] = [];
    categories: RestaurantMenuCategoryDto[] = [];
    waiters: GetUserDropdownDto[] = [];

    get canUseTableFilter(): boolean {
        return this.isGranted('Pages.Restaurant.Setup');
    }

    get canUseCategoryFilter(): boolean {
        return this.isGranted('Pages.Restaurant.Menu');
    }

    get visibleReportGridSections(): RestaurantAgGridReport[] {
        return this.reportGridSections.filter((section) => section.type === this.activeReportType);
    }

    get visibleReportTypes(): Array<{ key: RestaurantReportType; label: string; icon: string }> {
        return this.reportTypes.filter((type) => this.canViewReportType(type.key));
    }

    get reportKpis(): RestaurantReportKpi[] {
        const money = (value: number | null | undefined) =>
            Number(value || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        const sum = <T>(rows: T[], value: (row: T) => number) => rows.reduce((total, row) => total + value(row), 0);

        switch (this.activeReportType) {
            case 'operations':
                return [
                    { label: this.l('Open Tickets'), value: String(this.kotBotStatus.filter((row) => (row.status || '').toLowerCase() === 'pending').length) },
                    { label: this.l('Cancelled Tickets'), value: String(this.kotBotStatus.filter((row) => (row.status || '').toLowerCase() === 'cancelled').length) },
                    { label: this.l('Waiters'), value: String(this.waiterPerformance.length) },
                    { label: this.l('Tables'), value: String(this.tableTurnover.length) },
                ];
            case 'inventory':
                return [
                    { label: this.l('Low Stock Items'), value: String(this.lowStockReport.length) },
                    { label: this.l('Wastage'), value: money(sum(this.wastageReport, (row) => Number(row.amount || 0))) },
                    { label: this.l('Stock Consumption'), value: money(sum(this.materialConsumption, (row) => Number(row.amount || 0))) },
                    { label: this.l('Food Cost Items'), value: String(this.foodCosting.length) },
                ];
            case 'payroll':
                return [
                    { label: this.l('Active Employees'), value: String(this.payrollReport.summary.activeEmployeeCount || 0) },
                    { label: this.l('Payroll Runs'), value: String(this.payrollReport.summary.payrollRunCount || 0) },
                    { label: this.l('Gross Payroll'), value: money(this.payrollReport.summary.totalGross) },
                    { label: this.l('Net Payroll'), value: money(this.payrollReport.summary.totalNet) },
                ];
            case 'audit':
                return [
                    { label: this.l('Void / Cancelled'), value: String(this.voidCancelledAudit.length) },
                    { label: this.l('Discounts'), value: money(sum(this.discountReport, (row) => Number(row.totalDiscountAmount || 0))) },
                    { label: this.l('Payment Methods'), value: String(this.settlementReport.length) },
                    { label: this.l('Settled Sales'), value: money(sum(this.settlementReport, (row) => Number(row.grandTotal || 0))) },
                ];
            default:
                return [
                    { label: this.l('Orders'), value: String(this.summary?.orderCount || 0) },
                    { label: this.l('Sales'), value: money(this.summary?.grandTotal) },
                    { label: this.l('Average Bill'), value: money(this.summary?.averageBill) },
                    { label: this.l('Discounts'), value: money(this.summary?.discountAmount) },
                ];
        }
    }

    private restaurantReportsService = inject(RestaurantReportsServiceProxy);
    private restaurantReportsApiService = inject(RestaurantReportsApiService);
    private restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private restaurantMenuService = inject(RestaurantMenuServiceProxy);
    private reportingServiceProxy = inject(ReportingServiceProxy);
    private cdr = inject(ChangeDetectorRef);

    constructor() {
        super(inject(Injector));
        this.noRowsOverlayTemplate = `<span class="restaurant-grid-empty fw-bolder">${this.l('NoData')}</span>`;
        this.reportGridSections = this.createReportGridSections();
    }

    ngOnInit(): void {
        this.activeReportType = this.visibleReportTypes[0]?.key || 'sales';
        this.loadLookups();
        this.refresh();
    }

    ngAfterViewInit(): void {
        this.cdr.detectChanges();
    }

    loadLookups(): void {
        (this.canUseTableFilter ? this.restaurantSetupService.getTables(null) : of([])).subscribe((result) => {
            this.tables = result || [];
            this.cdr.markForCheck();
        });
        (this.canUseCategoryFilter ? this.restaurantMenuService.getCategories() : of([])).subscribe((result) => {
            this.categories = result || [];
            this.cdr.markForCheck();
        });
        this.reportingServiceProxy.getAllUserDropdown().subscribe((result) => {
            this.waiters = result || [];
            this.cdr.markForCheck();
        });
    }

    selectReportType(type: RestaurantReportType): void {
        if (!this.canViewReportType(type)) {
            return;
        }
        this.activeReportType = type;
        this.updateCharts();
        this.cdr.markForCheck();
    }

    refresh(): void {
        this.loading = true;
        this.cdr.markForCheck();
        const fromDate = this.toDateTime(this.filter.fromDate);
        const toDate = this.toDateTime(this.filter.toDate);
        const tableId = this.filter.tableId || null;
        const waiterUserId = this.filter.waiterUserId || null;
        const categoryId = this.filter.categoryId || null;
        const canViewSales = this.canViewReportType('sales');
        const canViewOperations = this.canViewReportType('operations');
        const canViewInventory = this.canViewReportType('inventory');
        const canViewPayroll = this.canViewReportType('payroll');
        const canViewAudit = this.canViewReportType('audit');

        forkJoin({
            summary: canViewSales ? this.restaurantReportsService.getPosSalesSummary(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of(new RestaurantPosSalesSummaryDto()),
            materialConsumption: canViewInventory ? this.restaurantReportsService.getMaterialConsumption(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            itemSales: canViewSales ? this.restaurantReportsService.getItemSales(fromDate, toDate, tableId, waiterUserId, categoryId) : of([]),
            tableSales: canViewOperations ? this.restaurantReportsService.getTableSales(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            waiterSales: canViewOperations ? this.restaurantReportsService.getWaiterSales(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            dailySalesSummary: canViewSales ? this.restaurantReportsApiService.getDailySalesSummary(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            kotBotStatus: canViewOperations ? this.restaurantReportsApiService.getKotBotStatus(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            itemSalesWithMargin: canViewSales ? this.restaurantReportsApiService.getItemSalesWithMargin(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            waiterPerformance: canViewOperations ? this.restaurantReportsApiService.getWaiterPerformance(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            tableTurnover: canViewOperations ? this.restaurantReportsApiService.getTableTurnover(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            voidCancelledAudit: canViewAudit ? this.restaurantReportsApiService.getVoidCancelledAudit(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            discountReport: canViewAudit ? this.restaurantReportsApiService.getDiscountReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            settlementReport: canViewAudit ? this.restaurantReportsApiService.getSettlementReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            recipeCosting: canViewInventory ? this.restaurantReportsApiService.getRecipeCosting(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            foodCosting: canViewInventory ? this.restaurantReportsApiService.getFoodCosting(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            wastageReport: canViewInventory ? this.restaurantReportsApiService.getWastageReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            lowStockReport: canViewInventory ? this.restaurantReportsApiService.getLowStockReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of([]),
            payrollReport: canViewPayroll ? this.restaurantReportsApiService.getPayrollReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ) : of(this.createEmptyPayrollReport()),
        })
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((result) => {
                this.summary = result.summary;
                this.materialConsumption = result.materialConsumption || [];
                this.itemSales = result.itemSales || [];
                this.tableSales = result.tableSales || [];
                this.waiterSales = result.waiterSales || [];
                this.dailySalesSummary = result.dailySalesSummary || [];
                this.kotBotStatus = result.kotBotStatus || [];
                this.itemSalesWithMargin = result.itemSalesWithMargin || [];
                this.waiterPerformance = result.waiterPerformance || [];
                this.tableTurnover = result.tableTurnover || [];
                this.voidCancelledAudit = result.voidCancelledAudit || [];
                this.discountReport = result.discountReport || [];
                this.settlementReport = result.settlementReport || [];
                this.recipeCosting = result.recipeCosting || [];
                this.foodCosting = result.foodCosting || [];
                this.wastageReport = result.wastageReport || [];
                this.lowStockReport = result.lowStockReport || [];
                this.payrollReport = result.payrollReport || this.createEmptyPayrollReport();
                this.updateCharts();
                this.cdr.markForCheck();
            });
    }

    clearFilters(): void {
        this.filter = this.createEmptyFilter();
        this.dateFiltersVisible = false;
        this.cdr.detectChanges();
        this.dateFiltersVisible = true;
        this.refresh();
    }

    setFilterDate(field: 'fromDate' | 'toDate', dateInAd: string): void {
        this.filter[field] = this.normalizeDateValue(dateInAd);
    }

    private createEmptyFilter(): RestaurantReportFilter {
        return { fromDate: '', toDate: '', tableId: null, waiterUserId: null, categoryId: null };
    }

    private toDateTime(value: string | null | undefined): DateTime | null {
        return value ? DateTime.fromISO(this.normalizeDateValue(value)) : null;
    }

    private normalizeDateValue(value: string | null | undefined): string {
        return value ? value.replace(/\//g, '-') : '';
    }

    canViewReportType(type: RestaurantReportType): boolean {
        const hasAnySpecificPermission = Object.values(this.reportPermissionByType).some((permission) =>
            this.isGranted(permission),
        );

        return !hasAnySpecificPermission || this.isGranted(this.reportPermissionByType[type]);
    }

    private finishLoading(): void {
        this.loading = false;
        this.cdr.markForCheck();
    }

    private updateCharts(): void {
        const top = <T>(rows: T[], amount: (row: T) => number) =>
            [...rows].sort((left, right) => amount(right) - amount(left)).slice(0, 10);

        switch (this.activeReportType) {
            case 'sales': {
                const daily = new Map<string, { label: string; sales: number; orders: number }>();
                for (const row of this.dailySalesSummary) {
                    const label = this.formatGridDate(row.date);
                    const value = daily.get(label) || { label, sales: 0, orders: 0 };
                    value.sales += Number(row.grandTotal || 0);
                    value.orders += Number(row.orderCount || 0);
                    daily.set(label, value);
                }
                this.primaryChartTitle = this.l('Daily Sales Trend');
                this.primaryChartOptions = this.chartOptions([...daily.values()], [
                    { type: 'line', xKey: 'label', yKey: 'sales', yName: this.l('Sales') },
                    { type: 'bar', xKey: 'label', yKey: 'orders', yName: this.l('Orders') },
                ]);
                this.secondaryChartTitle = this.l('Top Selling Items');
                this.secondaryChartOptions = this.chartOptions(
                    top(this.itemSales, (row) => Number(row.grandTotal || 0)).map((row) => ({
                        label: row.productName,
                        sales: Number(row.grandTotal || 0),
                    })),
                    [{ type: 'bar', xKey: 'label', yKey: 'sales', yName: this.l('Sales') }],
                );
                break;
            }
            case 'operations':
                this.primaryChartTitle = this.l('Waiter Performance');
                this.primaryChartOptions = this.chartOptions(
                    top(this.waiterPerformance, (row) => Number(row.grandTotal || 0)).map((row) => ({
                        label: row.waiterName,
                        sales: Number(row.grandTotal || 0),
                        orders: Number(row.orderCount || 0),
                    })),
                    [
                        { type: 'bar', xKey: 'label', yKey: 'sales', yName: this.l('Sales') },
                        { type: 'bar', xKey: 'label', yKey: 'orders', yName: this.l('Orders') },
                    ],
                );
                this.secondaryChartTitle = this.l('Table Turnover');
                this.secondaryChartOptions = this.chartOptions(
                    top(this.tableTurnover, (row) => Number(row.grandTotal || 0)).map((row) => ({
                        label: row.tableName,
                        sales: Number(row.grandTotal || 0),
                        minutes: Number(row.averageMinutes || 0),
                    })),
                    [
                        { type: 'bar', xKey: 'label', yKey: 'sales', yName: this.l('Sales') },
                        { type: 'line', xKey: 'label', yKey: 'minutes', yName: this.l('Average Minutes') },
                    ],
                );
                break;
            case 'inventory':
                this.primaryChartTitle = this.l('Food Cost by Item');
                this.primaryChartOptions = this.chartOptions(
                    top(this.foodCosting, (row) => Number(row.salesAmount || 0)).map((row) => ({
                        label: row.productName,
                        sales: Number(row.salesAmount || 0),
                        cost: Number(row.actualCostAmount || row.theoreticalCostAmount || 0),
                    })),
                    [
                        { type: 'bar', xKey: 'label', yKey: 'sales', yName: this.l('Sales') },
                        { type: 'bar', xKey: 'label', yKey: 'cost', yName: this.l('Cost') },
                    ],
                );
                this.secondaryChartTitle = this.l('Low Stock Exposure');
                this.secondaryChartOptions = this.chartOptions(
                    top(this.lowStockReport, (row) => Number(row.suggestedQty || 0)).map((row) => ({
                        label: row.productName,
                        available: Number(row.availableQty || 0),
                        minimum: Number(row.minimumStock || 0),
                    })),
                    [
                        { type: 'bar', xKey: 'label', yKey: 'available', yName: this.l('Available') },
                        { type: 'line', xKey: 'label', yKey: 'minimum', yName: this.l('Minimum') },
                    ],
                );
                break;
            case 'payroll':
                this.primaryChartTitle = this.l('Payroll Run Cost');
                this.primaryChartOptions = this.chartOptions(
                    [...this.payrollReport.runs].reverse().map((row) => ({
                        label: row.runNumber,
                        gross: Number(row.totalGross || 0),
                        net: Number(row.totalNet || 0),
                        deductions: Number(row.totalDeduction || 0),
                    })),
                    [
                        { type: 'bar', xKey: 'label', yKey: 'gross', yName: this.l('Gross') },
                        { type: 'bar', xKey: 'label', yKey: 'net', yName: this.l('Net') },
                        { type: 'bar', xKey: 'label', yKey: 'deductions', yName: this.l('Deductions') },
                    ],
                );
                this.secondaryChartTitle = this.l('Employee Payroll Cost');
                this.secondaryChartOptions = this.chartOptions(
                    top(this.payrollReport.employeeCosts, (row) => Number(row.grossPay || 0)).map((row) => ({
                        label: row.employeeName,
                        gross: Number(row.grossPay || 0),
                        net: Number(row.netPay || 0),
                    })),
                    [
                        { type: 'bar', xKey: 'label', yKey: 'gross', yName: this.l('Gross') },
                        { type: 'bar', xKey: 'label', yKey: 'net', yName: this.l('Net') },
                    ],
                );
                break;
            case 'audit':
                this.primaryChartTitle = this.l('Settlement Mix');
                this.primaryChartOptions = this.chartOptions(
                    this.settlementReport.map((row) => ({
                        label: row.paymentMethodName,
                        amount: Number(row.grandTotal || 0),
                    })),
                    [{ type: 'pie', angleKey: 'amount', legendItemKey: 'label' }],
                );
                this.secondaryChartTitle = this.l('Largest Discounts');
                this.secondaryChartOptions = this.chartOptions(
                    top(this.discountReport, (row) => Number(row.totalDiscountAmount || 0)).map((row) => ({
                        label: row.orderNo,
                        discount: Number(row.totalDiscountAmount || 0),
                    })),
                    [{ type: 'bar', xKey: 'label', yKey: 'discount', yName: this.l('Discount') }],
                );
                break;
        }
    }

    private chartOptions(data: object[], series: object[]): any {
        return {
            data,
            series,
            background: { fill: 'transparent' },
            legend: { enabled: true, position: 'bottom' },
            padding: { top: 12, right: 12, bottom: 12, left: 12 },
        };
    }

    private createReportGridSections(): RestaurantAgGridReport[] {
        return [
            {
                type: 'inventory',
                title: this.l('Material Consumption'),
                rowData: () => this.materialConsumption,
                height: 300,
                columnDefs: [
                    this.textColumn('Material', 'productName', 210),
                    this.numberColumn('Qty', 'qty', {}, 2, 3),
                    this.textColumn('Unit', 'unitName', 110),
                    this.numberColumn('Amount', 'amount'),
                ],
            },
            {
                type: 'sales',
                title: this.l('Item Sales'),
                rowData: () => this.itemSales,
                height: 300,
                columnDefs: [
                    this.textColumn('Item', 'productName', 210),
                    this.textColumn('Category', 'categoryName', 160),
                    this.numberColumn('Qty', 'qty'),
                    this.numberColumn('Sales', 'grandTotal'),
                ],
            },
            {
                type: 'operations',
                title: this.l('Table Sales'),
                rowData: () => this.tableSales,
                height: 260,
                columnDefs: [
                    this.textColumn('Table', 'tableName', 180),
                    this.integerColumn('Orders', 'orderCount'),
                    this.numberColumn('Sales', 'grandTotal'),
                ],
            },
            {
                type: 'operations',
                title: this.l('Waiter Sales'),
                rowData: () => this.waiterSales,
                height: 260,
                columnDefs: [
                    this.textColumn('Waiter', 'waiterName', 180),
                    this.integerColumn('Orders', 'orderCount'),
                    this.numberColumn('Sales', 'grandTotal'),
                ],
            },
            {
                type: 'sales',
                title: this.l('Daily Sales Summary'),
                rowData: () => this.dailySalesSummary,
                height: 360,
                columnDefs: [
                    this.dateColumn('Date', 'date'),
                    this.textColumn('Outlet', 'outletName', 170),
                    this.textColumn('Table', 'tableName', 130),
                    this.textColumn('User', 'userName', 150),
                    this.integerColumn('Orders', 'orderCount'),
                    this.numberColumn('Gross', 'grossAmount'),
                    this.numberColumn('Discount', 'discountAmount'),
                    this.numberColumn('Sales', 'grandTotal'),
                    this.numberColumn('Average Bill', 'averageBill'),
                ],
            },
            {
                type: 'operations',
                title: this.l('KOT/BOT Pending and Cancelled'),
                rowData: () => this.kotBotStatus,
                height: 380,
                columnDefs: [
                    this.textColumn('Ticket', 'ticketNo', 130),
                    this.textColumn('Order', 'orderNo', 130),
                    this.textColumn('Type', 'ticketType', 120),
                    this.textColumn('Status', 'status', 120),
                    this.textColumn('Station', 'stationName', 150),
                    this.textColumn('Table', 'tableName', 130),
                    this.textColumn('Waiter', 'waiterName', 150),
                    this.dateColumn('Sent At', 'sentAt', true),
                    this.integerColumn('Items', 'itemCount'),
                    this.numberColumn('Qty', 'qty'),
                    this.textColumn('Reason', 'cancelReason', 220),
                ],
            },
            {
                type: 'sales',
                title: this.l('Item Sales With Margin'),
                rowData: () => this.itemSalesWithMargin,
                height: 360,
                columnDefs: [
                    this.textColumn('Item', 'productName', 210),
                    this.textColumn('Category', 'categoryName', 160),
                    this.numberColumn('Qty', 'qty'),
                    this.numberColumn('Gross', 'grossAmount'),
                    this.numberColumn('Discount', 'discountAmount'),
                    this.numberColumn('Sales', 'grandTotal'),
                    this.numberColumn('Cost', 'costAmount'),
                    this.numberColumn('Margin', 'marginAmount'),
                    this.numberColumn('Margin %', 'marginPercent'),
                ],
            },
            {
                type: 'operations',
                title: this.l('Waiter Performance'),
                rowData: () => this.waiterPerformance,
                height: 340,
                columnDefs: [
                    this.textColumn('Waiter', 'waiterName', 180),
                    this.integerColumn('Orders', 'orderCount'),
                    this.integerColumn('Items', 'itemCount'),
                    this.integerColumn('Guests', 'guestCount'),
                    this.numberColumn('Discount', 'discountAmount'),
                    this.numberColumn('Sales', 'grandTotal'),
                    this.numberColumn('Average Bill', 'averageBill'),
                ],
            },
            {
                type: 'operations',
                title: this.l('Table Turnover'),
                rowData: () => this.tableTurnover,
                height: 340,
                columnDefs: [
                    this.textColumn('Table', 'tableName', 180),
                    this.integerColumn('Sessions', 'sessionCount'),
                    this.integerColumn('Orders', 'orderCount'),
                    this.integerColumn('Guests', 'guestCount'),
                    this.numberColumn('Avg Minutes', 'averageMinutes', {}, 0, 0),
                    this.numberColumn('Sales', 'grandTotal'),
                    this.numberColumn('Average Bill', 'averageBill'),
                ],
            },
            {
                type: 'audit',
                title: this.l('Void/Cancelled Bill Audit'),
                rowData: () => this.voidCancelledAudit,
                height: 380,
                columnDefs: [
                    this.dateColumn('Date', 'date', true),
                    this.textColumn('Order', 'orderNo', 130),
                    this.textColumn('Ticket', 'ticketNo', 130),
                    this.textColumn('Table', 'tableName', 130),
                    this.textColumn('Waiter', 'waiterName', 150),
                    this.textColumn('Item', 'itemName', 180),
                    this.numberColumn('Qty', 'qty'),
                    this.numberColumn('Amount', 'amount'),
                    this.textColumn('Status', 'status', 120),
                    this.textColumn('Reason', 'reason', 220),
                ],
            },
            {
                type: 'audit',
                title: this.l('Discount Report'),
                rowData: () => this.discountReport,
                height: 380,
                columnDefs: [
                    this.dateColumn('Date', 'date'),
                    this.textColumn('Order', 'orderNo', 130),
                    this.textColumn('User', 'userName', 150),
                    this.textColumn('Table', 'tableName', 130),
                    this.textColumn('Customer', 'customerName', 160),
                    this.numberColumn('Gross', 'grossAmount'),
                    this.numberColumn('Order Discount', 'orderDiscountAmount'),
                    this.numberColumn('Item Discount', 'itemDiscountAmount'),
                    this.numberColumn('Total Discount', 'totalDiscountAmount'),
                    this.textColumn('Reason', 'reason', 220),
                ],
            },
            {
                type: 'audit',
                title: this.l('Settlement Report'),
                rowData: () => this.settlementReport,
                height: 320,
                columnDefs: [
                    this.textColumn('Payment Method', 'paymentMethodName', 190),
                    this.integerColumn('Orders', 'orderCount'),
                    this.numberColumn('Gross', 'grossAmount'),
                    this.numberColumn('Discount', 'discountAmount'),
                    this.numberColumn('Tax', 'taxAmount'),
                    this.numberColumn('Net', 'netAmount'),
                    this.numberColumn('Sales', 'grandTotal'),
                ],
            },
            {
                type: 'inventory',
                title: this.l('Recipe Costing'),
                rowData: () => this.recipeCosting,
                height: 380,
                columnDefs: [
                    this.textColumn('Menu Item', 'productName', 210),
                    this.textColumn('Category', 'categoryName', 160),
                    {
                        headerName: this.l('Raw Materials'),
                        minWidth: 320,
                        flex: 1.4,
                        autoHeight: true,
                        wrapText: true,
                        valueGetter: (params) => this.formatRecipeLines(params),
                    },
                    this.numberColumn('Menu Price', 'menuPrice'),
                    this.numberColumn('Recipe Cost', 'recipeCost'),
                    this.numberColumn('Food Cost %', 'foodCostPercent'),
                    this.numberColumn('Margin', 'marginAmount'),
                ],
            },
            {
                type: 'inventory',
                title: this.l('Food Costing'),
                rowData: () => this.foodCosting,
                height: 360,
                columnDefs: [
                    this.textColumn('Item', 'productName', 210),
                    this.textColumn('Category', 'categoryName', 160),
                    this.numberColumn('Sold Qty', 'soldQty'),
                    this.numberColumn('Sales', 'salesAmount'),
                    this.numberColumn('Recipe Cost', 'theoreticalCostAmount'),
                    this.numberColumn('Actual Issue', 'actualCostAmount'),
                    this.numberColumn('Wastage', 'wastageCostAmount'),
                    this.numberColumn('Gross Margin', 'marginAmount'),
                    this.numberColumn('Food Cost %', 'foodCostPercent'),
                ],
            },
            {
                type: 'inventory',
                title: this.l('Wastage Report'),
                rowData: () => this.wastageReport,
                height: 340,
                columnDefs: [
                    this.dateColumn('Date', 'date'),
                    this.textColumn('Raw Material', 'productName', 210),
                    this.numberColumn('Qty', 'qty', {}, 2, 3),
                    this.textColumn('Unit', 'unitName', 110),
                    this.numberColumn('Rate', 'rate'),
                    this.numberColumn('Amount', 'amount'),
                    this.textColumn('Reason', 'reason', 180),
                    this.textColumn('User', 'userName', 150),
                ],
            },
            {
                type: 'inventory',
                title: this.l('Low Stock Report'),
                rowData: () => this.lowStockReport,
                height: 340,
                columnDefs: [
                    this.textColumn('Raw Material', 'productName', 210),
                    this.numberColumn('Available', 'availableQty', {}, 2, 3),
                    this.textColumn('Unit', 'unitName', 110),
                    this.numberColumn('Minimum', 'minimumStock', {}, 2, 3),
                    this.numberColumn('Maximum', 'maximumStock', {}, 2, 3),
                    this.numberColumn('Pending PO', 'pendingPurchaseQty', {}, 2, 3),
                    this.textColumn('Supplier', 'supplierName', 180),
                    this.numberColumn('Suggested Qty', 'suggestedQty', {}, 2, 3),
                    {
                        headerName: this.l('Mapping'),
                        field: 'missingSupplierMapping',
                        minWidth: 130,
                        valueFormatter: (params) => (params.value ? this.l('Mapping Missing') : ''),
                    },
                ],
            },
            {
                type: 'payroll',
                title: this.l('Payroll Runs'),
                rowData: () => this.payrollReport.runs,
                height: 340,
                columnDefs: [
                    this.textColumn('Run', 'runNumber', 150),
                    this.dateColumn('Period Start', 'periodStart'),
                    this.dateColumn('Period End', 'periodEnd'),
                    {
                        headerName: this.l('Status'),
                        field: 'status',
                        minWidth: 120,
                        valueFormatter: (params) => ['Draft', 'Approved', 'Paid'][Number(params.value)] || '-',
                    },
                    this.integerColumn('Employees', 'employeeCount'),
                    this.numberColumn('Gross', 'totalGross'),
                    this.numberColumn('Deductions', 'totalDeduction'),
                    this.numberColumn('Net', 'totalNet'),
                ],
            },
            {
                type: 'payroll',
                title: this.l('Employee Payroll Cost'),
                rowData: () => this.payrollReport.employeeCosts,
                height: 380,
                columnDefs: [
                    this.textColumn('Employee', 'employeeName', 190),
                    this.textColumn('Staff Code', 'staffCode', 130),
                    this.textColumn('Job Role', 'jobRole', 160),
                    this.numberColumn('Worked Hours', 'workedHours'),
                    this.numberColumn('Overtime', 'overtimeHours'),
                    this.numberColumn('Basic Pay', 'basicPay'),
                    this.numberColumn('Allowance', 'allowance'),
                    this.numberColumn('Tips & Service', 'tipsAndServiceCharge'),
                    this.numberColumn('Gross', 'grossPay'),
                    this.numberColumn('Deductions', 'totalDeduction'),
                    this.numberColumn('Net', 'netPay'),
                ],
            },
            {
                type: 'payroll',
                title: this.l('Attendance Summary'),
                rowData: () => this.payrollReport.attendance,
                height: 300,
                columnDefs: [
                    this.textColumn('Status', 'statusName', 170),
                    this.integerColumn('Records', 'recordCount'),
                    this.numberColumn('Regular Hours', 'regularHours'),
                    this.numberColumn('Overtime Hours', 'overtimeHours'),
                ],
            },
        ];
    }

    private createEmptyPayrollReport(): RestaurantPayrollReportBundleDto {
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

    private textColumn(headerName: string, field: string, minWidth = 140): ColDef {
        return {
            headerName: this.l(headerName),
            field,
            minWidth,
            valueFormatter: (params) => this.formatDash(params.value),
        };
    }

    private integerColumn(headerName: string, field: string, options: Partial<ColDef> = {}): ColDef {
        return this.numberColumn(headerName, field, options, 0, 0);
    }

    private numberColumn(
        headerName: string,
        field: string,
        options: Partial<ColDef> = {},
        minimumFractionDigits = 2,
        maximumFractionDigits = 2,
    ): ColDef {
        return {
            headerName: this.l(headerName),
            field,
            minWidth: 120,
            type: 'rightAligned',
            valueFormatter: (params) =>
                this.formatGridNumber(params.value, minimumFractionDigits, maximumFractionDigits),
            ...options,
        };
    }

    private dateColumn(headerName: string, field: string, includeTime = false): ColDef {
        return {
            headerName: this.l(headerName),
            field,
            minWidth: includeTime ? 170 : 130,
            valueFormatter: (params) => this.formatGridDate(params.value, includeTime),
        };
    }

    private formatRecipeLines(params: ValueGetterParams): string {
        const lines = params.data?.lines || [];

        if (!lines.length) {
            return '-';
        }

        return lines
            .map(
                (line) =>
                    `${line.rawMaterialName}: ${this.formatGridNumber(line.quantity, 2, 3)} ${line.unitName || ''} / ${this.formatGridNumber(line.costRate)} / ${this.formatGridNumber(line.wastagePercentage, 0, 2)}%`,
            )
            .join('\n');
    }

    private formatDash(value: unknown): string {
        return value === null || value === undefined || value === '' ? '-' : String(value);
    }

    private formatGridDate(value: unknown, includeTime = false): string {
        if (!value) {
            return '-';
        }

        const dateValue = value instanceof Date ? DateTime.fromJSDate(value) : DateTime.fromISO(String(value));

        return dateValue.isValid ? dateValue.toFormat(includeTime ? 'yyyy-MM-dd HH:mm' : 'yyyy-MM-dd') : String(value);
    }

    private formatGridNumber(
        value: ValueFormatterParams['value'],
        minimumFractionDigits = 2,
        maximumFractionDigits = 2,
    ): string {
        if (value === null || value === undefined || value === '') {
            return '';
        }

        const numberValue = Number(value);

        if (Number.isNaN(numberValue)) {
            return String(value);
        }

        return numberValue.toLocaleString(undefined, {
            minimumFractionDigits,
            maximumFractionDigits,
        });
    }
}
