import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AccountGroupReportList, AccountGroupReportServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';

@Component({
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
    selector: 'appAccountGroupReport',
    templateUrl: './accountGroupReport.component.html',
    styleUrls: ['./accountGroupReport.component.css'],
    animations: [appModuleAnimation],
})
export class AccountGroupsReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    tableRows: AccountGroupReportList[] = [];
    tableData: AccountGroupReportList[] = [];
    pinnedBottomRowData: any[] = [];
    totalItems = 0;
    totalCredits = 0;
    totalDebits = 0;
    totalOpening = 0;
    totalBalance = 0;
    loading = false;
    filtersExpanded = true;
    advancedFiltersAreShown = false;
    rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    groupDefaultExpanded = 1;
    form: FormGroup;
    myForm: FormGroup;

    readonly defaultColDef: ColDef = {
        cellStyle: { fontSize: '13px' },
        sortable: true,
        // Restaurant startup hides these globally; this report follows AcademicUpgrade.
        suppressHeaderFilterButton: false,
        suppressHeaderMenuButton: false,
    };

    readonly statusBar = {
        statusPanels: [
            { statusPanel: 'agTotalAndFilteredRowCountComponent', align: 'left' },
            { statusPanel: 'agAggregationComponent', align: 'right' },
        ],
    };

    readonly columnDefs: ColDef[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => (params.node.rowPinned ? '' : (params.node.rowIndex ?? 0) + 1),
            width: 80,
        },
        {
            field: 'accountGroupName',
            headerName: this.l('Account Group Name'),
            sortable: true,
            filter: true,
            width: 200,
        },
        ...[
            ['openingDr', 'Opening Dr'],
            ['openingCr', 'Opening Cr'],
            ['debit', 'Debit'],
            ['credit', 'Credit'],
            ['closingDr', 'Closing Dr'],
            ['closingCr', 'Closing Cr'],
        ].map(([field, headerName]) => ({
            field,
            headerName: this.l(headerName),
            sortable: true,
            filter: true,
            width: 140,
            valueFormatter: (params) => this.formatAmount(params.value),
            cellStyle: { textAlign: 'right' },
        })),
    ];

    private readonly destroy$ = new Subject<void>();
    private gridApi?: GridApi;

    constructor(
        injector: Injector,
        private readonly proxy: AccountGroupReportServiceProxy,
        private readonly fb: FormBuilder,
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.createForm();
        this.loadReport();
    }

    createForm(item: any = {}): void {
        this.form = this.fb.group({
            fromMiti: [item.fromMiti || this.fromMiti || this.today],
            toMiti: [item.toMiti || this.toMiti || this.today],
        });
        this.myForm = this.form;
    }

    loadReport(): void {
        const { fromMiti, toMiti } = this.myForm.getRawValue();
        this.loading = true;

        this.proxy
            .getReport(fromMiti, toMiti)
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                }),
                takeUntil(this.destroy$),
            )
            .subscribe({
                next: (result) => {
                    this.tableRows = this.adaptRows(result ?? []);
                    this.tableData = [...this.tableRows];
                    this.gridApi?.setGridOption('rowData', this.tableData);
                    this.updateDisplayedTotals();
                    this.markViewForCheck();
                },
                error: () => {
                    this.tableRows = [];
                    this.tableData = [];
                    this.updateDisplayedTotals();
                    this.notify.error(this.l('FailedToLoadData'));
                },
            });
    }
    loadreport(): void {
        this.loadReport();
    }
    onAdvanceSearch(formValue: any): void {
        this.form.patchValue(formValue || {});
        this.loadReport();
    }
    openAdvanceFilter(isOpen: boolean): void {
        this.advancedFiltersAreShown = isOpen;
    }
    searchFilter(event: any): void {
        const value = event?.target?.value ?? event ?? '';
        const normalized = String(value).trim().toLowerCase();
        this.tableData = normalized
            ? this.tableRows.filter((row) => row.accountGroupName?.toLowerCase().includes(normalized))
            : [...this.tableRows];
        this.gridApi?.setGridOption('rowData', this.tableData);
        this.updateDisplayedTotals();
    }

    onGridReady(event: GridReadyEvent): void {
        this.gridApi = event.api;
        this.updateDisplayedTotals();
        setTimeout(() => event.api.sizeColumnsToFit());
    }

    onFilterChanged(): void {
        this.updateDisplayedTotals();
    }

    searchValueOnApiCall(value: string): void {
        this.gridApi?.setGridOption('quickFilterText', value?.trim() ?? '');
        this.updateDisplayedTotals();
    }

    exportExcel(): void {
        this.gridApi?.exportDataAsExcel({
            fileName: `account-group-report-${this.myForm.value.fromMiti}-${this.myForm.value.toMiti}.xlsx`,
            sheetName: 'Account Groups',
        });
    }

    toggleFilters(): void {
        this.filtersExpanded = !this.filtersExpanded;
    }

    formatAmount(value: unknown): string {
        const amount = Number(value) || 0;
        return amount.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    formatSignedAmount(value: number): string {
        if (!value) {
            return '0.00';
        }

        return `${this.formatAmount(Math.abs(value))} ${value < 0 ? 'Cr' : 'Dr'}`;
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    private updateDisplayedTotals(): void {
        const rows: any[] = [];

        if (this.gridApi) {
            this.gridApi.forEachNodeAfterFilterAndSort((node) => {
                if (node.data && !node.rowPinned) {
                    rows.push(node.data);
                }
            });
        } else {
            rows.push(...this.tableData);
        }

        this.totalItems = rows.length;
        const totalOpeningDr = rows.reduce((sum, row) => sum + (Number(row.openingDr) || 0), 0);
        const totalOpeningCr = rows.reduce((sum, row) => sum + (Number(row.openingCr) || 0), 0);
        this.totalDebits = rows.reduce((sum, row) => sum + (Number(row.debit) || 0), 0);
        this.totalCredits = rows.reduce((sum, row) => sum + (Number(row.credit) || 0), 0);
        const totalClosingDr = rows.reduce((sum, row) => sum + (Number(row.closingDr) || 0), 0);
        const totalClosingCr = rows.reduce((sum, row) => sum + (Number(row.closingCr) || 0), 0);
        this.totalOpening = totalOpeningDr - totalOpeningCr;
        this.totalBalance = totalClosingDr - totalClosingCr;
        this.pinnedBottomRowData = [
            {
                accountGroupName: this.l('Total'),
                openingDr: totalOpeningDr,
                openingCr: totalOpeningCr,
                debit: this.totalDebits,
                credit: this.totalCredits,
                closingDr: totalClosingDr,
                closingCr: totalClosingCr,
            },
        ];
        this.gridApi?.setGridOption('pinnedBottomRowData', this.pinnedBottomRowData);
    }

    private parseSignedAmount(value: unknown): number {
        if (typeof value === 'number') {
            return Number.isFinite(value) ? value : 0;
        }

        const text = String(value ?? '').trim();
        const amount = Number(text.replace(/[^0-9.-]/g, '')) || 0;
        return /\bcr\b/i.test(text) ? -Math.abs(amount) : amount;
    }
    private adaptRows(rows: AccountGroupReportList[]): any[] {
        return rows.map((row) => {
            const opening = this.parseSignedAmount(row.opening);
            const closing = this.parseSignedAmount(row.balance);
            return {
                ...row,
                openingDr: opening > 0 ? opening : 0,
                openingCr: opening < 0 ? Math.abs(opening) : 0,
                closingDr: closing > 0 ? closing : 0,
                closingCr: closing < 0 ? Math.abs(closing) : 0,
            };
        });
    }
}
