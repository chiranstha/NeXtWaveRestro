import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef, GridApi, GridOptions, GridReadyEvent } from 'ag-grid-community';
import { PDFDocument, PDFFont, PDFPage, rgb, StandardFonts } from 'pdf-lib';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import {
    BookReportApiService,
    BookReportResultDto,
    BookReportRowDto,
    BookReportSummaryDto,
    BookReportVoucherTypeDto,
} from './book-report-api.service';
import { UniversalDropdownDto } from '@shared/service-proxies/service-proxies';

interface BookReportOption {
    id: string;
    name: string;
    icon: string;
    requiresLedger?: boolean;
    showLedger?: boolean;
    showVoucher?: boolean;
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
    selector: 'app-book-report',
    templateUrl: './bookReport.component.html',
    styleUrls: ['./bookReport.component.css'],
    providers: [BookReportApiService],
})
export class BookReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    private destroy$ = new Subject<void>();
    private gridApi!: GridApi;

    myForm: FormGroup;
    filterText = '';
    loading = false;
    advancedFiltersAreShown = true;
    inlineMessage = '';
    rowData: BookReportRowDto[] = [];
    ledgers: UniversalDropdownDto[] = [];
    voucherTypes: BookReportVoucherTypeDto[] = [];
    reportResult: BookReportResultDto;
    summary: BookReportSummaryDto = this.emptySummary();

    bookReports: BookReportOption[] = [
        { id: 'day-book', name: 'Daybook Report', icon: 'fa-duotone fa-book-open', showLedger: true, showVoucher: true },
        { id: 'cash-book', name: 'Cash Book', icon: 'fa-duotone fa-money-bill-wave', showLedger: true },
        { id: 'bank-book', name: 'Bank Book', icon: 'fa-duotone fa-building-columns', showLedger: true },
        {
            id: 'ledger-statement',
            name: 'Ledger Statement',
            icon: 'fa-duotone fa-memo-circle-info',
            requiresLedger: true,
            showLedger: true,
            showVoucher: true,
        },
        {
            id: 'voucher-register',
            name: 'Voucher Register',
            icon: 'fa-duotone fa-receipt',
            showLedger: true,
            showVoucher: true,
        },
        { id: 'journal', name: 'Journal', icon: 'fa-duotone fa-book', showLedger: true },
        { id: 'receipt', name: 'Receipt', icon: 'fa-duotone fa-file-invoice-dollar', showLedger: true },
        { id: 'payment', name: 'Payment', icon: 'fa-duotone fa-sack-dollar', showLedger: true },
        { id: 'contra', name: 'Contra', icon: 'fa-duotone fa-building-columns', showLedger: true },
        {
            id: 'opening-balance',
            name: 'Opening Balance',
            icon: 'fa-duotone fa-scale-balanced',
            showLedger: true,
            showVoucher: true,
        },
        { id: 'receivable-aging', name: 'Receivable Aging', icon: 'fa-duotone fa-calendar-clock', showLedger: true },
        { id: 'payable-aging', name: 'Payable Aging', icon: 'fa-duotone fa-hourglass-clock', showLedger: true },
        {
            id: 'party-ledger-statement',
            name: 'Party Ledger Statement',
            icon: 'fa-duotone fa-address-book',
            requiresLedger: true,
            showLedger: true,
            showVoucher: true,
        },
        {
            id: 'cash-bank-reconciliation',
            name: 'Cash/Bank Reconciliation',
            icon: 'fa-duotone fa-scale-balanced',
            showLedger: true,
        },
        {
            id: 'voucher-audit',
            name: 'Voucher Audit Report',
            icon: 'fa-duotone fa-shield-check',
            showLedger: true,
            showVoucher: true,
        },
        {
            id: 'daily-collection-payment',
            name: 'Daily Collection & Payment',
            icon: 'fa-duotone fa-calendar-day',
            showLedger: true,
        },
        { id: 'ledger-group-summary', name: 'Ledger Group Summary', icon: 'fa-duotone fa-layer-group' },
    ];

    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 120,
    };

    gridOptions: GridOptions = {
        rowHeight: 28,
        headerHeight: 32,
        animateRows: true,
        pagination: true,
        paginationPageSize: 100,
        suppressHorizontalScroll: false,
    };

    columnDefs: ColDef[] = [];

    constructor(
        injector: Injector,
        private _fb: FormBuilder,
        private _bookReportApi: BookReportApiService,
        private _fileDownloadService: FileDownloadService
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.createForm();
        this.updateColumnDefs();
        this.getLookups();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    createForm(): void {
        this.myForm = this._fb.group({
            reportType: ['day-book'],
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
            ledgerId: [null],
            voucherTypeId: [null],
        });
    }

    getLookups(): void {
        this.loading = true;
        this._bookReportApi
            .getLookups()
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                }),
                takeUntil(this.destroy$)
            )
            .subscribe((result) => {
                this.ledgers = result?.ledgers ?? [];
                this.voucherTypes = result?.voucherTypes ?? [];
                if (result?.financialYear) {
                    this.myForm.patchValue({
                        fromMiti: result.financialYear.fromMiti,
                        toMiti: result.financialYear.toMiti,
                    });
                }
                this.loadReport();
            });
    }

    selectReport(reportType: string): void {
        this.myForm.patchValue({
            reportType,
            voucherTypeId: null,
        });
        this.updateColumnDefs();
        this.loadReport();
    }

    loadReport(): void {
        if (!this.canLoadSelectedReport()) {
            this.rowData = this.setGridRowData(this.gridApi, []);
            this.reportResult = null;
            this.summary = this.emptySummary();
            this.inlineMessage = `Select a ledger to view ${this.selectedReport.name}.`;
            return;
        }

        this.inlineMessage = '';
        this.loading = true;
        const { reportType, fromMiti, toMiti, ledgerId, voucherTypeId } = this.myForm.value;

        this._bookReportApi
            .getReport(reportType, fromMiti, toMiti, this.cleanId(ledgerId), this.cleanId(voucherTypeId))
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                }),
                takeUntil(this.destroy$)
            )
            .subscribe((result) => {
                this.reportResult = result;
                this.summary = result?.summary ?? this.emptySummary();
                this.rowData = this.setGridRowData(this.gridApi, result?.rows ?? []);
                this.totalRecords = this.rowData.length;
                this.updatePinnedTotals();
            });
    }

    exportToExcel(): void {
        if (!this.canLoadSelectedReport()) {
            this.inlineMessage = `Select a ledger to export ${this.selectedReport.name}.`;
            return;
        }

        const { reportType, fromMiti, toMiti, ledgerId, voucherTypeId } = this.myForm.value;
        this.loading = true;
        this._bookReportApi
            .createBookReportToExcel(reportType, fromMiti, toMiti, this.cleanId(ledgerId), this.cleanId(voucherTypeId))
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                }),
                takeUntil(this.destroy$)
            )
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    async exportToPdf(): Promise<void> {
        if (!this.canLoadSelectedReport()) {
            this.inlineMessage = `Select a ledger to export ${this.selectedReport.name}.`;
            return;
        }

        const rows = this.getVisibleRows();
        if (!rows.length) {
            this.inlineMessage = 'No rows available to export.';
            return;
        }

        this.loading = true;
        this.inlineMessage = '';
        this.markViewForCheck();

        try {
            const pdfBytes = await this.createPdfBytes(rows);
            this.downloadPdf(pdfBytes, `${this.slugify(this.reportTitle)}-${this.myForm.get('fromMiti')?.value || 'from'}-${this.myForm.get('toMiti')?.value || 'to'}.pdf`);
        } finally {
            this.loading = false;
            this.markViewForCheck();
        }
    }

    printReport(): void {
        window.print();
    }

    searchValueOnApiCall(value: string): void {
        this.filterText = value;
        this.gridApi?.setGridOption('quickFilterText', value);
    }

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.gridApi.setGridOption('rowData', this.rowData);
        this.updatePinnedTotals();
    }

    get selectedReport(): BookReportOption {
        const reportType = this.myForm?.get('reportType')?.value;
        return this.bookReports.find((x) => x.id === reportType) ?? this.bookReports[0];
    }

    get showLedgerFilter(): boolean {
        return !!this.selectedReport?.showLedger;
    }

    get showVoucherFilter(): boolean {
        return !!this.selectedReport?.showVoucher;
    }

    get reportTitle(): string {
        return this.reportResult?.reportTitle || this.selectedReport?.name || 'Book Report';
    }

    get netMovementLabel(): string {
        const value = this.summary?.difference ?? 0;
        if (value > 0) {
            return 'Debit';
        }

        if (value < 0) {
            return 'Credit';
        }

        return 'Balanced';
    }

    get activeFilterCount(): number {
        let count = 0;
        if (this.cleanId(this.myForm?.get('ledgerId')?.value)) {
            count++;
        }

        if (this.cleanId(this.myForm?.get('voucherTypeId')?.value)) {
            count++;
        }

        if (this.filterText) {
            count++;
        }

        return count;
    }

    private canLoadSelectedReport(): boolean {
        return !this.selectedReport?.requiresLedger || !!this.cleanId(this.myForm?.get('ledgerId')?.value);
    }

    private cleanId(value: string): string | undefined {
        if (!value || value === this.emptyGuId) {
            return undefined;
        }
        return value;
    }

    private updateColumnDefs(): void {
        const reportType = this.myForm?.get('reportType')?.value ?? 'day-book';
        const baseColumns: ColDef[] = [
            { headerName: 'S.N', field: 'sn', width: 82 },
            { headerName: 'Date', field: 'date', valueFormatter: (params) => this.formatDate(params.value), width: 120 },
            { headerName: 'Miti', field: 'dateMiti', width: 120 },
            { headerName: 'Voucher Type', field: 'voucherType', minWidth: 160 },
            { headerName: 'Voucher No', field: 'voucherNo', minWidth: 140 },
        ];

        const ledgerColumn: ColDef = { headerName: 'Ledger', field: 'ledgerName', minWidth: 220, flex: 1 };
        const groupColumn: ColDef = { headerName: 'Group', field: 'groupName', minWidth: 180 };
        const lineColumn: ColDef = { headerName: 'Lines', field: 'lineCount', width: 110, type: 'rightAligned' };
        const debitColumn: ColDef = this.amountColumn('Debit', 'debit');
        const creditColumn: ColDef = this.amountColumn('Credit', 'credit');
        const openingColumn: ColDef = this.amountColumn('Opening', 'openingBalance');
        const inColumn: ColDef = this.amountColumn('In', 'inAmount');
        const outColumn: ColDef = this.amountColumn('Out', 'outAmount');
        const differenceColumn: ColDef = this.amountColumn('Difference', 'difference');
        const closingColumn: ColDef = this.amountColumn('Closing', 'closingBalance');
        const ageColumn: ColDef = { headerName: 'Age Days', field: 'ageDays', width: 120, type: 'rightAligned' };
        const currentColumn: ColDef = this.amountColumn('Current', 'currentAmount');
        const age1To30Column: ColDef = this.amountColumn('1-30', 'age1To30');
        const age31To60Column: ColDef = this.amountColumn('31-60', 'age31To60');
        const age61To90Column: ColDef = this.amountColumn('61-90', 'age61To90');
        const ageAbove90Column: ColDef = this.amountColumn('90+', 'ageAbove90');
        const statusColumn: ColDef = { headerName: 'Status', field: 'status', minWidth: 140 };
        const remarksColumn: ColDef = { headerName: 'Remarks', field: 'remarks', minWidth: 180, flex: 1 };

        if (['voucher-register', 'voucher-audit'].includes(reportType)) {
            this.columnDefs = [...baseColumns, ledgerColumn, lineColumn, debitColumn, creditColumn, differenceColumn, statusColumn, remarksColumn];
            return;
        }

        if (['receivable-aging', 'payable-aging'].includes(reportType)) {
            this.columnDefs = [
                { headerName: 'S.N', field: 'sn', width: 82 },
                ledgerColumn,
                groupColumn,
                { headerName: 'Last Txn Date', field: 'date', valueFormatter: (params) => this.formatDate(params.value), width: 135 },
                { headerName: 'Last Txn Miti', field: 'dateMiti', width: 130 },
                ageColumn,
                currentColumn,
                age1To30Column,
                age31To60Column,
                age61To90Column,
                ageAbove90Column,
                closingColumn,
                statusColumn,
            ];
            return;
        }

        if (['cash-book', 'bank-book', 'ledger-statement', 'party-ledger-statement'].includes(reportType)) {
            this.columnDefs = [
                ...baseColumns,
                ledgerColumn,
                openingColumn,
                inColumn,
                outColumn,
                closingColumn,
                remarksColumn,
            ];
            return;
        }

        if (reportType === 'cash-bank-reconciliation') {
            this.columnDefs = [
                { headerName: 'S.N', field: 'sn', width: 82 },
                ledgerColumn,
                groupColumn,
                openingColumn,
                debitColumn,
                creditColumn,
                closingColumn,
                statusColumn,
                remarksColumn,
            ];
            return;
        }

        if (reportType === 'daily-collection-payment') {
            this.columnDefs = [
                { headerName: 'S.N', field: 'sn', width: 82 },
                { headerName: 'Date', field: 'date', valueFormatter: (params) => this.formatDate(params.value), width: 120 },
                { headerName: 'Miti', field: 'dateMiti', width: 120 },
                { headerName: 'Type', field: 'voucherType', minWidth: 150 },
                { headerName: 'Vouchers', field: 'voucherNo', width: 120, type: 'rightAligned' },
                lineColumn,
                this.amountColumn('Collection', 'inAmount'),
                this.amountColumn('Payment', 'outAmount'),
                differenceColumn,
                remarksColumn,
            ];
            return;
        }

        if (reportType === 'ledger-group-summary') {
            this.columnDefs = [
                { headerName: 'S.N', field: 'sn', width: 82 },
                groupColumn,
                { headerName: 'Ledger Count', field: 'lineCount', width: 130, type: 'rightAligned' },
                openingColumn,
                debitColumn,
                creditColumn,
                closingColumn,
                remarksColumn,
            ];
            return;
        }

        if (reportType === 'opening-balance') {
            this.columnDefs = [ledgerColumn, groupColumn, debitColumn, creditColumn, differenceColumn, remarksColumn];
            return;
        }

        this.columnDefs = [...baseColumns, ledgerColumn, groupColumn, debitColumn, creditColumn, differenceColumn, remarksColumn];
    }

    private amountColumn(headerName: string, field: keyof BookReportRowDto): ColDef {
        return {
            headerName,
            field: field as string,
            minWidth: 130,
            type: 'rightAligned',
            valueFormatter: (params) => this.formatAmount(params.value),
        };
    }

    private updatePinnedTotals(): void {
        if (!this.gridApi || !this.rowData?.length) {
            this.gridApi?.setGridOption('pinnedBottomRowData', []);
            return;
        }

        this.gridApi.setGridOption('pinnedBottomRowData', [
            {
                ledgerName: 'Grand Total',
                voucherNo: 'Grand Total',
                lineCount: this.summary.totalRows,
                openingBalance: this.summary.openingBalance,
                debit: this.summary.totalDebit,
                credit: this.summary.totalCredit,
                inAmount: this.summary.totalIn,
                outAmount: this.summary.totalOut,
                difference: this.summary.difference,
                closingBalance: this.summary.closingBalance,
                currentAmount: this.sumRowField('currentAmount'),
                age1To30: this.sumRowField('age1To30'),
                age31To60: this.sumRowField('age31To60'),
                age61To90: this.sumRowField('age61To90'),
                ageAbove90: this.sumRowField('ageAbove90'),
            },
        ]);
    }

    private sumRowField(field: keyof BookReportRowDto): number {
        return (this.rowData ?? []).reduce((sum, row) => sum + (Number(row[field]) || 0), 0);
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
        const tableRowHeight = 17;
        const columns = this.getPdfColumns();
        const tableWidth = columns.reduce((sum, column) => sum + column.width, 0);
        let page = pdfDoc.addPage([pageWidth, pageHeight]);
        let y = this.drawPdfHeader(page, font, boldFont, margin, pageWidth, pageHeight);

        y = this.drawPdfTableHeader(page, columns, margin, y, tableWidth, boldFont);

        rows.forEach((row, index) => {
            if (y < margin + tableRowHeight + 18) {
                page = pdfDoc.addPage([pageWidth, pageHeight]);
                y = pageHeight - margin;
                y = this.drawPdfTableHeader(page, columns, margin, y, tableWidth, boldFont);
            }

            this.drawPdfRow(page, columns, row, index, margin, y, tableRowHeight, font);
            y -= tableRowHeight;
        });

        if (y < margin + tableRowHeight + 18) {
            page = pdfDoc.addPage([pageWidth, pageHeight]);
            y = pageHeight - margin;
            y = this.drawPdfTableHeader(page, columns, margin, y, tableWidth, boldFont);
        }

        this.drawPdfTotalRow(page, columns, margin, y, tableRowHeight, boldFont);
        this.drawPdfFooters(pdfDoc, font, pageWidth, margin);

        return pdfDoc.save();
    }

    private drawPdfHeader(
        page: PDFPage,
        font: PDFFont,
        boldFont: PDFFont,
        margin: number,
        pageWidth: number,
        pageHeight: number
    ): number {
        const title = this.reportTitle || 'Daybook Report';
        const dateRange = `${this.myForm.get('fromMiti')?.value || ''} - ${this.myForm.get('toMiti')?.value || ''}`;
        let y = pageHeight - margin;

        page.drawText(title, {
            x: margin,
            y: y - 4,
            size: 16,
            font: boldFont,
            color: rgb(0.07, 0.12, 0.22),
        });
        page.drawText(`Date: ${dateRange}`, {
            x: pageWidth - margin - 190,
            y,
            size: 9,
            font,
            color: rgb(0.33, 0.37, 0.44),
        });

        y -= 31;
        const metrics = [
            ['Rows', `${this.summary.totalRows || 0}`],
            ['Debit', this.formatAmount(this.summary.totalDebit)],
            ['Credit', this.formatAmount(this.summary.totalCredit)],
            ['Difference', this.formatAmount(this.summary.difference)],
            ['Closing', this.formatAmount(this.summary.closingBalance)],
        ];
        const boxWidth = (pageWidth - margin * 2 - 32) / metrics.length;

        metrics.forEach(([label, value], index) => {
            const x = margin + index * (boxWidth + 8);
            page.drawRectangle({
                x,
                y: y - 32,
                width: boxWidth,
                height: 28,
                color: rgb(0.97, 0.98, 0.99),
                borderColor: rgb(0.83, 0.86, 0.9),
                borderWidth: 0.5,
            });
            page.drawText(label, {
                x: x + 7,
                y: y - 14,
                size: 7,
                font: boldFont,
                color: rgb(0.41, 0.45, 0.52),
            });
            page.drawText(this.ellipsize(value, font, 9, boxWidth - 14), {
                x: x + 7,
                y: y - 26,
                size: 9,
                font: boldFont,
                color: rgb(0.07, 0.12, 0.22),
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
            color: rgb(0.92, 0.95, 0.98),
            borderColor: rgb(0.77, 0.82, 0.88),
            borderWidth: 0.5,
        });

        let currentX = x;
        columns.forEach((column) => {
            page.drawText(column.header, {
                x: currentX + 4,
                y: y - 11,
                size: 7,
                font: boldFont,
                color: rgb(0.18, 0.25, 0.34),
            });
            currentX += column.width;
        });

        return y - 16;
    }

    private drawPdfRow(
        page: PDFPage,
        columns: PdfColumn[],
        row: BookReportRowDto,
        rowIndex: number,
        x: number,
        y: number,
        rowHeight: number,
        font: PDFFont
    ): void {
        const fill = rowIndex % 2 === 0 ? rgb(1, 1, 1) : rgb(0.985, 0.99, 0.995);
        const totalWidth = columns.reduce((sum, column) => sum + column.width, 0);

        page.drawRectangle({
            x,
            y: y - rowHeight,
            width: totalWidth,
            height: rowHeight,
            color: fill,
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

    private drawPdfTotalRow(
        page: PDFPage,
        columns: PdfColumn[],
        x: number,
        y: number,
        rowHeight: number,
        boldFont: PDFFont
    ): void {
        const totalWidth = columns.reduce((sum, column) => sum + column.width, 0);
        const totalRow: Partial<BookReportRowDto> = {
            ledgerName: 'Grand Total',
            lineCount: this.summary.totalRows,
            debit: this.summary.totalDebit,
            credit: this.summary.totalCredit,
            difference: this.summary.difference,
            closingBalance: this.summary.closingBalance,
        };

        page.drawRectangle({
            x,
            y: y - rowHeight,
            width: totalWidth,
            height: rowHeight,
            color: rgb(0.9, 0.96, 0.94),
            borderColor: rgb(0.56, 0.72, 0.66),
            borderWidth: 0.5,
        });

        let currentX = x;
        columns.forEach((column) => {
            const value = this.ellipsize(column.value(totalRow as BookReportRowDto), boldFont, 7, column.width - 8);
            const textWidth = boldFont.widthOfTextAtSize(value, 7);
            page.drawText(value, {
                x: column.align === 'right' ? currentX + column.width - textWidth - 4 : currentX + 4,
                y: y - 11,
                size: 7,
                font: boldFont,
                color: rgb(0.06, 0.25, 0.18),
            });
            currentX += column.width;
        });
    }

    private drawPdfFooters(pdfDoc: PDFDocument, font: PDFFont, pageWidth: number, margin: number): void {
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
            { header: 'Ledger', width: 130, value: (row) => row.ledgerName || '' },
            { header: 'Group', width: 95, value: (row) => row.groupName || '' },
            { header: 'Debit', width: 64, align: 'right', value: (row) => this.formatAmount(row.debit) },
            { header: 'Credit', width: 64, align: 'right', value: (row) => this.formatAmount(row.credit) },
            { header: 'Diff.', width: 64, align: 'right', value: (row) => this.formatAmount(row.difference) },
            { header: 'Remarks', width: 88, value: (row) => row.remarks || '' },
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

    private ellipsize(value: string, font: PDFFont, fontSize: number, maxWidth: number): string {
        const text = value ?? '';
        if (font.widthOfTextAtSize(text, fontSize) <= maxWidth) {
            return text;
        }

        let trimmed = text;
        while (trimmed.length > 0 && font.widthOfTextAtSize(`${trimmed}...`, fontSize) > maxWidth) {
            trimmed = trimmed.slice(0, -1);
        }

        return `${trimmed}...`;
    }

    private valueOrEmpty(value: number | string): string {
        return value === null || value === undefined ? '' : `${value}`;
    }

    private slugify(value: string): string {
        return (value || 'daybook-report')
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-|-$/g, '');
    }

    private emptySummary(): BookReportSummaryDto {
        return {
            totalRows: 0,
            openingBalance: 0,
            totalDebit: 0,
            totalCredit: 0,
            totalIn: 0,
            totalOut: 0,
            difference: 0,
            closingBalance: 0,
        };
    }

    formatAmount(value: number): string {
        return value === null || value === undefined
            ? ''
            : Number(value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    private formatDate(value: Date | string): string {
        if (!value) {
            return '';
        }

        if (typeof value === 'string') {
            return value.substring(0, 10);
        }

        return value.toISOString().substring(0, 10);
    }
}
