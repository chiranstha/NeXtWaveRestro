import { ChangeDetectionStrategy, Component, HostListener, Injector, OnDestroy, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormControl } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    LedgerwiseMonthlySalesDto,
    MonthlySalesReportServiceProxy,
    ReportingServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { Observable, Subject } from 'rxjs';
import { delay, finalize, takeUntil } from 'rxjs/operators';
import { ColDef, GridApi, GridOptions, GridReadyEvent, ValueFormatterParams } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-ledger-monthly-report',
    templateUrl: './ledgerWiseMonthlySalesReport.component.html',
})
export class LedgerWiseMonthlySalesReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    public destroy$ = new Subject<void>();
    loading = false;
    allAccountGroup$: Observable<UniversalDropdownDto[]>;
    accountGroupId = new FormControl(0);
    advancedFiltersAreShown = false;
    totalShrawan: number;
    totalBhadra: number;
    totalAshoj: number;
    totalFirstQuarter: number;
    totalKartik: number;
    totalMangsir: number;
    totalPoush: number;
    totalSecondQuarter: number;
    totalMagh: number;
    totalFalgun: number;
    totalChaitra: number;
    totalThirdQuarter: number;
    totalBaisakh: number;
    totalJestha: number;
    totalAsadh: number;
    totalYearly: number;
    tempTable: LedgerwiseMonthlySalesDto[] = [];
    ledgerData: LedgerwiseMonthlySalesDto[] = [];

    // AG Grid properties
    gridApi: GridApi<LedgerwiseMonthlySalesDto>;
    gridOptions: GridOptions;
    columnDefs: ColDef[];
    defaultColDef: ColDef;

    constructor(
        injector: Injector,
        private _proxy: MonthlySalesReportServiceProxy,
        private _reportProxy : ReportingServiceProxy
    ) {
        super(injector);
        this.getSetting();
        this.setupGrid();
        this.calculateScreenSize();
    }

    ngOnInit(): void {
        this.loadreport(this.emptyGuId);
        this.getAllProductGroups();
    }

    ngOnDestroy() {
        this.destroy$.next();
        this.destroy$.complete();
    }

    @HostListener('window:resize')
    calculateScreenSize() {
        this.screenHeight = window.innerHeight - 300; // Adjust based on your layout
    }

    // Format currency values
    currencyFormatter(params: ValueFormatterParams) {
        if (params.value == null || params.value === undefined) {
            return '';
        }
        return parseFloat(params.value).toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    setupGrid() {
        this.defaultColDef = {
            sortable: true,
            filter: false,
            resizable: true,
            minWidth: 100,
            flex: 1,
            valueFormatter: this.currencyFormatter.bind(this)
        };

        this.columnDefs = [
            {
                headerName: 'LEDGER',
                field: 'ledgerName',
                minWidth: 200,
                flex: 2,
                pinned: 'left',
                filter: 'agTextColumnFilter',
                valueFormatter: (params => {
                    if (params.value == null) {
                        return '';
                    }
                    return params.value;
                }),
            },
            { headerName: 'SHRAWAN', field: 'details.shrawan', type: 'numericColumn' },
            { headerName: 'BHADRA', field: 'details.bhadra', type: 'numericColumn' },
            { headerName: 'ASWIN', field: 'details.aswin', type: 'numericColumn' },
            {
                headerName: '1st QUARTER',
                field: 'details.firstQuarter',
                type: 'numericColumn',
                cellClass: 'bg-light-warning'
            },
            { headerName: 'KARTIK', field: 'details.kartik', type: 'numericColumn' },
            { headerName: 'MANGSIR', field: 'details.mangsir', type: 'numericColumn' },
            { headerName: 'POUSH', field: 'details.poush', type: 'numericColumn' },
            {
                headerName: '2nd QUARTER',
                field: 'details.secondQuarter',
                type: 'numericColumn',
                cellClass: 'bg-light-warning'
            },
            { headerName: 'MAGH', field: 'details.magh', type: 'numericColumn' },
            { headerName: 'FALGUN', field: 'details.falgun', type: 'numericColumn' },
            { headerName: 'CHAITRA', field: 'details.chaitra', type: 'numericColumn' },
            {
                headerName: '3rd QUARTER',
                field: 'details.thirdQuarter',
                type: 'numericColumn',
                cellClass: 'bg-light-warning'
            },
            { headerName: 'BAISAKH', field: 'details.baisakh', type: 'numericColumn' },
            { headerName: 'JESTHA', field: 'details.jestha', type: 'numericColumn' },
            { headerName: 'ASHAD', field: 'details.asar', type: 'numericColumn' },
            {
                headerName: 'YEARLY',
                field: 'details.yearly',
                type: 'numericColumn',
                cellClass: 'bg-light-success'
            }
        ];

        this.gridOptions = {

            enableCellTextSelection: true,
            rowSelection: { mode: 'multiRow', enableClickSelection: false },
            animateRows: true,
            domLayout: 'normal',
            enableRangeSelection: true,
            rowData: this.ledgerData,
            suppressAggFuncInHeader: true,
            getRowStyle: params => {
                if (params.node.rowIndex % 2 === 0) {
                    return { background: '#f8f9fa' };
                }
                return null;
            },
            statusBar: {
                statusPanels: [
                    { statusPanel: 'agTotalRowCountComponent', align: 'left' },
                    { statusPanel: 'agFilteredRowCountComponent' }
                ]
            }
        };
    }

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        setTimeout(() => {

        }, 300);
    }

    loadreport(id) {
        this.loading = true;
        this._proxy.getAccountwiseReport(id)
            .pipe(
                takeUntil(this.destroy$),
                delay(200),
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                })
            )
            .subscribe({
                next: (result) => {
                    this.tempTable = result ?? [];
                    this.ledgerData = this.setGridRowData(this.gridApi, this.tempTable);
                    this.calculateTotoal();
                },
                error: () => {
                    this.notify.error(this.l('FailedToLoadReport'));
                },
            });
    }

    getAllProductGroups() {
        this.allAccountGroup$ = this._reportProxy.getAllAccountGroupForTableDropdown();
    }

    searchFilter(e) {
        const searchStr = typeof e === 'string' ? e : e?.target?.value;
        if (searchStr) {
            this.ledgerData = this.tempTable.filter(
                (type) => type.ledgerName?.toLowerCase().search(searchStr.toLowerCase()) !== -1
            );
            this.gridApi?.setGridOption('rowData', this.ledgerData);
            this.markViewForCheck();
        } else {
            this.ledgerData = this.tempTable;
            this.gridApi?.setGridOption('rowData', this.ledgerData);
            this.markViewForCheck();
        }
    }

    openAdvanceFilter($event) {
        this.advancedFiltersAreShown = $event === true;
    }

    calculateTotoal() {
        // Reset totals
        this.totalShrawan = 0;
        this.totalBhadra = 0;
        this.totalAshoj = 0;
        this.totalFirstQuarter = 0;
        this.totalKartik = 0;
        this.totalMangsir = 0;
        this.totalPoush = 0;
        this.totalSecondQuarter = 0;
        this.totalMagh = 0;
        this.totalFalgun = 0;
        this.totalChaitra = 0;
        this.totalThirdQuarter = 0;
        this.totalBaisakh = 0;
        this.totalJestha = 0;
        this.totalAsadh = 0;
        this.totalYearly = 0;

        if (!this.tempTable || this.tempTable.length === 0) {
            this.gridApi?.setGridOption('pinnedBottomRowData', []);
            return;
        }

        // Calculate totals
        this.tempTable.forEach(item => {
            this.totalShrawan += Number(item.details.shrawan || 0);
            this.totalBhadra += Number(item.details.bhadra || 0);
            this.totalAshoj += Number(item.details.aswin || 0);
            this.totalFirstQuarter += Number(item.details.firstQuarter || 0);
            this.totalKartik += Number(item.details.kartik || 0);
            this.totalMangsir += Number(item.details.mangsir || 0);
            this.totalPoush += Number(item.details.poush || 0);
            this.totalSecondQuarter += Number(item.details.secondQuarter || 0);
            this.totalMagh += Number(item.details.magh || 0);
            this.totalFalgun += Number(item.details.falgun || 0);
            this.totalChaitra += Number(item.details.chaitra || 0);
            this.totalThirdQuarter += Number(item.details.thirdQuarter || 0);
            this.totalBaisakh += Number(item.details.baisakh || 0);
            this.totalJestha += Number(item.details.jestha || 0);
            this.totalAsadh += Number(item.details.asar || 0);
            this.totalYearly += Number(item.details.yearly || 0);
        });

        // Add a pinned bottom row for totals
        if (this.gridApi) {
            const pinnedBottomData = {
                ledgerName: 'TOTAL',
                details: {
                    shrawan: this.totalShrawan,
                    bhadra: this.totalBhadra,
                    aswin: this.totalAshoj,
                    firstQuarter: this.totalFirstQuarter,
                    kartik: this.totalKartik,
                    mangsir: this.totalMangsir,
                    poush: this.totalPoush,
                    secondQuarter: this.totalSecondQuarter,
                    magh: this.totalMagh,
                    falgun: this.totalFalgun,
                    chaitra: this.totalChaitra,
                    thirdQuarter: this.totalThirdQuarter,
                    baisakh: this.totalBaisakh,
                    jestha: this.totalJestha,
                    asar: this.totalAsadh,
                    yearly: this.totalYearly
                }
            };

            this.gridApi.setGridOption('pinnedBottomRowData', [pinnedBottomData]);
        }
    }

    exportToExcel() {
        if (!this.gridApi) {return;}

        const params = {
            fileName: `Ledger_Monthly_Report_${  new Date().toISOString().split('T')[0]}`,
            sheetName: 'Ledger Monthly Report',
            exportMode: 'xlsx'
        };

        this.gridApi.exportDataAsExcel(params);
    }
}
