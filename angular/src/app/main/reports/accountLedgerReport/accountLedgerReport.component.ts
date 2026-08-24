// account-ledger-report.component.ts
import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ColDef, ColGroupDef, GridApi, GridOptions, GridReadyEvent } from 'ag-grid-community';
import { AppComponentBase } from '@shared/common/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
    AccountLedgerReportList,
    AccountLedgerReportServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
// import { FileDownloadService } from '@shared/utils/file-download.service';

@Component({
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
    selector: 'appaccountledgerreport',
    templateUrl: './accountLedgerReport.component.html',
    styleUrls: ['./accountLedgerReport.component.css'],
    animations: [appModuleAnimation],
})
export class AccountLedgerReportComponent extends AppComponentBase implements OnInit {
    private gridApi!: GridApi;
    myForm: FormGroup;
    filterText = '';
    advancedFiltersAreShown = false;
    rowData: AccountLedgerReportList[] = [];
    totalRecords = 0;

    public gridOptions: GridOptions = {
        defaultColDef: {
            resizable: true,
            minWidth: 100,
            maxWidth: 300,
            // Restaurant startup hides these globally; this report follows AcademicUpgrade.
            suppressHeaderFilterButton: false,
            suppressHeaderMenuButton: false,
        },
        headerHeight: 32,
        groupHeaderHeight: 32,
        rowHeight: 24,
        animateRows: true,
        rowSelection: {
            mode: 'multiRow',
            groupSelects: 'descendants',
            enableClickSelection: false,
        },
        pagination: false,
        pinnedBottomRowData: [],
        onGridReady: this.onGridReady.bind(this),
        suppressHorizontalScroll: false,
    };

    public columnDefs: (ColDef | ColGroupDef)[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => params.node.rowPinned ? '' : params.node.rowIndex + 1,
            width: 80,
            sortable: false,
            filter: false,
            enableRowGroup: false,
        },
        {
            field: 'groupName',
            headerName: this.l('Group Name'),
            sortable: true,
            filter: true,
            enableRowGroup: true,
            flex: 5,
            minWidth: 160,
            headerClass: 'light-blue-header'
        },
        {
            field: 'ledgerName',
            headerName: this.l('Ledger Name'),
            sortable: true,
            filter: true,
            enableRowGroup: true,
            flex: 5,
            minWidth: 160,
            headerClass: 'light-blue-header'
        },
        {
            headerName: this.l('OPENING'),
            children: [{
                field: 'openingDr',
                headerName: this.l('DEBIT'),
                sortable: true,
                filter: false,
                enableRowGroup: true,
                flex: 3,
                valueFormatter: AccountLedgerReportComponent.numberFormatter,
            },
            {
                field: 'openingCr',
                headerName: this.l('CREDIT'),
                sortable: true,
                filter: false,
                enableRowGroup: true,
                flex: 3,
                valueFormatter: AccountLedgerReportComponent.numberFormatter,
            }
            ]
        },
        {
            headerName: this.l('TRANSACTION'),
            children: [{
                field: 'debit',
                headerName: this.l('DEBIT'),
                sortable: true,
                filter: false,
                enableRowGroup: true,
                flex: 3,
                valueFormatter: AccountLedgerReportComponent.numberFormatter,
            },
            {
                field: 'credit',
                headerName: this.l('CREDIT'),
                sortable: true,
                filter: false,
                enableRowGroup: true,
                flex: 3,
                valueFormatter: AccountLedgerReportComponent.numberFormatter,
            }
            ]
        },
        {
            headerName: this.l('CLOSING'),
            children: [{
                field: 'balanceDr',
                headerName: this.l('DEBIT'),
                sortable: true,
                filter: false,
                enableRowGroup: true,
                flex: 3,
                valueFormatter: AccountLedgerReportComponent.numberFormatter,
            },
            {
                field: 'balanceCr',
                headerName: this.l('CREDIT'),
                sortable: true,
                filter: false,
                enableRowGroup: true,
                flex: 3,
                valueFormatter: AccountLedgerReportComponent.numberFormatter,
            }
            ]
        }
    ];
    allAccountGroups: UniversalDropdownDto[];

    constructor(
        injector: Injector,
        private _router: Router,
        private _proxy: AccountLedgerReportServiceProxy,
        private _fb: FormBuilder,
        // private _fileDownloadService: FileDownloadService,
        private _route: ActivatedRoute
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.createForm();
        this.getAllAccountGroup();
        this.setFinancialYear();
    }

    createForm() {
        this.myForm = this._fb.group({
            groupId: [this.emptyGuId],
            isZeroBalance: [true],
            fromMiti: [''],
            toMiti: [''],
        });
    }

    // Format numbers with commas and two decimal places
    static numberFormatter(params: any) {
        if (params.value == null) {
            return '';
        }
        return Number(params.value).toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }


    getAllAccountGroup() {
        const groupId = this._route.snapshot.queryParamMap.get('id');
        this._proxy.getAllAccountGroupForTableDropdown().subscribe(data => {
            const allGroupOption = {
                id: this.emptyGuId,
                displayName: '- Select All -',
            } as any;
            this.allAccountGroups = [allGroupOption, ...(data ?? [])];
            if (groupId && groupId != null) {
                this.myForm.get('groupId').setValue(groupId);
                this.loadreport();
            }
            else {
                this.myForm.get('groupId').setValue(this.emptyGuId);
            }
        });
    }

    setFinancialYear() {
        this._proxy.getFinancialYears().subscribe(result => {
            this.myForm.patchValue({
                fromMiti: result.fromMiti,
                toMiti: result.toMiti
            }, { emitEvent: false });
            this.loadreport();
        })
    }

    toggleVisibility(e) {
        this.myForm.get('isZeroBalance').setValue(e.target.checked);
        this.loadreport();
    } onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.gridApi.addEventListener('filterChanged', () => {
            this.calculateTotals();
        });

        if (this.rowData?.length > 0) {
            this.calculateTotals();
        }
    }

    enableGrouping() {
        if (this.gridApi) {
            // Group by groupName field
            this.gridApi.applyColumnState({
                state: [
                    {
                        colId: 'groupName',
                        rowGroup: true,
                        hide: true
                    }
                ]
            });
        }
    }

    toggleGrouping() {
        if (this.gridApi) {
            const currentState = this.gridApi.getColumnState();
            const groupNameColumn = currentState.find(col => col.colId === 'groupName');

            if (groupNameColumn && groupNameColumn.rowGroup) {
                // Disable grouping
                this.gridApi.applyColumnState({
                    state: [
                        {
                            colId: 'groupName',
                            rowGroup: false,
                            hide: false
                        }
                    ]
                });
            } else {
                // Enable grouping
                this.enableGrouping();
            }
        }
    } calculateTotals(): void {
        if (!this.gridApi) {
            return;
        }

        const displayedRows = [];
        this.gridApi.forEachNodeAfterFilter(node => {
            // Only include leaf nodes (actual data, not group nodes)
            if (!node.group && node.data) {
                displayedRows.push(node.data);
            }
        });

        if (!displayedRows.length) {
            this.gridOptions.pinnedBottomRowData = [];
            this.gridApi.setGridOption('pinnedBottomRowData', []);
            return;
        }

        const totals = {
            groupName: '',
            ledgerName: 'Total',
            openingDr: this.sumFilteredField('openingDr', displayedRows),
            openingCr: this.sumFilteredField('openingCr', displayedRows),
            debit: this.sumFilteredField('debit', displayedRows),
            credit: this.sumFilteredField('credit', displayedRows),
            balanceDr: this.sumFilteredField('balanceDr', displayedRows),
            balanceCr: this.sumFilteredField('balanceCr', displayedRows),
        };

        this.gridOptions.pinnedBottomRowData = [totals];
        this.gridApi.setGridOption('pinnedBottomRowData', [totals]);
    }

    sumFilteredField(fieldName: string, rows: any[]): number {
        return rows.reduce((sum, row) => {
            const value = row[fieldName] || 0;
            return sum + (typeof value === 'number' ? value : 0);
        }, 0);
    }

    onCellDoubleClicked(params) {
        if (!params?.data?.accountLedgerId || params.node?.rowPinned) {
            return;
        }
        localStorage.setItem('accountledgerwise', JSON.stringify(this.myForm.value));
        this._router.navigate(['/app/main/reports/account-wise', params.data.accountLedgerId]);
    }

    loadreport() {
        const { fromMiti, toMiti, groupId, isZeroBalance } = this.myForm.value;
        this._proxy
            .getReport(groupId, isZeroBalance, fromMiti, toMiti)
            .subscribe((result) => {
                this.rowData = this.setGridRowData(this.gridApi, result);
                this.totalRecords = this.rowData.length;
                if (this.gridApi) {
                    setTimeout(() => this.calculateTotals(), 100);
                }
            });
    }

    searchValueOnApiCall(value: string) {
        this.filterText = value;
        if (this.gridApi) {
            this.gridApi.setGridOption('quickFilterText', value);
            this.calculateTotals();
        }
    }

    expandAllGroups() {
        if (this.gridApi) {
            this.gridApi.expandAll();
        }
    }

    collapseAllGroups() {
        if (this.gridApi) {
            this.gridApi.collapseAll();
        }
    }

    exportGroupedData() {
        if (this.gridApi) {
            this.gridApi.exportDataAsCsv({
                fileName: `Account_Ledger_Report_Grouped_${new Date().toISOString().split('T')[0]}.csv`,
                processCellCallback: (params) => {
                    if (params.column.getColId() === 'ag-Grid-AutoColumn') {
                        return params.value;
                    }
                    return params.value;
                }
            });
        }
    } groupByField(fieldName: string) {
        if (this.gridApi) {
            if (!fieldName || fieldName === '') {
                // Clear all grouping
                this.gridApi.applyColumnState({
                    state: this.gridApi.getColumnState().map(col => ({
                        colId: col.colId,
                        rowGroup: false,
                        hide: false
                    }))
                });
            } else {
                // First clear all existing row groups
                this.gridApi.applyColumnState({
                    state: this.gridApi.getColumnState().map(col => ({
                        colId: col.colId,
                        rowGroup: false,
                        hide: col.colId === fieldName ? true : false
                    }))
                });

                // Then apply the new grouping
                this.gridApi.applyColumnState({
                    state: [
                        {
                            colId: fieldName,
                            rowGroup: true,
                            hide: true
                        }
                    ]
                });
            }
        }
    }

    onRowGroupChanged() {
        // Recalculate totals when grouping changes
        setTimeout(() => {
            this.calculateTotals();
        }, 100);
    }
}
