import {
    Component,
    OnDestroy,
    OnInit,
    ViewChild,
    ElementRef,
    HostListener,
    inject,
    signal,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    FinancialStatementDto,
    ReportingServiceProxy,
    TrailBalanceReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { Subscription } from 'rxjs';
import { finalize, tap } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';

import { ColDef, GridApi, GridOptions, GridReadyEvent } from 'ag-grid-enterprise';
import { AgGridAngular } from 'ag-grid-angular';
import { NgClass, DecimalPipe } from '@angular/common';
import { NepaliDatepickerComponent } from '../../../shared/common/nepalidatepicker/nepali-datepicker-angular.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AddProductComponent } from '@app/main/inventory/products/addProduct/addProduct.component';
@Component({
    selector: 'app-trial-balance-report',
    templateUrl: './trial-balance-report.component.html',
    styleUrls: ['./trial-balance-report.component.css'],
    animations: [appModuleAnimation],
    imports: [
        NgClass,
        FormsModule,
        ReactiveFormsModule,
        NepaliDatepickerComponent,
        AgGridAngular,
        DecimalPipe,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class TrialBalanceReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    _proxy = inject(TrailBalanceReportServiceProxy);
    _fileDownloadService = inject(FileDownloadService);
    _fb = inject(FormBuilder);
    _router = inject(Router);
    _allProxy = inject(ReportingServiceProxy);
    @ViewChild(AgGridAngular) agGrid!: AgGridAngular;
    @ViewChild('exportDropdown') exportDropdownRef!: ElementRef;
    @ViewChild('productCreateModal', { static: true }) productCreateModal: AddProductComponent;

    loadingReport: Subscription;
    getAllAccGroup: Subscription;

    treeLength = signal<number>(0);

    allAccountGroups: any[] = [];
    loading = true;
    exportDropdownOpen = false;
    myForm: FormGroup;
    advancedFiltersAreShown = false;
    enabledDate = false;
    pdfUrl: string;
    title = 'Trial Balance Report';
    showPdfOk = false;
    allData: FinancialStatementDto[] = [];
    Math = Math;

    public gridApi: GridApi;
    public gridOptions: GridOptions;
    public columnDefs: ColDef[];
    public defaultColDef: ColDef;
    public rowData: any[] = [];
    public getDataPath: (data: any) => string[];
    public autoGroupColumnDef: ColDef;
    constructor() {
        super();
        this.getSetting();
        this.initializeAgGrid();
        this.myForm = this._fb.group({
            fromMiti: [null],
            toMiti: [null],
            isShowOpenClosing: [false],
        });
    }
    public initializeAgGrid() {
        this.columnDefs = [
            {
                headerName: 'Opening Debit',
                field: 'openingDr',
                type: 'numericColumn',
                valueFormatter: this.currencyFormatter,
                minWidth: 120,
                cellClass: 'fw-medium text-dark',
            },
            {
                headerName: 'Opening Credit',
                field: 'openingCr',
                type: 'numericColumn',
                valueFormatter: this.currencyFormatter,
                minWidth: 120,
                cellClass: 'fw-medium text-dark',
            },
            {
                headerName: 'Transaction Debit',
                field: 'debit',
                type: 'numericColumn',
                valueFormatter: this.currencyFormatter,
                minWidth: 140,
                cellClass: 'fw-bold text-success',
            },
            {
                headerName: 'Transaction Credit',
                field: 'credit',
                type: 'numericColumn',
                valueFormatter: this.currencyFormatter,
                minWidth: 140,
                cellClass: 'fw-bold text-danger',
            },
            {
                headerName: 'Closing Debit',
                field: 'closingDr',
                type: 'numericColumn',
                valueFormatter: this.currencyFormatter,
                minWidth: 120,
                cellClass: 'fw-bold text-primary',
            },
            {
                headerName: 'Closing Credit',
                field: 'closingCr',
                type: 'numericColumn',
                valueFormatter: this.currencyFormatter,
                minWidth: 120,
                cellClass: 'fw-bold text-primary',
            },
        ];
        this.defaultColDef = {
            flex: 1,
            sortable: false,
            resizable: true,
            filter: true,
        };

        this.getDataPath = (data) => {
            return data.path || [];
        };
        this.autoGroupColumnDef = {
            headerName: 'PARTICULAR',
            minWidth: 250,
            cellRendererParams: {
                suppressCount: true,
                innerRenderer: (params) => {
                    if (
                        params.data &&
                        (params.data.groupType === 1 || params.data.groupType === 2 || params.data.groupType === 5)
                    ) {
                        return `<span class="clickable-cell" style="cursor: pointer; font-weight: 600;">${params.value || ''}</span>`;
                    }
                    return params.value || '';
                },
            },
            cellRenderer: 'agGroupCellRenderer',
            cellClass: (params) => {
                if (params.data?.name === 'GrandTotal') {
                    return 'fw-bold text-primary';
                }
                return '';
            },
        };
        this.gridOptions = {
            columnDefs: this.columnDefs,
            defaultColDef: this.defaultColDef,
            animateRows: true,
            enableRangeSelection: true,
            treeData: true,
            getDataPath: this.getDataPath,
            autoGroupColumnDef: this.autoGroupColumnDef,
            groupDefaultExpanded: 0,
            suppressAggFuncInHeader: true,
            enableCellTextSelection: true,
            rowClassRules: {
                'total-row'(params) {
                    return params.data?.name === 'GrandTotal';
                },
                'ledger-row'(params) {
                    return params.data?.groupType === 1;
                },
                'account-group-row'(params) {
                    return params.data?.groupType === 2;
                },
            },
            onCellClicked: (params) => {
                if (params.column.getColId() === 'ag-Grid-AutoColumn') {
                    const { data } = params;
                    if (data && (data.groupType === 1 || data.groupType === 2 || data.groupType === 5)) {
                        this.redirectToAccountGroup(data.id, data.groupType);
                    }
                }
            },
        };
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.createForm();
        this.loadReport();
    }
    createForm(item: any = {}) {
        this.myForm = this._fb.group({
            fromMiti: [item.fromMiti ? item.fromMiti : this.fromMiti],
            toMiti: [item.toMiti ? item.toMiti : this.toMiti],
            isShowOpenClosing: [item.isShowOpenClosing ? item.isShowOpenClosing : false],
        });
    }
    ngOnDestroy(): void {
        if (this.loadingReport) {
            this.loadingReport.unsubscribe();
        }
        if (this.getAllAccGroup) {
            this.getAllAccGroup.unsubscribe();
        }
    }
    toggleExportDropdown(event: Event) {
        event.stopPropagation();
        this.exportDropdownOpen = !this.exportDropdownOpen;
    }
    @HostListener('document:click', ['$event'])
    onDocumentClick(event: Event) {
        try {
            if (
                this.exportDropdownOpen &&
                this.exportDropdownRef &&
                !this.exportDropdownRef.nativeElement.contains(event.target)
            ) {
                this.exportDropdownOpen = false;
            }
        } catch {
            /* empty */
        }
    }
    onAdvanceSearch() {
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        this.loading = true;
        this._proxy
            .getReport(fromMiti, toMiti)
            .pipe(
                tap(() => {
                    this.loading = false;
                }),
            )
            .subscribe({
                next: (data) => {
                    this.allData = data;
                    this.processDataForAgGrid(data);
                    this.treeLength.set(this.rowData.length);
                },
                error: (err) => {
                    this.loading = false;
                    this.notify.error(this.l('An error occurred while loading data'));
                    console.error('Error loading report:', err);
                },
            });
    }
    loadReport() {
        this.loading = true;
        this.loadingReport = this._proxy
            .getReport(this.fromMiti, this.toMiti)
            .pipe(finalize(() => (this.loading = false)))
            .subscribe({
                next: (data) => {
                    this.allData = data;
                    this.processDataForAgGrid(data);
                    this.treeLength.set(this.rowData.length);
                },
                error: (err) => {
                    this.notify.error(this.l('An error occurred while loading data'));
                    console.error('Error loading report:', err);
                },
            });
    }
    processDataForAgGrid(data: FinancialStatementDto[]) {
        this.rowData = this.transformDataForAgGrid(data);
    }
    transformDataForAgGrid(data: FinancialStatementDto[], parentPath: string[] = []) {
        let result = [];
        if (!data || data.length === 0) {
            return result;
        }
        data.forEach((item) => {
            const currentPath = [...parentPath, item.data.name];

            const node = {
                ...item.data,
                path: currentPath,
                id: item.id,
            };
            result.push(node);

            if (item.children && item.children.length > 0) {
                const childrenData = this.transformDataForAgGrid(item.children, currentPath);
                result = result.concat(childrenData);
            }
        });
        return result;
    }
    refresh() {
        this.loadReport();
        if (this.enabledDate) {
            this.myForm.get('isShowOpenClosing').setValue(false);
        }
    }
    searchDataTable(e) {
        if (e) {
            (this.gridApi as any).setQuickFilter(e);
        } else {
            (this.gridApi as any).setQuickFilter(null);
        }
    }
    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }
    expandAll() {
        this.gridApi.expandAll();
    }
    collapseAll() {
        this.gridApi.collapseAll();
    }

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;

        if (this.gridApi) {
            setTimeout(() => {}, 100);
        }
    }
    redirectToAccountGroup(id, groupTypeId) {
        if (groupTypeId === 1) {
            if (this.permission.isGranted('Pages.AccountLedgerReport')) {
                this._router.navigate(['/app/main/reports/account-ledger-report'], { queryParams: { id } });
            } else {
                this.notify.error('Permission Not Granted');
            }
        } else if (groupTypeId === 2) {
            if (this.permission.isGranted('Pages.AccountWiseNewLedgerReport')) {
                this._router.navigate(['/app/main/reports/account-wise'], { queryParams: { id } });
            } else {
                this.notify.error('Permission Not Granted');
            }
        } else if (groupTypeId === 5) {
            if (this.permission.isGranted('Pages.Products.Edit')) {
                this.productCreateModal.show(id);
            } else {
                this.notify.error('Permission Not Granted');
            }
        }
    }

    currencyFormatter(params) {
        if (params.value === null || params.value === undefined) {
            return '';
        }

        return new Intl.NumberFormat('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        }).format(Math.abs(params.value));
    }

    enabledorDisabled() {
        const isShowOpenClosing = this.myForm.get('isShowOpenClosing').value;
        if (isShowOpenClosing) {
            this.enabledDate = true;
        } else {
            this.enabledDate = false;
        }
    }
    exportToExcelfromAPI(event) {
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        if (event) {
            this._proxy.trialBalanceExportToExcel(fromMiti, toMiti).subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
                this.notify.success(this.l('ExportSuccessful'));
            });
        }
    }
    exportToPdffromAPI(event) {
        if (event) {
            this._router.navigate(['app/reports/pdf/1']);
        }
    }

    onExportToExcel() {
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        this._proxy.trialBalanceExportToExcel(fromMiti, toMiti).subscribe((result) => {
            this._fileDownloadService.downloadTempFile(result);
            this.notify.success(this.l('ExportSuccessful'));
        });
    }
    showPdf() {
        this.showPdfOk = true;
        this.loading = true;
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        this._proxy.getPdfDownload(fromMiti, toMiti).subscribe({
            next: (data) => {
                const linkSource = `data:application/pdf;base64,${data}`;
                this.pdfUrl = linkSource;
                this.loading = false;
            },
            error: (err) => {
                this.loading = false;
                this.showPdfOk = false;
                this.notify.error(this.l('Failed to generate PDF'));
                console.error('PDF generation error:', err);
            },
        });
    }
    back() {
        this.showPdfOk = false;
    }

    getTotalOpeningDebit(): number {
        if (!this.rowData || this.rowData.length === 0) {
            return 0;
        }

        const grandTotal = this.rowData.find((row) => row.name === 'GrandTotal');
        if (grandTotal) {
            return grandTotal.openingDr || 0;
        }

        return this.rowData.reduce((sum, row) => {
            if (row.path?.length === 1) {
                return sum + (row.openingDr || 0);
            }
            return sum;
        }, 0);
    }
    getTotalOpeningCredit(): number {
        if (!this.rowData || this.rowData.length === 0) {
            return 0;
        }
        const grandTotal = this.rowData.find((row) => row.name === 'GrandTotal');
        if (grandTotal) {
            return grandTotal.openingCr || 0;
        }
        return this.rowData.reduce((sum, row) => {
            if (row.path?.length === 1) {
                return sum + (row.openingCr || 0);
            }
            return sum;
        }, 0);
    }
    getTotalClosingDebit(): number {
        if (!this.rowData || this.rowData.length === 0) {
            return 0;
        }
        const grandTotal = this.rowData.find((row) => row.name === 'GrandTotal');
        if (grandTotal) {
            return grandTotal.closingDr || 0;
        }
        return this.rowData.reduce((sum, row) => {
            if (row.path?.length === 1) {
                return sum + (row.closingDr || 0);
            }
            return sum;
        }, 0);
    }
    getTotalClosingCredit(): number {
        if (!this.rowData || this.rowData.length === 0) {
            return 0;
        }
        const grandTotal = this.rowData.find((row) => row.name === 'GrandTotal');
        if (grandTotal) {
            return grandTotal.closingCr || 0;
        }
        return this.rowData.reduce((sum, row) => {
            if (row.path?.length === 1) {
                return sum + (row.closingCr || 0);
            }
            return sum;
        }, 0);
    }
    getTotalDebit(): number {
        if (!this.rowData || this.rowData.length === 0) {
            return 0;
        }
        const grandTotal = this.rowData.find((row) => row.name === 'GrandTotal');
        if (grandTotal) {
            return grandTotal.debit || 0;
        }
        return this.rowData.reduce((sum, row) => {
            if (row.path?.length === 1) {
                return sum + (row.debit || 0);
            }
            return sum;
        }, 0);
    }
    getTotalCredit(): number {
        if (!this.rowData || this.rowData.length === 0) {
            return 0;
        }
        const grandTotal = this.rowData.find((row) => row.name === 'GrandTotal');
        if (grandTotal) {
            return grandTotal.credit || 0;
        }
        return this.rowData.reduce((sum, row) => {
            if (row.path?.length === 1) {
                return sum + (row.credit || 0);
            }
            return sum;
        }, 0);
    }
    get isBalanced(): boolean {
        return Math.abs(this.getTotalDebit() - this.getTotalCredit()) < 0.01;
    }
}
