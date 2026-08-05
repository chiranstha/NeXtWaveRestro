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
import { finalize, forkJoin } from 'rxjs';
import {
    RestaurantDailySalesSummaryReportDto,
    RestaurantDiscountReportDto,
    RestaurantItemMarginReportDto,
    RestaurantFoodCostingReportDto,
    RestaurantLowStockReportDto,
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
    title: string;
    columnDefs: ColDef[];
    rowData: () => object[];
    height: number;
}

@Component({
    selector: 'restaurant-reports',
    templateUrl: './restaurant-reports.component.html',
    styleUrls: ['../restaurant-shared.css'],
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
    tables: RestaurantTableDto[] = [];
    categories: RestaurantMenuCategoryDto[] = [];
    waiters: GetUserDropdownDto[] = [];

    private restaurantReportsService = inject(RestaurantReportsServiceProxy);
    private restaurantReportsApiService = inject(RestaurantReportsApiService);
    private restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private restaurantMenuService = inject(RestaurantMenuServiceProxy);
    private reportingServiceProxy = inject(ReportingServiceProxy);
    private cdr = inject(ChangeDetectorRef);

    constructor() {
        super(inject(Injector));
        this.noRowsOverlayTemplate = `<span class="restaurant-grid-empty">${this.l('NoData')}</span>`;
        this.reportGridSections = this.createReportGridSections();
    }

    ngOnInit(): void {
        this.loadLookups();
        this.refresh();
    }

    ngAfterViewInit(): void {
        this.cdr.detectChanges();
    }

    loadLookups(): void {
        this.restaurantSetupService.getTables(null).subscribe((result) => {
            this.tables = result || [];
            this.cdr.markForCheck();
        });
        this.restaurantMenuService.getCategories().subscribe((result) => {
            this.categories = result || [];
            this.cdr.markForCheck();
        });
        this.reportingServiceProxy.getAllUserDropdown().subscribe((result) => {
            this.waiters = result || [];
            this.cdr.markForCheck();
        });
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
            summary: this.restaurantReportsService.getPosSalesSummary(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            materialConsumption: this.restaurantReportsService.getMaterialConsumption(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            itemSales: this.restaurantReportsService.getItemSales(fromDate, toDate, tableId, waiterUserId, categoryId),
            tableSales: this.restaurantReportsService.getTableSales(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            waiterSales: this.restaurantReportsService.getWaiterSales(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            dailySalesSummary: this.restaurantReportsApiService.getDailySalesSummary(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            kotBotStatus: this.restaurantReportsApiService.getKotBotStatus(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            itemSalesWithMargin: this.restaurantReportsApiService.getItemSalesWithMargin(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            waiterPerformance: this.restaurantReportsApiService.getWaiterPerformance(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            tableTurnover: this.restaurantReportsApiService.getTableTurnover(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            voidCancelledAudit: this.restaurantReportsApiService.getVoidCancelledAudit(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            discountReport: this.restaurantReportsApiService.getDiscountReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            settlementReport: this.restaurantReportsApiService.getSettlementReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            recipeCosting: this.restaurantReportsApiService.getRecipeCosting(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            foodCosting: this.restaurantReportsApiService.getFoodCosting(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            wastageReport: this.restaurantReportsApiService.getWastageReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
            lowStockReport: this.restaurantReportsApiService.getLowStockReport(
                fromDate,
                toDate,
                tableId,
                waiterUserId,
                categoryId,
            ),
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

    private finishLoading(): void {
        this.loading = false;
        this.cdr.markForCheck();
    }

    private createReportGridSections(): RestaurantAgGridReport[] {
        return [
            {
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
                    this.numberColumn('Recipe Cost', 'totalRecipeCost'),
                    this.numberColumn('Food Cost %', 'foodCostPercent'),
                    this.numberColumn('Margin', 'marginAmount'),
                ],
            },
            {
                title: this.l('Food Costing'),
                rowData: () => this.foodCosting,
                height: 360,
                columnDefs: [
                    this.textColumn('Item', 'productName', 210),
                    this.textColumn('Category', 'categoryName', 160),
                    this.numberColumn('Sold Qty', 'soldQty'),
                    this.numberColumn('Sales', 'salesAmount'),
                    this.numberColumn('Recipe Cost', 'theoreticalRecipeCost'),
                    this.numberColumn('Actual Issue', 'actualStockIssueCost'),
                    this.numberColumn('Wastage', 'wastageCost'),
                    this.numberColumn('Gross Margin', 'grossMargin'),
                    this.numberColumn('Food Cost %', 'foodCostPercent'),
                ],
            },
            {
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
        ];
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
                    `${line.rawMaterialName}: ${this.formatGridNumber(line.qty, 2, 3)} ${line.unitName || ''} / ${this.formatGridNumber(line.unitCost)} / ${this.formatGridNumber(line.wastagePercentage, 0, 2)}%`,
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
