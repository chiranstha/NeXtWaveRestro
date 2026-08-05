import {
    ChangeDetectorRef,
    Component,
    OnDestroy,
    OnInit,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { BalanceSheetReportServiceProxy, FinancialStatementDto } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';

import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent, GetDataPath } from 'ag-grid-enterprise';

import { NepaliDatepickerComponent } from '../../../shared/common/nepalidatepicker/nepali-datepicker-angular.component';
@Component({
    selector: 'app-balance-sheet',
    templateUrl: './balance-sheet.component.html',
    styleUrls: ['./balance-sheet.component.css'],
    animations: [appModuleAnimation],
    imports: [FormsModule, ReactiveFormsModule, NepaliDatepickerComponent, AgGridAngular],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class BalanceSheetComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _proxy = inject(BalanceSheetReportServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);
    private _fb = inject(FormBuilder);
    private _changeDetector = inject(ChangeDetectorRef);
    @ViewChild('agGrid') agGrid: AgGridAngular;

    pageTitle = 'BalanceSheet';
    public gridApi: GridApi;
    public columnDefs: ColDef[] = [];
    public defaultColDef: ColDef = {
        flex: 1,
        minWidth: 120,
        resizable: true,
        sortable: true,
        filter: true,
    };
    public rowData: any[] = [];
    public treeData = true;
    public groupDefaultExpanded = 0;
    public getDataPath: GetDataPath = (data) => data.path;
    public rowClassRules = {
        'balance-sheet-row--group': (params: any) =>
            params.data && (params.data.groupType === 0 || params.data.groupType === 1),
        'balance-sheet-row--leaf': (params: any) =>
            params.data && params.data.groupType !== 0 && params.data.groupType !== 1,
        'balance-sheet-row--summary': (params: any) => this.isSummaryRow(params.data),
    };
    public autoGroupColumnDef: ColDef = {
        headerName: 'PARTICULAR',
        minWidth: 400,
        flex: 2,
        cellRendererParams: {
            suppressCount: true,
            innerRenderer: (params: any) => {
                const isGroup = params.data && (params.data.groupType === 0 || params.data.groupType === 1);
                const isSummary = this.isSummaryRow(params.data);
                const cellClass = isSummary
                    ? 'balance-sheet-tree-cell--summary'
                    : isGroup
                      ? 'balance-sheet-tree-cell--group'
                      : 'balance-sheet-tree-cell--leaf';
                const icon = isSummary ? 'fa-calculator' : isGroup ? 'fa-folder-open' : 'fa-file-invoice';
                return `
                    <span class="balance-sheet-tree-cell ${cellClass}">
                        <span class="balance-sheet-tree-cell__icon">
                            <i class="fas ${icon}"></i>
                        </span>
                        <span class="balance-sheet-tree-cell__label">${this.escapeHtml(params.value || '')}</span>
                    </span>
                `;
            },
        },
        cellRenderer: 'agGroupCellRenderer',
    };

    loading = false;
    advancedFiltersAreShown = false;
    myForm: FormGroup;
    showPdfOk = false;
    title = 'Balance Sheet Report';
    pdfUrl: string;
    allData: FinancialStatementDto[] = [];

    totalAssets = 0;
    totalLiabilities = 0;
    netWorth = 0;
    workingCapital = 0;
    private reportRequestId = 0;
    private destroyed = false;
    private destroy$: Subject<boolean> = new Subject();

    get gridLoading(): boolean {
        return this.loading;
    }

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.getSetting();
        this.createForm();
        this.setupColumnDefs();
        this.loadReport();
    }
    ngOnDestroy(): void {
        this.destroyed = true;
        this.destroy$.next(true);
        this.destroy$.complete();
    }
    createForm(item: any = {}): void {
        this.myForm = this._fb.group({
            fromMiti: [item.fromMiti || this.fromMiti || this.today || undefined],
            toMiti: [item.toMiti || this.toMiti || this.today || undefined],
        });
    }
    setupColumnDefs(): void {
        this.columnDefs = [
            {
                headerName: 'ASSETS',
                field: 'debit',
                type: 'numericColumn',
                width: 150,
                cellClass: 'balance-sheet-amount-cell',
                cellRenderer: (params: any) => this.amountCellRenderer(params, 'assets'),
            },
            {
                headerName: 'LIABILITIES & EQUITY',
                field: 'credit',
                type: 'numericColumn',
                width: 150,
                cellClass: 'balance-sheet-amount-cell',
                cellRenderer: (params: any) => this.amountCellRenderer(params, 'liabilities'),
            },
        ];
    }
    private amountCellRenderer(params: any, tone: 'assets' | 'liabilities'): string {
        const value = params.value;
        if (value === null || value === undefined || value === 0) {
            return '<span class="balance-sheet-amount balance-sheet-amount--empty"></span>';
        }

        const negativeClass = value < 0 ? ' balance-sheet-amount--negative' : '';
        return `<span class="balance-sheet-amount balance-sheet-amount--${tone}${negativeClass}">
            ${this.currencyFormatter(params)}
        </span>`;
    }
    private isSummaryRow(row: any): boolean {
        const name = row?.name?.toLowerCase?.() || '';
        return (
            name.includes('total') || name.includes('net worth') || name.includes('capital') || name.includes('equity')
        );
    }
    private escapeHtml(value: string): string {
        return value
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
    currencyFormatter = (params: any): string => {
        if (params.value === null || params.value === undefined || params.value === 0) {
            return '';
        }
        return params.value.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    };
    loadReport(): void {
        const requestId = ++this.reportRequestId;
        this.loading = true;
        const { fromMiti, toMiti } = this.getReportDateRange();
        this._proxy
            .getReport(fromMiti, toMiti)
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this.commitViewState(() => {
                        if (requestId === this.reportRequestId) {
                            this.loading = false;
                        }
                    });
                }),
            )
            .subscribe({
                next: (data) => {
                    this.commitLatestReportResult(requestId, data ?? []);
                },
                error: () => {
                    this.notify.error(this.l('ErrorLoadingReport'));
                },
            });
    }
    onAdvanceSearch(): void {
        this.loadReport();
    }
    transformDataForAgGrid(data: FinancialStatementDto[], path: string[] = []): any[] {
        const result: any[] = [];
        if (!data) {
            return result;
        }
        data.forEach((item) => {
            const currentPath = [...path, item.data.name];
            const gridItem = {
                ...item.data,
                path: currentPath,
                id: item.id,
            };
            result.push(gridItem);
            if (item.children && item.children.length > 0) {
                const childItems = this.transformDataForAgGrid(item.children, currentPath);
                result.push(...childItems);
            }
        });
        return result;
    }
    calculateSummary(): void {
        this.totalAssets = 0;
        this.totalLiabilities = 0;
        this.netWorth = 0;
        this.workingCapital = 0;
        let currentAssets = 0;
        let currentLiabilities = 0;
        this.allData.forEach((node) => {
            const name = node.data.name.toLowerCase();

            if (name.includes('assets')) {
                this.totalAssets += node.data.debit || 0;
                if (name.includes('current')) {
                    currentAssets += node.data.debit || 0;
                }
            }

            if (name.includes('liabilities') || name.includes('capital') || name.includes('equity')) {
                this.totalLiabilities += node.data.credit || 0;
                if (name.includes('current')) {
                    currentLiabilities += node.data.credit || 0;
                }
            }

            if (name.includes('capital') || name.includes('equity')) {
                this.netWorth += node.data.credit || 0;
            }
        });

        this.workingCapital = currentAssets - currentLiabilities;

        if (this.netWorth === 0) {
            this.netWorth = this.totalAssets - (this.totalLiabilities - this.netWorth);
        }
    }
    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.gridApi.sizeColumnsToFit();
    }
    searchDataTable(event: any): void {
        const { value } = event.target;
        if (this.gridApi) {
            (this.gridApi as any).setQuickFilter(value);
        }
    }
    toggleAdvancedFilters(): void {
        this.advancedFiltersAreShown = !this.advancedFiltersAreShown;
    }
    exportToExcel(): void {
        const { fromMiti, toMiti } = this.getReportDateRange();
        this.loading = true;
        this._proxy
            .balanceSheetExportToExcel(fromMiti, toMiti)
            .pipe(finalize(() => this.commitViewState(() => (this.loading = false))))
            .subscribe({
                next: (result) => {
                    this._fileDownloadService.downloadTempFile(result);
                    this.notify.success(this.l('ExportSuccessful'));
                },
                error: () => {
                    this.notify.error(this.l('ExportFailed'));
                },
            });
    }
    exportToExcelfromAPI(event: any): void {
        if (event) {
            this.exportToExcel();
        }
    }
    showPdf(): void {
        this.showPdfOk = true;
        this.loading = true;
        const { fromMiti, toMiti } = this.getReportDateRange();
        this._proxy
            .getPdfDownload(fromMiti, toMiti)
            .pipe(finalize(() => this.commitViewState(() => (this.loading = false))))
            .subscribe({
                next: (data) => {
                    this.commitViewState(() => {
                        this.pdfUrl = `data:application/pdf;base64,${data}`;
                    });
                },
                error: () => {
                    this.commitViewState(() => {
                        this.showPdfOk = false;
                    });
                    this.notify.error(this.l('Failed to generate PDF'));
                },
            });
    }
    back(): void {
        this.showPdfOk = false;
    }
    refresh(): void {
        this.loadReport();
    }
    expandAll(): void {
        this.gridApi?.expandAll();
    }
    collapseAll(): void {
        this.gridApi?.collapseAll();
    }
    formatCurrency(amount: number): string {
        if (amount === undefined || amount === null) {
            return '0.00';
        }
        const currency = this.l('Currency') === 'Currency' ? 'Rs.' : this.l('Currency');
        return `${currency} ${amount.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
    }
    private getReportDateRange(): { fromMiti: string | undefined; toMiti: string | undefined } {
        return {
            fromMiti: this.normalizeMiti(this.myForm?.get('fromMiti')?.value || this.fromMiti || this.today),
            toMiti: this.normalizeMiti(this.myForm?.get('toMiti')?.value || this.toMiti || this.today),
        };
    }
    private normalizeMiti(value: string | null | undefined): string | undefined {
        const normalized = typeof value === 'string' ? value.trim() : value;
        return normalized || undefined;
    }
    private commitLatestReportResult(requestId: number, data: FinancialStatementDto[]): void {
        this.commitViewState(() => {
            if (requestId !== this.reportRequestId) {
                return;
            }

            this.allData = data;
            this.rowData = this.transformDataForAgGrid(data);
            this.calculateSummary();
        });
    }
    private commitViewState(update: () => void): void {
        setTimeout(() => {
            if (this.destroyed) {
                return;
            }

            update();
            this._changeDetector.markForCheck();
        });
    }
}
