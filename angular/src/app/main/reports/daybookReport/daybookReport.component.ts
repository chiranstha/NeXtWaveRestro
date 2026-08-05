import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef, GridApi, GridOptions, GridReadyEvent } from 'ag-grid-community';
import { PDFDocument, PDFFont, PDFPage, rgb, StandardFonts } from 'pdf-lib';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import {
    BookReportResultDto,
    BookReportRowDto,
    BookReportServiceProxy,
    BookReportSummaryDto,
    BookReportVoucherTypeDto,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';

interface DaySummary {
    date: string;
    miti: string;
    voucherCount: number;
    lineCount: number;
    debit: number;
    credit: number;
    balance: number;
}

interface PdfColumn {
    header: string;
    width: number;
    align?: 'left' | 'right';
    value: (row: BookReportRowDto) => string;
}

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-daybook-report',
    templateUrl: './daybookReport.component.html',
    styleUrls: ['./daybookReport.component.css'],
    providers: [BookReportServiceProxy],
})
export class DaybookReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    private destroy$ = new Subject<void>();
    private gridApi!: GridApi;

    myForm: FormGroup;
    filterText = '';
    loading = false;
    advancedFiltersAreShown = true;
    errorMessage = '';
    rowData: BookReportRowDto[] = [];
    ledgers: UniversalDropdownDto[] = [];
    voucherTypes: BookReportVoucherTypeDto[] = [];
    summary: BookReportSummaryDto = this.emptySummary();
    daySummaries: DaySummary[] = [];

    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 100,
    };

    gridOptions: GridOptions = {
        animateRows: true,
        headerHeight: 34,
        rowHeight: 30,
        pagination: true,
        paginationPageSize: 100,
        suppressHorizontalScroll: false,
        rowClassRules: {
            'daybook-debit-row': (params) => Number(params.data?.debit) > 0,
            'daybook-credit-row': (params) => Number(params.data?.credit) > 0,
        },
        pinnedBottomRowData: [],
    };

    columnDefs: ColDef[] = [
        { headerName: 'S.N', field: 'sn', width: 82, type: 'rightAligned' },
        { headerName: 'Date', field: 'date', valueFormatter: (params) => this.formatDate(params.value), width: 120 },
        { headerName: 'Miti', field: 'dateMiti', width: 120 },
        { headerName: 'Voucher Type', field: 'voucherType', minWidth: 150 },
        { headerName: 'Voucher No', field: 'voucherNo', minWidth: 135 },
        { headerName: 'Ledger', field: 'ledgerName', minWidth: 220, flex: 1 },
        { headerName: 'Group', field: 'groupName', minWidth: 170 },
        this.amountColumn('Debit', 'debit'),
        this.amountColumn('Credit', 'credit'),
        this.amountColumn('Net', 'difference'),
        { headerName: 'Remarks', field: 'remarks', minWidth: 180, flex: 1 },
    ];

    constructor(
        injector: Injector,
        private _fb: FormBuilder,
        private _daybookProxy: BookReportServiceProxy,
        private _fileDownloadService: FileDownloadService
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.createForm();
        this.loadLookups();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    createForm(): void {
        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
            ledgerId: [null],
            voucherTypeId: [null],
        });
    }

    loadLookups(): void {
        this.loading = true;
        this._daybookProxy
            .getLookups()
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                }),
                takeUntil(this.destroy$)
            )
            .subscribe({
                next: (result) => {
                    this.ledgers = result?.ledgers ?? [];
                    this.voucherTypes = result?.voucherTypes ?? [];
                    if (result?.financialYear) {
                        this.myForm.patchValue({
                            fromMiti: result.financialYear.fromMiti,
                            toMiti: result.financialYear.toMiti,
                        });
                    }
                    this.loadReport();
                },
                error: () => {
                    this.errorMessage = 'Could not load Daybook filters.';
                },
            });
    }

    loadReport(): void {
        if (!this.myForm) {
            return;
        }

        const { fromMiti, toMiti, ledgerId, voucherTypeId } = this.myForm.value;
        this.loading = true;
        this.errorMessage = '';

        this._daybookProxy
            .getReport('day-book', fromMiti, toMiti, this.cleanId(ledgerId), this.cleanId(voucherTypeId))
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                }),
                takeUntil(this.destroy$)
            )
            .subscribe({
                next: (result: BookReportResultDto) => {
                    this.summary = result?.summary ?? this.emptySummary();
                    this.rowData = this.setGridRowData(this.gridApi, result?.rows ?? []);
                    this.totalRecords = this.rowData.length;
                    this.daySummaries = this.buildDaySummaries(this.rowData);
                    this.updatePinnedTotals();
                },
                error: () => {
                    this.rowData = this.setGridRowData(this.gridApi, []);
                    this.daySummaries = [];
                    this.summary = this.emptySummary();
                    this.updatePinnedTotals();
                    this.errorMessage = 'Could not load Daybook report.';
                },
            });
    }

    exportToExcel(): void {
        const { fromMiti, toMiti, ledgerId, voucherTypeId } = this.myForm.value;
        this.loading = true;

        this._daybookProxy
            .createBookReportToExcel('day-book', fromMiti, toMiti, this.cleanId(ledgerId), this.cleanId(voucherTypeId))
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                }),
                takeUntil(this.destroy$)
            )
            .subscribe({
                next: (result) => this._fileDownloadService.downloadTempFile(result),
                error: () => {
                    this.errorMessage = 'Could not export Daybook Excel.';
                },
            });
    }

    async exportToPdf(): Promise<void> {
        const rows = this.getVisibleRows();
        if (!rows.length) {
            this.errorMessage = 'No Daybook rows available to export.';
            return;
        }

        this.loading = true;
        this.errorMessage = '';
        this.markViewForCheck();

        try {
            const bytes = await this.createPdfBytes(rows);
            this.downloadPdf(bytes, `daybook-${this.myForm.get('fromMiti')?.value || 'from'}-${this.myForm.get('toMiti')?.value || 'to'}.pdf`);
        } finally {
            this.loading = false;
            this.markViewForCheck();
        }
    }

    searchValueOnApiCall(value: string): void {
        this.filterText = value;
        this.gridApi?.setGridOption('quickFilterText', value);
        this.updatePinnedTotals();
    }

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.gridApi.setGridOption('rowData', this.rowData);
        this.updatePinnedTotals();
        this.gridApi.addEventListener('filterChanged', () => this.updatePinnedTotals());
    }

    get voucherCount(): number {
        return new Set((this.rowData ?? []).map((row) => `${row.voucherType || ''}-${row.voucherNo || ''}`).filter(Boolean)).size;
    }

    get netSide(): string {
        const difference = this.summary?.difference ?? 0;
        if (difference > 0) {
            return 'Debit';
        }

        if (difference < 0) {
            return 'Credit';
        }

        return 'Balanced';
    }

    formatAmount(value: number): string {
        return value === null || value === undefined
            ? ''
            : Number(value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    private amountColumn(headerName: string, field: keyof BookReportRowDto): ColDef {
        return {
            headerName,
            field: field as string,
            minWidth: 125,
            type: 'rightAligned',
            valueFormatter: (params) => this.formatAmount(params.value),
        };
    }

    private cleanId(value: string): string | undefined {
        return !value || value === this.emptyGuId ? undefined : value;
    }

    private updatePinnedTotals(): void {
        if (!this.gridApi) {
            return;
        }

        const rows = this.getVisibleRows();
        if (!rows.length) {
            this.gridApi.setGridOption('pinnedBottomRowData', []);
            return;
        }

        this.gridApi.setGridOption('pinnedBottomRowData', [
            {
                voucherNo: 'Grand Total',
                ledgerName: 'Grand Total',
                debit: this.sumRows(rows, 'debit'),
                credit: this.sumRows(rows, 'credit'),
                difference: this.sumRows(rows, 'difference'),
            },
        ]);
    }

    private sumRows(rows: BookReportRowDto[], field: keyof BookReportRowDto): number {
        return rows.reduce((sum, row) => sum + (Number(row[field]) || 0), 0);
    }

    private buildDaySummaries(rows: BookReportRowDto[]): DaySummary[] {
        const map = new Map<string, DaySummary>();

        rows.forEach((row) => {
            const key = row.dateMiti || this.formatDate(row.date);
            const existing = map.get(key) ?? {
                date: this.formatDate(row.date),
                miti: row.dateMiti || '',
                voucherCount: 0,
                lineCount: 0,
                debit: 0,
                credit: 0,
                balance: 0,
            };

            existing.lineCount += 1;
            existing.debit += Number(row.debit) || 0;
            existing.credit += Number(row.credit) || 0;
            existing.balance = existing.debit - existing.credit;
            map.set(key, existing);
        });

        map.forEach((summary, key) => {
            summary.voucherCount = new Set(
                rows
                    .filter((row) => (row.dateMiti || this.formatDate(row.date)) === key)
                    .map((row) => `${row.voucherType || ''}-${row.voucherNo || ''}`)
            ).size;
        });

        return Array.from(map.values()).sort((a, b) => (a.miti || a.date).localeCompare(b.miti || b.date));
    }

    private getVisibleRows(): BookReportRowDto[] {
        const rows: BookReportRowDto[] = [];
        if (this.gridApi) {
            this.gridApi.forEachNodeAfterFilterAndSort((node) => {
                if (node.data) {
                    rows.push(node.data);
                }
            });
        }

        return rows.length ? rows : [...(this.rowData ?? [])];
    }

    private async createPdfBytes(rows: BookReportRowDto[]): Promise<Uint8Array> {
        const pdfDoc = await PDFDocument.create();
        const font = await pdfDoc.embedFont(StandardFonts.Helvetica);
        const boldFont = await pdfDoc.embedFont(StandardFonts.HelveticaBold);
        const pageWidth = 841.89;
        const pageHeight = 595.28;
        const margin = 24;
        const rowHeight = 17;
        const columns = this.getPdfColumns();
        const tableWidth = columns.reduce((sum, column) => sum + column.width, 0);
        let page = pdfDoc.addPage([pageWidth, pageHeight]);
        let y = this.drawPdfHeader(page, font, boldFont, pageWidth, pageHeight, margin);

        y = this.drawPdfTableHeader(page, columns, margin, y, tableWidth, boldFont);
        rows.forEach((row, index) => {
            if (y < margin + rowHeight + 18) {
                page = pdfDoc.addPage([pageWidth, pageHeight]);
                y = pageHeight - margin;
                y = this.drawPdfTableHeader(page, columns, margin, y, tableWidth, boldFont);
            }

            this.drawPdfRow(page, columns, row, index, margin, y, rowHeight, font);
            y -= rowHeight;
        });

        this.drawPdfFooterRows(pdfDoc, font, pageWidth, margin);
        return pdfDoc.save();
    }

    private drawPdfHeader(
        page: PDFPage,
        font: PDFFont,
        boldFont: PDFFont,
        pageWidth: number,
        pageHeight: number,
        margin: number
    ): number {
        const fromMiti = this.myForm.get('fromMiti')?.value || '';
        const toMiti = this.myForm.get('toMiti')?.value || '';
        let y = pageHeight - margin;

        page.drawText('Daybook Report', { x: margin, y, size: 16, font: boldFont, color: rgb(0.07, 0.12, 0.22) });
        page.drawText(`Date: ${fromMiti} - ${toMiti}`, {
            x: pageWidth - margin - 190,
            y: y + 2,
            size: 9,
            font,
            color: rgb(0.35, 0.39, 0.46),
        });

        y -= 35;
        const metrics = [
            ['Vouchers', `${this.voucherCount}`],
            ['Lines', `${this.summary.totalRows || 0}`],
            ['Debit', this.formatAmount(this.summary.totalDebit)],
            ['Credit', this.formatAmount(this.summary.totalCredit)],
            ['Net', this.formatAmount(this.summary.difference)],
        ];
        const width = (pageWidth - margin * 2 - 32) / metrics.length;

        metrics.forEach(([label, value], index) => {
            const x = margin + index * (width + 8);
            page.drawRectangle({
                x,
                y: y - 30,
                width,
                height: 28,
                color: rgb(0.97, 0.98, 0.99),
                borderColor: rgb(0.84, 0.87, 0.91),
                borderWidth: 0.5,
            });
            page.drawText(label, { x: x + 7, y: y - 12, size: 7, font: boldFont, color: rgb(0.39, 0.44, 0.52) });
            page.drawText(this.ellipsize(value, boldFont, 9, width - 14), {
                x: x + 7,
                y: y - 24,
                size: 9,
                font: boldFont,
                color: rgb(0.08, 0.13, 0.2),
            });
        });

        return y - 44;
    }

    private drawPdfTableHeader(
        page: PDFPage,
        columns: PdfColumn[],
        x: number,
        y: number,
        tableWidth: number,
        boldFont: PDFFont
    ): number {
        page.drawRectangle({
            x,
            y: y - 16,
            width: tableWidth,
            height: 16,
            color: rgb(0.91, 0.94, 0.98),
            borderColor: rgb(0.74, 0.8, 0.87),
            borderWidth: 0.5,
        });

        let currentX = x;
        columns.forEach((column) => {
            page.drawText(column.header, {
                x: currentX + 4,
                y: y - 11,
                size: 7,
                font: boldFont,
                color: rgb(0.17, 0.23, 0.32),
            });
            currentX += column.width;
        });

        return y - 16;
    }

    private drawPdfRow(
        page: PDFPage,
        columns: PdfColumn[],
        row: BookReportRowDto,
        index: number,
        x: number,
        y: number,
        rowHeight: number,
        font: PDFFont
    ): void {
        const tableWidth = columns.reduce((sum, column) => sum + column.width, 0);
        page.drawRectangle({
            x,
            y: y - rowHeight,
            width: tableWidth,
            height: rowHeight,
            color: index % 2 === 0 ? rgb(1, 1, 1) : rgb(0.985, 0.99, 0.995),
            borderColor: rgb(0.9, 0.92, 0.95),
            borderWidth: 0.35,
        });

        let currentX = x;
        columns.forEach((column) => {
            const value = this.ellipsize(column.value(row), font, 7, column.width - 8);
            const textWidth = font.widthOfTextAtSize(value, 7);
            page.drawText(value, {
                x: column.align === 'right' ? currentX + column.width - textWidth - 4 : currentX + 4,
                y: y - 11,
                size: 7,
                font,
                color: rgb(0.13, 0.17, 0.24),
            });
            currentX += column.width;
        });
    }

    private drawPdfFooterRows(pdfDoc: PDFDocument, font: PDFFont, pageWidth: number, margin: number): void {
        const pages = pdfDoc.getPages();
        pages.forEach((page, index) => {
            page.drawText(`Page ${index + 1} of ${pages.length}`, {
                x: pageWidth - margin - 58,
                y: 12,
                size: 7,
                font,
                color: rgb(0.45, 0.49, 0.56),
            });
        });
    }

    private getPdfColumns(): PdfColumn[] {
        return [
            { header: 'S.N', width: 32, align: 'right', value: (row) => this.valueOrEmpty(row.sn) },
            { header: 'Date', width: 58, value: (row) => this.formatDate(row.date) },
            { header: 'Miti', width: 58, value: (row) => row.dateMiti || '' },
            { header: 'Voucher', width: 78, value: (row) => row.voucherType || '' },
            { header: 'No.', width: 62, value: (row) => row.voucherNo || '' },
            { header: 'Ledger', width: 140, value: (row) => row.ledgerName || '' },
            { header: 'Group', width: 95, value: (row) => row.groupName || '' },
            { header: 'Debit', width: 64, align: 'right', value: (row) => this.formatAmount(row.debit) },
            { header: 'Credit', width: 64, align: 'right', value: (row) => this.formatAmount(row.credit) },
            { header: 'Remarks', width: 98, value: (row) => row.remarks || '' },
        ];
    }

    private downloadPdf(bytes: Uint8Array, fileName: string): void {
        const blob = new Blob([bytes as unknown as BlobPart], { type: 'application/pdf' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
    }

    private valueOrEmpty(value: number | string): string {
        return value === null || value === undefined ? '' : `${value}`;
    }

    private ellipsize(value: string, font: PDFFont, fontSize: number, maxWidth: number): string {
        const text = value ?? '';
        if (font.widthOfTextAtSize(text, fontSize) <= maxWidth) {
            return text;
        }

        let output = text;
        while (output.length > 0 && font.widthOfTextAtSize(`${output}...`, fontSize) > maxWidth) {
            output = output.slice(0, -1);
        }

        return `${output}...`;
    }

    private formatDate(value: any): string {
        if (!value) {
            return '';
        }

        if (typeof value === 'string') {
            return value.substring(0, 10);
        }

        if (typeof value.toFormat === 'function') {
            return value.toFormat('yyyy-MM-dd');
        }

        if (value instanceof Date) {
            return value.toISOString().substring(0, 10);
        }

        return `${value}`.substring(0, 10);
    }

    private emptySummary(): BookReportSummaryDto {
        const summary = new BookReportSummaryDto();
        summary.totalRows = 0;
        summary.openingBalance = 0;
        summary.totalDebit = 0;
        summary.totalCredit = 0;
        summary.totalIn = 0;
        summary.totalOut = 0;
        summary.difference = 0;
        summary.closingBalance = 0;
        return summary;
    }
}
