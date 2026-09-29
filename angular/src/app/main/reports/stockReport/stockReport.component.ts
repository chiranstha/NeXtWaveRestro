import { Component, Injector, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { GridApi, RowSelectionOptions, ColDef, GridOptions, ValueFormatterParams } from 'ag-grid-community';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    StockCalculationDto,
    StockReportServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { finalize, forkJoin, Subject, takeUntil, debounceTime } from 'rxjs';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    standalone: false,
    selector: 'appstockreport',
    templateUrl: './stockReport.component.html',
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class StockReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    // UI state
    advancedFiltersAreShown = false;
    isPdfShow = false;
    loading = false;
    pdfUrl = '';
    filterText = '';
    title = 'Stock Report';

    // Form
    form: FormGroup;

    // Data
    tableRows: StockCalculationDto[] = [];
    pinnedBottomRowData: any[] = [];

    allProductGroups: UniversalDropdownDto[] = [];
    allAccountLedgers: UniversalDropdownDto[] = [];

    // AG Grid Configuration
    private gridApi: GridApi;
    public autoGroupColumnDef: ColDef = {
        minWidth: 200,
        cellClass: params => params.node.group ? 'ag-group-cell' : ''
    };
    public rowSelection: RowSelectionOptions | 'single' | 'multiple' = { mode: 'multiRow' };

    // Formatting function for numeric values
    private currencyFormatter(params: ValueFormatterParams): string {
        const {value} = params;
        if (value === null || value === undefined || value === '') {return '';}
        return value.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    // Column definitions with improved formatting
    public columnDefs: ColDef[] = [
        {
            headerName: 'Product Name',
            field: 'productName',
            minWidth: 200,
            cellRenderer: (params) => {
                const productName = document.createElement('span');
                if (params.node?.rowPinned === 'bottom') {
                    productName.textContent = 'Grand Total';
                    productName.className = 'stock-report-grand-total';
                    return productName;
                }
                productName.textContent = params.value ?? '';
                productName.className = 'cursor-pointer';
                return productName;
            },
            sortable: true,
            filter: true,
            enableRowGroup: true
        },
        {
            headerName: 'Rate',
            field: 'rate',
            minWidth: 100,
            valueFormatter: this.currencyFormatter,
            cellStyle: { 'text-align': 'right' }
        },
        {
            headerName: 'O. Qty',
            field: 'openingStockQtyString',
            minWidth: 100,
            wrapText: true,
            autoHeight: true
        },
        {
            headerName: 'O. Amt',
            field: 'openingStockValue',
            minWidth: 100,
            valueFormatter: this.currencyFormatter,
            aggFunc: 'sum',
            cellStyle: { 'text-align': 'right' }
        },
        {
            headerName: 'I. Qty',
            field: 'inWardQtyString',
            minWidth: 100,
            wrapText: true,
            autoHeight: true
        },
        {
            headerName: 'I. Amt',
            field: 'inWardValue',
            minWidth: 100,
            valueFormatter: this.currencyFormatter,
            aggFunc: 'sum',
            cellStyle: { 'text-align': 'right' }
        },
        {
            headerName: 'O. Qty',
            field: 'outWardQtyString',
            minWidth: 100,
            wrapText: true,
            autoHeight: true
        },
        {
            headerName: 'O. Amt',
            field: 'outWardValue',
            minWidth: 100,
            valueFormatter: this.currencyFormatter,
            aggFunc: 'sum',
            cellStyle: { 'text-align': 'right' }
        },
        {
            headerName: 'Closing\nQty',
            field: 'closingQtyString',
            minWidth: 100,
            wrapText: true,
            autoHeight: true
        },
        {
            headerName: 'Closing\nAmount',
            field: 'closingValue',
            minWidth: 120,
            valueFormatter: this.currencyFormatter,
            aggFunc: 'sum',
            cellStyle: { 'text-align': 'right' }
        },
    ];

    // Grid options
    public gridOptions: GridOptions = {
        defaultColDef: {
            resizable: true,
            minWidth: 100,
            maxWidth: 300,
            sortable: true,
            filter: true,
        },
        headerHeight: 44,
        rowHeight: 40,
        animateRows: true,
        pagination: false,
        pinnedBottomRowData: [],
        suppressHorizontalScroll: false,
        getContextMenuItems: this.getCustomContextMenuItems,
        getRowStyle: (params) => {
            if (params.node.rowPinned === 'bottom') {
                return {
                    'font-weight': 'bold',
                    'background-color': '#f5f5f5',
                    'border-top': '2px solid #711905',
                    'color': '#711905',
                    'font-size': '13px'
                };
            }
            return null;
        },

    };

    // Component cleanup
    private destroy$ = new Subject<void>();
    private searchSubject = new Subject<string>();

    constructor(
        injector: Injector,
        private _router: Router,
        private _fb: FormBuilder,
        // private _allProxy: ReportingServiceProxy,
        private _proxy: StockReportServiceProxy,
        private _fileDownloadService: FileDownloadService,
        private _cdr: ChangeDetectorRef
    ) {
        super(injector);
        this.createForm();
        this.initSearchDebounce();
    }

    ngOnInit(): void {
        this.initializeForm();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
        this.searchSubject.complete();
    }

    // Initialize search with debounce
    private initSearchDebounce(): void {
        this.searchSubject.pipe(
            debounceTime(300),
            takeUntil(this.destroy$)
        ).subscribe(value => {
            if (this.gridApi) {
                this.gridApi.setGridOption('quickFilterText', value);
                this.calculateTotals();
                this._cdr.markForCheck();
            }
        });
    }

    // Form initialization
    private initializeForm(): void {
        const savedFormValue = this.getSavedFormValue();

        if (savedFormValue) {
            this.form.patchValue(savedFormValue, { emitEvent: false });
        }

        this.loadInitialData(savedFormValue);
    }

    private getSavedFormValue(): any | null {
        const savedFormValue = localStorage.getItem(AppConsts.suktasStorage.stockReportForm);
        if (!savedFormValue) {
            return null;
        }

        try {
            return JSON.parse(savedFormValue);
        } catch {
            localStorage.removeItem(AppConsts.suktasStorage.stockReportForm);
            return null;
        }
    }

    private loadInitialData(savedFormValue: any = null): void {

        forkJoin({
            financialData: this._proxy.getFinancialYears(),
            ledgers: this._proxy.getAllLedgers(),
            productGroups: this._proxy.getAllProductGroupForTableDropdown()
        }).pipe(
            takeUntil(this.destroy$),
            finalize(() => {

                this._cdr.markForCheck();
            })
        ).subscribe({
            next: (result) => {
                // Set data arrays
                this.allAccountLedgers = result.ledgers || [];
                this.allProductGroups = result.productGroups || [];

                if (savedFormValue) {
                    this.form.patchValue(savedFormValue, { emitEvent: false });
                } else if (result.financialData) {
                    this.form.patchValue({
                        fromMiti: result.financialData.fromMiti,
                        toMiti: result.financialData.toMiti
                    }, { emitEvent: false });
                }

                if (!this.form.get('productGroupId').value || this.form.get('productGroupId').value === this.emptyGuId) {
                    this.form.get('productGroupId').setValue(this.allProductGroups?.[0]?.id || this.emptyGuId, { emitEvent: false });
                }
                if (!this.form.get('ledgerId').value || this.form.get('ledgerId').value === this.emptyGuId) {
                    this.form.get('ledgerId').setValue(this.allAccountLedgers?.[0]?.id || this.emptyGuId, { emitEvent: false });
                }

                // Save form and load data
                localStorage.setItem(AppConsts.suktasStorage.stockReportForm, JSON.stringify(this.form.value));
                this.loadReportData();
                this._cdr.markForCheck();
            },
            error: (error) => {

                this.notify.error('Failed to load initial data');
                console.error('Error loading initial data:', error);
                this._cdr.markForCheck();
            }
        });
    }

    // Form creation
    createForm(item: any = {}): void {
        this.form = this._fb.group({
            productGroupId: [item.productGroupId || this.emptyGuId],
            ledgerId: [item.ledgerId || this.emptyGuId],
            fromMiti: [item.fromMiti || this.fromMiti],
            toMiti: [item.toMiti || this.toMiti],
            isZeroStock: [item.isZeroStock || false],
        });
    }

    refresh(): void {
        this.form.reset({
            productGroupId: this.emptyGuId,
            ledgerId: this.emptyGuId,
            isZeroStock: false
        });

        localStorage.removeItem(AppConsts.suktasStorage.stockReportForm);
        this.loadInitialData();
    }

    onSearch(form): void {
        this.isPdfShow = false;
        localStorage.setItem(AppConsts.suktasStorage.stockReportForm, JSON.stringify(form));

        this.getAllData(
            form.fromMiti,
            form.toMiti,
            form.productGroupId,
            form.ledgerId,
            form.isZeroStock
        );
    }

    loadReportData(): void {
        const formValues = this.form.value;
        this.getAllData(
            formValues.fromMiti,
            formValues.toMiti,
            formValues.productGroupId,
            formValues.ledgerId,
            formValues.isZeroStock
        );
    }

    // Data loading
    getAllData(fromMiti: string, toMiti: string, productGroupId: string, ledgerId: string, isZeroStock = false): void {
        this.tableRows = this.setGridRowData(this.gridApi, []);
        this.setPinnedBottomRows([]);
        this.loading = true;
        this._cdr.markForCheck();

        this._proxy.getNewReport(fromMiti, toMiti, productGroupId, ledgerId, isZeroStock)
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this.loading = false;
                    this._cdr.markForCheck();
                })
            )
            .subscribe({
                next: (result) => {
                    this.tableRows = this.setGridRowData(this.gridApi, result);
                    this.calculateTotals();
                    this._cdr.markForCheck();
                },
                error: (error) => {
                    this.notify.error('Failed to load report data');
                    console.error('Error loading report data:', error);
                    this._cdr.markForCheck();
                }
            });
    }

    // Grid initialization
    onGridReady(params): void {
        this.gridApi = params.api;

        // Apply specific CSS for headers
        const style = document.createElement('style');
        style.innerHTML = `
            .ag-theme-balham .ag-header-cell-text {
                font-size: 13px !important;
                font-weight: bold !important;
            }
            .ag-theme-balham .ag-row-pinned {
                background-color: #f5f5f5 !important;
                font-weight: bold !important;
                border-top: 2px solid #711905 !important;
                color: #711905 !important;
            }
        `;
        document.head.appendChild(style);

        // Size columns to fit
        setTimeout(() => this.gridApi.sizeColumnsToFit());

        // Event listeners
        this.gridApi.addEventListener('columnRowGroupChanged', () => {
            this.calculateTotals();
        });

        this.gridApi.addEventListener('filterChanged', () => {
            this.calculateTotals();
        });

        this.gridApi.addEventListener('sortChanged', () => {
            this.calculateTotals();
        });

        // Calculate totals
        if (this.tableRows?.length > 0) {
            this.calculateTotals();
        }
    }

    // Context menu
    getCustomContextMenuItems = (params) => {
        return [
            {
                name: 'Export to Excel',
                action: () => {
                    params.api.exportDataAsExcel({
                        fileName: `StockReport_${  new Date().toISOString().split('T')[0]}`
                    });
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            },
            {
                name: 'Copy Selected',
                action: () => {
                    params.api.copySelectedRowsToClipboard(true);
                },
                icon: '<span class="ag-icon ag-icon-copy"></span>',
            }
        ];
    };

    // Totals calculation
    calculateTotals(): void {
        if (!this.gridApi) {return;}

        // Get displayed rows
        const displayedRows: any[] = [];
        this.gridApi.forEachNodeAfterFilterAndSort(node => {
            if (!node.group && node.data) {
                displayedRows.push(node.data);
            }
        });

        // Clear totals if no rows
        if (!displayedRows.length) {
            this.setPinnedBottomRows([]);
            return;
        }

        // Calculate totals
        const totals = {
            productName: 'Grand Total',
            rate: null,
            openingStockQtyString: `${displayedRows.length  } items`,
            openingStockValue: this.sumFilteredField('openingStockValue', displayedRows),
            inWardQtyString: null,
            inWardValue: this.sumFilteredField('inWardValue', displayedRows),
            outWardQtyString: null,
            outWardValue: this.sumFilteredField('outWardValue', displayedRows),
            closingQtyString: null,
            closingValue: this.sumFilteredField('closingValue', displayedRows),
        };

        this.setPinnedBottomRows([totals]);
    }

    private setPinnedBottomRows(rows: any[]): void {
        this.pinnedBottomRowData = rows;
        this.gridApi?.setGridOption('pinnedBottomRowData', rows);
        this.gridApi?.refreshCells({ force: true });
        this._cdr.markForCheck();
    }

    // Sum helper
    sumFilteredField(fieldName: string, rows: any[]): number {
        return rows.reduce((sum, row) => {
            const value = row[fieldName];
            return sum + (typeof value === 'number' ? value : 0);
        }, 0);
    }

    // Cell double-click handler
    onCellDoubleClicked(params): void {
        if (!params.data?.productId) {return;}

        const {productId} = params.data;
        localStorage.setItem(AppConsts.suktasStorage.stockReportForm, JSON.stringify(this.form.value));
        this._router.navigate(['/app/main/reports/product-wise', productId]);
    }

    // Excel export
    exportToExcelfromAPI(event): void {
        if (!event) {return;}

        const formValues = this.form.value;


        this._proxy.createStockReportToExcelNew(
            formValues.fromMiti,
            formValues.toMiti,
            formValues.productGroupId,
            formValues.ledgerId,
        )
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this._cdr.markForCheck();
                })
            )
            .subscribe({
                next: (result) => {
                    this._fileDownloadService.downloadTempFile(result);
                },
                error: (error) => {
                    this.notify.error('Failed to export to Excel');
                    console.error('Error exporting to Excel:', error);
                }
            });
    }

    excel(): void {
        this.exportToExcelfromAPI(true);
    }

    // PDF download
    pdfDownload(): void {
        const formValues = this.form.value;


        // this._proxy.getPdfDownload(
        //     formValues.fromMiti,
        //     formValues.toMiti,
        //     formValues.productGroupId,
        //     formValues.ledgerId
        // )
        //     .pipe(
        //         takeUntil(this.destroy$),
        //         finalize(() => {

        //             this._cdr.markForCheck();
        //         })
        //     )
        //     .subscribe({
        //         next: (data) => {
        //             this.pdfUrl = 'data:application/pdf;base64,' + data;
        //             this.isPdfShow = true;
        //             this._cdr.markForCheck();
        //         },
        //         error: (error) => {
        //             this.notify.error('Failed to download PDF');
        //             console.error('Error downloading PDF:', error);
        //         }
        //     });
    }

    // UI state handlers
    openAdvanceFilter($event): void {
        this.advancedFiltersAreShown = $event === true;
        this._cdr.markForCheck();
    }

    cancel(): void {
        this.isPdfShow = false;
        this._cdr.markForCheck();
    }

    // Search handler
    searchValueOnApiCall(value: string) {
        this.filterText = value;
        this.searchSubject.next(value);
    }

    // Utility number formatter
    numberWithCommas(x: number): string {
        return x?.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',') || '0.00';
    }
}
