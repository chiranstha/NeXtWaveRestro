import { ChangeDetectorRef, Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import {
    ProfitAndLossAgGridResultDto,
    ProfitAndLossAgGridRowDto,
    ProfitAndLossReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef, GetDataPath, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule, NgClass } from '@angular/common';
import { NepaliDatepickerComponent } from '../../../shared/common/nepalidatepicker/nepali-datepicker-angular.component';
import { AgGridAngular } from 'ag-grid-angular';
import { AgGridFeatureModule } from '@app/shared/common/ag-grid/ag-grid-feature.module';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';
@Component({
    selector: 'app-profit-loss-report',
    templateUrl: './profitloss-report.component.html',
    styleUrls: ['./profitloss-report.component.css'],
    animations: [appModuleAnimation],
    imports: [
        CommonModule,
        NgClass,
        FormsModule,
        ReactiveFormsModule,
        NepaliDatepickerComponent,
        AgGridFeatureModule,
        NgxExtendedPdfViewerModule,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class ProfitLossReportComponent extends AppComponentBase implements OnInit {
    private _profitAndLossService = inject(ProfitAndLossReportServiceProxy);
    _fb = inject(FormBuilder);
    private _fileDownloadService = inject(FileDownloadService);
    private _changeDetector = inject(ChangeDetectorRef);

    public gridApi: GridApi;
    public columnDefs: ColDef[] = [];
    public defaultColDef: ColDef = {
        flex: 1,
        minWidth: 120,
        resizable: true,
        sortable: false,
        filter: false,
        suppressHeaderMenuButton: true,
        suppressHeaderFilterButton: true,
        suppressMovable: true,
    };
    public rowData: ProfitAndLossAgGridRowDto[] = [];
    public treeData = true;
    public groupDefaultExpanded = 0;
    public getDataPath: GetDataPath = (data) => data.path ?? [data.name ?? ''];
    public rowClassRules = {
        'profit-loss-row--summary': (params) => this.isSummaryRow(params.data),
        'profit-loss-row--group': (params) => this.isGroupRow(params.data),
        'profit-loss-row--leaf': (params) => params.data?.isLeaf === true,
    };
    public autoGroupColumnDef: ColDef = {
        headerName: 'PARTICULAR',
        minWidth: 450,
        flex: 2,
        suppressHeaderMenuButton: true,
        suppressHeaderFilterButton: true,
        cellRendererParams: {
            suppressCount: true,
            innerRenderer: (params) => this.treeCellRenderer(params),
        },
        cellRenderer: 'agGroupCellRenderer',
        cellClass: (params) =>
            this.isSummaryRow(params.data)
                ? 'profit-loss-particular-cell profit-loss-particular-cell--summary'
                : this.isGroupRow(params.data)
                  ? 'profit-loss-particular-cell profit-loss-particular-cell--group'
                  : 'profit-loss-particular-cell profit-loss-particular-cell--leaf',
    };

    loading = true;
    advancedFiltersVisible = false;
    advancedFiltersAreShown = false;
    myForm: FormGroup;
    showPdfOk = false;
    pdfUrl: string;
    title = 'Profit & Loss Report';

    totalRevenue = 0;
    totalExpenses = 0;
    grossProfit = 0;
    netProfit = 0;
    private reportRequestId = 0;

    get gridLoading(): boolean {
        return this.loading;
    }

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.getSetting();
        this.createForm();
        this.setupColumnDefs();
        setTimeout(() => {
            this.autoSizeColumns();
            this.createForm();
            this.getProfitAndLossReport();
        }, 500);
    }
    createForm(item: any = {}) {
        this.myForm = this._fb.group({
            fromMiti: [item.fromMiti || this.fromMiti || this.today || undefined],
            toMiti: [item.toMiti || this.toMiti || this.today || undefined],
            isShowOpenClosing: [item.isShowOpenClosing ? item.isShowOpenClosing : false],
        });
    }

    setupColumnDefs(): void {
        this.columnDefs = [
            {
                headerName: 'DEBIT',
                field: 'debit',
                type: 'numericColumn',
                minWidth: 170,
                maxWidth: 230,
                valueFormatter: (params) => this.currencyFormatter(params),
                cellClass: (params) => this.amountCellClass(params, 'debit'),
                cellRenderer: (params) => this.amountRenderer(params.value, 'debit'),
            },
            {
                headerName: 'CREDIT',
                field: 'credit',
                type: 'numericColumn',
                minWidth: 170,
                maxWidth: 230,
                valueFormatter: (params) => this.currencyFormatter(params),
                cellClass: (params) => this.amountCellClass(params, 'credit'),
                cellRenderer: (params) => this.amountRenderer(params.value, 'credit'),
            },
        ];
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

    private treeCellRenderer(params: any): string {
        const value = this.escapeHtml(params.value || '');
        const isSummary = this.isSummaryRow(params.data);
        const isGroup = this.isGroupRow(params.data);
        const modifier = isSummary ? 'summary' : isGroup ? 'group' : 'leaf';
        const icon = isSummary ? 'fa-chart-line' : isGroup ? 'fa-folder-open' : 'fa-file-invoice';

        return `
            <span class="profit-loss-tree-cell profit-loss-tree-cell--${modifier}">
                <span class="profit-loss-tree-cell__icon">
                    <i class="fad ${icon}"></i>
                </span>
                <span class="profit-loss-tree-cell__label">${value}</span>
            </span>
        `;
    }

    private amountRenderer(value: number, tone: 'debit' | 'credit'): string {
        if (!value) {
            return '<span class="profit-loss-amount profit-loss-amount--empty"></span>';
        }

        return `<span class="profit-loss-amount profit-loss-amount--${tone}">${this.formatGridAmount(value)}</span>`;
    }

    private amountCellClass(params: any, tone: 'debit' | 'credit'): string {
        return params.value > 0
            ? `profit-loss-amount-cell profit-loss-amount-cell--${tone}`
            : 'profit-loss-amount-cell profit-loss-amount-cell--empty';
    }

    private isSummaryRow(data?: ProfitAndLossAgGridRowDto): boolean {
        const name = (data?.name || '').replace(/\s+/g, '').toLowerCase();
        return name === 'grossprofit' || name === 'netprofit' || name === 'grandtotal';
    }

    private isGroupRow(data?: ProfitAndLossAgGridRowDto): boolean {
        return !!data && data.isLeaf !== true && !this.isSummaryRow(data);
    }

    private formatGridAmount(amount: number): string {
        return amount.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    private escapeHtml(value: string): string {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
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

    getProfitAndLossReport(): void {
        this.loading = true;
        const requestId = ++this.reportRequestId;
        const { fromMiti, toMiti } = this.getReportDateRange();
        this._profitAndLossService.getReportForAgGrid(fromMiti, toMiti).subscribe(
            (result) => {
                this.commitLatestReportResult(requestId, result);
            },
            () => {
                this.message.error(this.l('ErrorLoadingReport'));
                this.commitViewState(() => {
                    if (requestId === this.reportRequestId) {
                        this.loading = false;
                    }
                });
            },
        );
    }

    exportToExcel(): void {
        const { fromMiti, toMiti } = this.getReportDateRange();
        this.loading = true;
        this._profitAndLossService
            .profitAndLossExportToExcel(fromMiti, toMiti)
            .pipe(finalize(() => (this.loading = false)))
            .subscribe({
                next: (result) => {
                    this._fileDownloadService.downloadTempFile(result);
                    this.message.success(this.l('ExcelExportSuccess'));
                },
                error: () => {
                    this.message.error(this.l('ExportFailed'));
                },
            });
    }

    exportToCsv(): void {
        if (this.gridApi) {
            this.gridApi.exportDataAsCsv({
                fileName: 'ProfitAndLoss_Report.csv',
            });
            this.message.success('CSV export completed successfully');
        }
    }

    exportToPdf(): void {
        const { fromMiti, toMiti } = this.getReportDateRange();
        this.loading = true;
        this._profitAndLossService
            .getPdfDownload(fromMiti, toMiti)
            .pipe(finalize(() => (this.loading = false)))
            .subscribe({
                next: (data) => {
                    this.pdfUrl = `data:application/pdf;base64,${data}`;
                    this.showPdfOk = true;
                },
                error: () => {
                    this.showPdfOk = false;
                    this.message.error(this.l('Failed to generate PDF'));
                },
            });
    }

    back(): void {
        this.showPdfOk = false;
    }

    clearFilters(): void {
        this.createForm();
        this.getProfitAndLossReport();
    }

    toggleAdvancedFilters(): void {
        this.advancedFiltersVisible = !this.advancedFiltersVisible;
    }

    refresh(): void {
        this.getProfitAndLossReport();
    }

    expandAll(): void {
        if (this.gridApi) {
            this.gridApi.expandAll();
        }
    }

    collapseAll(): void {
        if (this.gridApi) {
            this.gridApi.collapseAll();
        }
    }

    autoSizeColumns(): void {
        if (this.gridApi) {
            this.gridApi.sizeColumnsToFit();
        }
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
            fromMiti: this.myForm?.get('fromMiti')?.value || this.fromMiti || undefined,
            toMiti: this.myForm?.get('toMiti')?.value || this.toMiti || undefined,
        };
    }

    private commitLatestReportResult(requestId: number, result: ProfitAndLossAgGridResultDto): void {
        this.commitViewState(() => {
            if (requestId !== this.reportRequestId) {
                return;
            }

            this.rowData = result.rows ?? [];
            this.totalRevenue = result.totalRevenue ?? 0;
            this.totalExpenses = result.totalExpenses ?? 0;
            this.grossProfit = result.grossProfit ?? 0;
            this.netProfit = result.netProfit ?? 0;
            this.loading = false;
        });
    }

    private commitViewState(update: () => void): void {
        setTimeout(() => {
            update();
            this._changeDetector.markForCheck();
        });
    }
}
