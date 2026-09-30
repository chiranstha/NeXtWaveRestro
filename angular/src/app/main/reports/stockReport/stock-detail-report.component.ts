import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ColDef, GridOptions } from 'ag-grid-community';
import { Subject, finalize, takeUntil } from 'rxjs';
import { StockDetailReport, StockDetailReportApiService, StockDetailReportRow } from './stock-detail-report-api.service';

@Component({
    standalone: false,
    selector: 'app-stock-detail-report',
    templateUrl: './stock-detail-report.component.html',
    styleUrls: ['./stock-detail-report.component.css'],
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class StockDetailReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    form: FormGroup;
    productId = '';
    report: StockDetailReport | null = null;
    rows: StockDetailReportRow[] = [];
    chartMode: 'quantity' | 'value' | 'movement' = 'quantity';
    dateRange = '30 Days';
    loading = false;
    loadError = false;
    searchText = '';
    private readonly destroy$ = new Subject<void>();

    readonly defaultColDef: ColDef = { resizable: true, sortable: true, filter: true, minWidth: 105, flex: 1 };
    readonly columnDefs: ColDef[] = [
        { headerName: 'DATE (B.S.)', field: 'dateMiti', minWidth: 120, pinned: 'left' },
        { headerName: 'DATE', field: 'date', minWidth: 120, valueFormatter: p => p.value ? new Date(p.value).toLocaleDateString() : '' },
        { headerName: 'VOUCHER NO.', field: 'voucherNo', minWidth: 130, pinned: 'left' },
        { headerName: 'VOUCHER TYPE', field: 'voucherType', minWidth: 150 },
        { headerName: 'LEDGER NAME', field: 'ledgerName', minWidth: 190 },
        { headerName: 'INWARD QTY', field: 'inwardQty', type: 'numericColumn', valueFormatter: p => this.formatNumber(p.value), cellClass: 'stock-detail-inward' },
        { headerName: 'RATE', field: 'inwardRate', type: 'numericColumn', valueFormatter: p => this.formatNumber(p.value) },
        { headerName: 'INWARD AMOUNT', field: 'inwardAmount', type: 'numericColumn', valueFormatter: p => this.formatNumber(p.value), cellClass: 'stock-detail-inward' },
        { headerName: 'OUTWARD QTY', field: 'outwardQty', type: 'numericColumn', valueFormatter: p => this.formatNumber(p.value), cellClass: 'stock-detail-outward' },
        { headerName: 'RATE', field: 'outwardRate', type: 'numericColumn', valueFormatter: p => this.formatNumber(p.value) },
        { headerName: 'OUTWARD AMOUNT', field: 'outwardAmount', type: 'numericColumn', valueFormatter: p => this.formatNumber(p.value), cellClass: 'stock-detail-outward' },
        { headerName: 'BALANCE QTY', field: 'balanceQty', type: 'numericColumn', valueFormatter: p => this.formatNumber(p.value), cellClass: 'stock-detail-balance', pinned: 'right' }
    ];
    readonly gridOptions: GridOptions = { animateRows: true, rowHeight: 42, headerHeight: 46, suppressCellFocus: false };

    constructor(
        injector: Injector,
        private readonly fb: FormBuilder,
        private readonly route: ActivatedRoute,
        private readonly router: Router,
        private readonly api: StockDetailReportApiService,
        private readonly cdr: ChangeDetectorRef
    ) {
        super(injector);
        this.form = this.fb.group({ fromMiti: [this.fromMiti], toMiti: [this.toMiti] });
    }

    ngOnInit(): void {
        this.productId = this.route.snapshot.paramMap.get('id') || '';
        this.loadReport();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    loadReport(): void {
        if (!this.productId) {
            this.loadError = true;
            return;
        }
        this.loading = true;
        this.loadError = false;
        this.api.getReport(this.productId, this.form.value.fromMiti, this.form.value.toMiti)
            .pipe(takeUntil(this.destroy$), finalize(() => {
                this.loading = false;
                this.cdr.markForCheck();
            }))
            .subscribe({
                next: report => {
                    this.report = report;
                    this.rows = report?.rows || [];
                    this.cdr.markForCheck();
                },
                error: () => {
                    this.loadError = true;
                    this.cdr.markForCheck();
                }
            });
    }

    setChartMode(mode: 'quantity' | 'value' | 'movement'): void { this.chartMode = mode; }
    setDateRange(range: string): void { this.dateRange = range; }
    get chartRows(): StockDetailReportRow[] {
        const days = this.dateRange === '7 Days' ? 7 : this.dateRange === '30 Days' ? 30 : this.dateRange === '3 Months' ? 90 : 0;
        if (!days || !this.rows.length) return this.rows;

        const latestDate = new Date(this.rows[this.rows.length - 1].date).getTime();
        if (!Number.isFinite(latestDate)) return this.rows;

        const cutoff = latestDate - days * 24 * 60 * 60 * 1000;
        return this.rows.filter(row => new Date(row.date).getTime() >= cutoff);
    }
    get chartWidth(): number { return Math.max(560, this.chartRows.length * 48); }
    get chartPath(): string {
        const values = this.chartRows.map(row => this.chartMode === 'value' ? row.balanceAmount : row.balanceQty);
        return this.makeLinePath(values);
    }
    get chartAreaPath(): string {
        const line = this.chartPath;
        return line ? `${line} L ${this.chartWidth - 12} 126 L 12 126 Z` : '';
    }
    chartPoint(index: number, field: 'inwardQty' | 'outwardQty' = 'inwardQty'): { x: number; y: number; height: number } {
        const max = Math.max(1, ...this.chartRows.flatMap(row => [row.inwardQty, row.outwardQty]));
        const value = this.chartRows[index]?.[field] || 0;
        const height = (value / max) * 92;
        return { x: 12 + index * ((this.chartWidth - 24) / Math.max(1, this.chartRows.length - 1)), y: 126 - height, height };
    }
    chartX(index: number): number { return 12 + index * ((this.chartWidth - 24) / Math.max(1, this.chartRows.length - 1)); }
    formatNumber(value: number): string { return Number(value || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    goBack(): void { this.router.navigate(['/app/main/reports/stock-report']); }

    private makeLinePath(values: number[]): string {
        if (!values.length) return '';
        const max = Math.max(1, ...values);
        const min = Math.min(0, ...values);
        const range = Math.max(1, max - min);
        return values.map((value, index) => {
            const x = 12 + index * ((this.chartWidth - 24) / Math.max(1, values.length - 1));
            const y = 126 - ((value - min) / range) * 100;
            return `${index ? 'L' : 'M'} ${x} ${y}`;
        }).join(' ');
    }
}
