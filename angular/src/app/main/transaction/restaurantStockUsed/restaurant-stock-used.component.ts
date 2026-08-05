import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { RestaurantInventoryServiceProxy, RestaurantStockAdjustmentDto } from '@shared/service-proxies/service-proxies';
import { ColDef, GridApi } from 'ag-grid-community';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs';

interface RestaurantStockUsedRow extends RestaurantStockAdjustmentDto {
    lineCount: number;
}

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    templateUrl: './restaurant-stock-used.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
})
export class RestaurantStockUsedComponent extends AppComponentBase implements OnInit, OnDestroy {
    filterText = '';
    form: FormGroup;
    advancedFiltersAreShown = false;
    loading = false;
    selectedRows: RestaurantStockUsedRow[] = [];
    rowData: RestaurantStockUsedRow[] = [];

    private readonly stockUsedAdjustmentType = 1;
    private allRows: RestaurantStockUsedRow[] = [];
    private gridApi!: GridApi;

    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private inventoryService: RestaurantInventoryServiceProxy,
        private router: Router,
    ) {
        super(injector);
        this.getSetting();
        this.createForm();
    }

    ngOnInit(): void {
        this.createForm();
        this.setFinancialYear();
    }

    override ngOnDestroy(): void {
        super.ngOnDestroy();
    }

    createForm(): void {
        this.form = this.fb.group({
            fromDate: [''],
            toDate: [''],
        });
    }

    setFinancialYear(): void {
        this.reportingService.getFinancialYears().subscribe({
            next: (result) => {
                this.form.patchValue({
                    fromDate: result.fromDate ? this.toInputDate(result.fromDate) : '',
                    toDate: result.toDate ? this.toInputDate(result.toDate) : '',
                });
                this.loadPage(this.currentPage);
                this.markViewForCheck();
            },
            error: () => {
                this.loadPage(this.currentPage);
            },
        });
    }

    public columnDefs: ColDef[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => params.node.rowIndex + 1,
            width: 80,
        },
        {
            field: 'dateMiti',
            headerName: this.l('Date Miti'),
            sortable: true,
            filter: true,
            flex: 2,
        },
        {
            field: 'voucherNo',
            headerName: this.l('Voucher No'),
            sortable: true,
            filter: true,
            flex: 2,
        },
        {
            field: 'description',
            headerName: this.l('Description'),
            sortable: true,
            filter: true,
            flex: 3,
        },
        {
            field: 'createUserName',
            headerName: this.l('Created By'),
            sortable: true,
            filter: true,
            flex: 2,
        },
        {
            field: 'lineCount',
            headerName: this.l('Lines'),
            sortable: true,
            filter: true,
            flex: 1,
        },
        {
            field: 'totalAmount',
            headerName: this.l('Total Amount'),
            sortable: true,
            filter: true,
            flex: 2,
            valueFormatter: (params) => this.numberWithCommas(Number(params.value || 0).toFixed(2)),
        },
    ];

    defaultColDef = {
        resizable: true,
        minWidth: 120,
        maxWidth: 800,
    };

    onGridReady(params): void {
        this.gridApi = params.api;
        this.refreshVisibleRows();
    }

    loadPage(page: number): void {
        this.currentPage = page || 0;
        this.loading = true;
        this.inventoryService
            .getStockAdjustments(
                this.toDateTime(this.form.get('fromDate').value),
                this.toDateTime(this.form.get('toDate').value),
                undefined,
                undefined,
                undefined,
            )
            .pipe(finalize(() => {
                this.loading = false;
                this.markViewForCheck();
            }))
            .subscribe((data) => {
                this.allRows = (data || [])
                    .filter((item) => item.adjustmentType === this.stockUsedAdjustmentType)
                    .map((item) => Object.assign(item, { lineCount: item.lines?.length || 0 }));
                this.refreshVisibleRows();
            });
    }

    onPageChange(page: number): void {
        this.currentPage = page;
        this.refreshVisibleRows();
    }

    onPageSizeChange(newPageSize: number): void {
        this.pageSize = +newPageSize;
        this.currentPage = 0;
        this.refreshVisibleRows();
    }

    onSelectionChanged(): void {
        this.selectedRows = this.gridApi?.getSelectedRows() || [];
    }

    addRoute(): void {
        if (this.isGranted('Pages.Restaurant.Inventory.StockAdjustment')) {
            this.router.navigate(['app/main/transaction/restaurantStockUsed/add']);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }

    exportSelectedRows(): void {
        this.gridApi?.exportDataAsExcel({
            onlySelected: this.selectedRows.length > 0,
            fileName: 'RestaurantStockUsed.xlsx',
        });
    }

    searchValueOnApiCall(value: string): void {
        this.filterText = value || '';
        this.currentPage = 0;
        this.refreshVisibleRows();
    }

    refresh(): void {
        this.currentPage = 0;
        this.loadPage(this.currentPage);
    }

    getCustomContextMenuItems = (params: any) => {
        return [
            {
                name: 'Export to Excel',
                action: () => params.api.exportDataAsExcel(),
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            },
        ];
    };

    numberWithCommas(value: string): string {
        return value.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    private refreshVisibleRows(): void {
        const filteredRows = this.getFilteredRows();
        this.totalRecords = filteredRows.length;
        this.currentPage = this.getValidPageStart(this.currentPage, filteredRows.length);
        this.rowData = this.setGridRowData(
            this.gridApi,
            filteredRows.slice(this.currentPage, this.currentPage + this.pageSize),
        );
        this.calculatePageSizeOptions();
        this.markViewForCheck();
    }

    private getFilteredRows(): RestaurantStockUsedRow[] {
        const search = (this.filterText || '').trim().toLowerCase();
        if (!search) {
            return this.allRows;
        }

        return this.allRows.filter((row) => {
            const values = [
                row.voucherNo,
                row.dateMiti,
                row.description,
                row.createUserName,
                row.totalAmount?.toString(),
                row.lineCount?.toString(),
            ];
            return values.some((value) => (value || '').toLowerCase().includes(search));
        });
    }

    private getValidPageStart(page: number, totalRecords: number): number {
        if (totalRecords <= 0 || page <= 0) {
            return 0;
        }

        const lastPageStart = Math.floor((totalRecords - 1) / this.pageSize) * this.pageSize;
        return Math.min(page, lastPageStart);
    }

    private toInputDate(value: any): string {
        if (!value) {
            return '';
        }

        if (typeof value === 'string') {
            return value.substring(0, 10);
        }

        if (value.toISODate) {
            return value.toISODate();
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '' : date.toISOString().substring(0, 10);
    }

    private toDateTime(value: string | undefined): DateTime | undefined {
        return value ? DateTime.fromISO(value) : undefined;
    }
}
