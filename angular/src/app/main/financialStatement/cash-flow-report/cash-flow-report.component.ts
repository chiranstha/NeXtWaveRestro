import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AgGridFeatureModule } from '@app/shared/common/ag-grid/ag-grid-feature.module';
import { NepaliDatepickerComponent } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.component';
import { ColDef, GetDataPath, GridApi, GridReadyEvent } from 'ag-grid-community';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';
import { finalize } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CashFlowReportServiceProxy, CashFlowStatementDto } from '@shared/service-proxies/service-proxies';

interface FlattenedCashFlowRow {
    name: string;
    path: string[];
    groupType: string;
    currentAmount: number;
    previousAmount: number;
    variance: number;
    level: number;
}

@Component({
    selector: 'app-cash-flow-report',
    templateUrl: './cash-flow-report.component.html',
    styleUrls: ['./cash-flow-report.component.css'],
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        NepaliDatepickerComponent,
        NgxExtendedPdfViewerModule,
        AgGridFeatureModule,
    ],
})
export class CashFlowReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly fb = inject(FormBuilder);
    private readonly proxy = inject(CashFlowReportServiceProxy);
    private readonly changeDetector = inject(ChangeDetectorRef);

    form: FormGroup;
    rows: FlattenedCashFlowRow[] = [];
    loading = false;
    filtersExpanded = true;
    showPdfOk = false;
    pdfUrl: string;
    title = 'Cash Flow Statement';
    currentNetCash = 0;
    previousNetCash = 0;
    operatingCash = 0;
    investingCash = 0;

    readonly defaultColDef: ColDef = {
        filter: true,
        floatingFilter: true,
        minWidth: 150,
        resizable: true,
        sortable: true,
    };

    readonly columnDefs: ColDef[] = [
        {
            field: 'groupType',
            headerName: 'Type',
            width: 120,
            minWidth: 110,
            maxWidth: 140,
        },
        {
            field: 'currentAmount',
            headerName: 'Current Period',
            type: 'numericColumn',
            flex: 1,
            minWidth: 175,
            cellClass: 'text-end tabular-number cash-amount--current',
            valueFormatter: (params) => this.formatAmount(params.value),
        },
        {
            field: 'previousAmount',
            headerName: 'Comparison Period',
            type: 'numericColumn',
            flex: 1,
            minWidth: 175,
            cellClass: 'text-end tabular-number cash-amount--previous',
            valueFormatter: (params) => this.formatAmount(params.value),
        },
        {
            field: 'variance',
            headerName: 'Variance',
            type: 'numericColumn',
            flex: 1,
            minWidth: 165,
            cellClass: (params) =>
                `text-end tabular-number fw-semibold ${Number(params.value) < 0 ? 'cash-variance--negative' : 'cash-variance--positive'}`,
            valueFormatter: (params) => this.formatSignedAmount(params.value),
        },
    ];

    readonly autoGroupColumnDef: ColDef = {
        headerName: 'Cash Flow Activity',
        minWidth: 330,
        flex: 1.8,
        filter: 'agTextColumnFilter',
        floatingFilter: true,
        cellRendererParams: {
            suppressCount: true,
            innerRenderer: (params: any) => {
                const row = params.data as FlattenedCashFlowRow;
                const groupClass = row?.level === 0 ? 'cash-flow-tree__label--group' : 'cash-flow-tree__label--leaf';
                const icon = row?.level === 0 ? 'fa-folder-open' : 'fa-file-invoice-dollar';
                return `<span class="cash-flow-tree ${groupClass}"><i class="fa-duotone ${icon}"></i><span>${this.escapeHtml(params.value || '')}</span></span>`;
            },
        },
    };

    readonly getDataPath: GetDataPath = (data: FlattenedCashFlowRow) => data.path;
    readonly statusBar = {
        statusPanels: [
            { statusPanel: 'agTotalAndFilteredRowCountComponent', align: 'left' },
            { statusPanel: 'agAggregationComponent', align: 'right' },
        ],
    };

    private gridApi?: GridApi;
    private destroyed = false;
    private reportRequestId = 0;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.getSetting();
        this.form = this.fb.group({
            fromMiti: [this.fromMiti || this.today],
            toMiti: [this.toMiti || this.today],
            comparisonPeriod: ['previous-year'],
        });
        this.loadReport();
    }

    ngOnDestroy(): void {
        this.destroyed = true;
    }

    loadReport(): void {
        const requestId = ++this.reportRequestId;
        const value = this.form.getRawValue();
        this.loading = true;
        this.proxy
            .getCashFlowReport(value.fromMiti, value.toMiti, value.comparisonPeriod)
            .pipe(
                finalize(() => {
                    this.commitViewState(() => {
                        if (requestId === this.reportRequestId) {
                            this.loading = false;
                        }
                    });
                }),
            )
            .subscribe({
                next: (result) => {
                    this.commitViewState(() => {
                        if (requestId !== this.reportRequestId) {
                            return;
                        }

                        this.rows = this.flatten(result ?? []);
                        this.updateSummary();
                        this.gridApi?.setGridOption('rowData', this.rows);
                    });
                },
                error: () => {
                    this.notify.error(this.l('FailedToLoadData'));
                },
            });
    }

    onGridReady(event: GridReadyEvent): void {
        this.gridApi = event.api;
        setTimeout(() => event.api.sizeColumnsToFit());
    }

    search(value: string): void {
        this.gridApi?.setGridOption('quickFilterText', value?.trim() ?? '');
    }

    exportToExcel(): void {
        const value = this.form.getRawValue();
        this.gridApi?.exportDataAsExcel({
            fileName: `cash-flow-${value.fromMiti}-${value.toMiti}.xlsx`,
            sheetName: 'Cash Flow',
        });
    }

    showPdf(): void {
        const value = this.form.getRawValue();
        this.showPdfOk = true;
        this.loading = true;
        this.proxy
            .getPdfDownload(value.fromMiti, value.toMiti, value.comparisonPeriod)
            .pipe(finalize(() => this.commitViewState(() => (this.loading = false))))
            .subscribe({
                next: (data) => {
                    this.commitViewState(() => {
                        this.pdfUrl = `data:application/pdf;base64,${data}`;
                    });
                },
                error: () => {
                    this.commitViewState(() => (this.showPdfOk = false));
                    this.notify.error(this.l('Failed to generate PDF'));
                },
            });
    }

    toggleFilters(): void {
        this.filtersExpanded = !this.filtersExpanded;
    }

    back(): void {
        this.showPdfOk = false;
    }

    formatAmount(value: unknown): string {
        return (Number(value) || 0).toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    formatSignedAmount(value: unknown): string {
        const amount = Number(value) || 0;
        if (!amount) {
            return '0.00';
        }
        return `${amount > 0 ? '+' : '−'}${this.formatAmount(Math.abs(amount))}`;
    }

    private flatten(items: CashFlowStatementDto[], level = 0, parentPath: string[] = []): FlattenedCashFlowRow[] {
        const rows: FlattenedCashFlowRow[] = [];
        (items ?? []).forEach((item) => {
            if (!item?.data) {
                return;
            }

            const name = item.data.name ?? '';
            const path = [...parentPath, name];
            const currentAmount = item.data.currentAmount ?? 0;
            const previousAmount = item.data.previousAmount ?? 0;
            rows.push({
                name,
                path,
                groupType: item.data.groupType?.toString() ?? '',
                currentAmount,
                previousAmount,
                variance: currentAmount - previousAmount,
                level,
            });

            if (item.children?.length > 0) {
                rows.push(...this.flatten(item.children, level + 1, path));
            }
        });
        return rows;
    }

    private updateSummary(): void {
        const netCash = this.findSummaryRow(['net increase', 'net decrease', 'net cash', 'closing cash']);
        const operating = this.findSummaryRow(['operating activit']);
        const investing = this.findSummaryRow(['investing activit']);
        this.currentNetCash = netCash?.currentAmount ?? 0;
        this.previousNetCash = netCash?.previousAmount ?? 0;
        this.operatingCash = operating?.currentAmount ?? 0;
        this.investingCash = investing?.currentAmount ?? 0;
    }

    private findSummaryRow(labels: string[]): FlattenedCashFlowRow | undefined {
        return [...this.rows]
            .reverse()
            .find((row) => labels.some((label) => row.name.toLowerCase().includes(label)));
    }

    private escapeHtml(value: string): string {
        return value
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    private commitViewState(update: () => void): void {
        setTimeout(() => {
            if (this.destroyed) {
                return;
            }

            update();
            this.changeDetector.markForCheck();
        });
    }
}
