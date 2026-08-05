import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
//
import { ColDef, ColGroupDef, GridApi, GridOptions, GridReadyEvent } from 'ag-grid-community';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ProductProfitReportDto, ProductProfitReportServiceProxy } from '@shared/service-proxies/service-proxies';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-product-profit-new-report',
    templateUrl: './productProfitReport.component.html',
    styleUrls: ['./productProfitReport.component.css'],
})
export class ProductProfitReportComponent extends AppComponentBase implements OnInit {
    private gridApi!: GridApi;
    myForm: FormGroup;
    filterText = '';
    advancedFiltersAreShown = false;
    rowData: ProductProfitReportDto[] = [];
    totalRecords = 0;


    ngOnInit(): void {
        this.createForm();
        this.setFinancialYear();

        setTimeout(() => {
            this.loadreport();
        }, 1000);
    }
    constructor(
        injector: Injector,
        private _router: Router,
        // private _reportingService: ReportingServiceProxy,
        private _proxy: ProductProfitReportServiceProxy,
        private _fb: FormBuilder,
        private _route: ActivatedRoute
    ) {
        super(injector);
        this.getSetting();
    }

    createForm() {
        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
        });
    }

    setFinancialYear() {
        this._proxy.getFinancialYears().subscribe(result => {
            this.myForm.patchValue({
                fromMiti: result.fromMiti,
                toMiti: result.toMiti
            });
        })
    }
    public gridOptions: GridOptions = {
        defaultColDef: {
            resizable: true,
            minWidth: 80,
            maxWidth: 200
        },
        headerHeight: 25,
        rowHeight: 24,
        animateRows: true,
        rowSelection: { mode: 'multiRow', enableClickSelection: false },
        groupSelectsChildren: true,
        pagination: false,
        pinnedBottomRowData: [],
        onGridReady: this.onGridReady.bind(this),
        suppressHorizontalScroll: false,
    };

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.gridApi.addEventListener('filterChanged', () => {
            this.calculateTotals();
        });

        if (this.rowData?.length > 0) {
            this.calculateTotals();
        }
    }

    calculateTotals(): void {
        if (!this.gridApi) {
            return;
        }

        const displayedRows = [];
        this.gridApi.forEachNodeAfterFilter(node => {
            displayedRows.push(node.data);
        });

        if (!displayedRows.length) {
            this.gridOptions.pinnedBottomRowData = [];
            return;
        }

        const totals = {
            productName: 'Total',
            openingStockValue: this.sumFilteredField('openingStockValue', displayedRows),
            inwardValue: this.sumFilteredField('inwardValue', displayedRows),
            outwardValue: this.sumFilteredField('outwardValue', displayedRows),
            closingValue: this.sumFilteredField('closingValue', displayedRows),
            profitAmount: this.sumFilteredField('profitAmount', displayedRows),
            // balanceCr: this.sumFilteredField('balanceCr', displayedRows),
        };

        this.gridOptions.pinnedBottomRowData = [totals];
        if (this.gridApi) {
            this.gridApi.refreshCells();
        }
    }

    sumFilteredField(fieldName: string, rows: any[]): number {
        return rows.reduce((sum, row) => {
            const value = row[fieldName] || 0;
            return sum + (typeof value === 'number' ? value : 0);
        }, 0);
    }


    searchValueOnApiCall(value: string) {
        this.filterText = value;
        if (this.gridApi) {
            this.gridApi.setGridOption('quickFilterText', value);
            this.calculateTotals();
        }
    }

    loadreport() {
        const { fromMiti, toMiti } = this.myForm.value;
        this._proxy
            .getReport(fromMiti, toMiti)
            .subscribe((result) => {
                this.rowData = this.setGridRowData(this.gridApi, result);
                this.totalRecords = this.rowData.length;
                if (this.gridApi) {
                    setTimeout(() => this.calculateTotals(), 100);
                }
            });
    }

    static numberFormatter(params: any) {
        if (params.value == null) {
            return '';
        }
        return Number(params.value).toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

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
            field: 'productName',
            headerName: this.l('Product Name'),
            sortable: true,
            filter: false,
            enableRowGroup: true,
            flex: 5,
            headerClass: 'light-blue-header'
        },
        {
            field: 'unitName',
            headerName: this.l('Unit'),
            sortable: true,
            filter: false,
            enableRowGroup: true,
            flex: 5,
            headerClass: 'light-blue-header'
        },
        {
            headerName: this.l('Opening'),
            children: [
                {
                    field: 'openingStockQty',
                    headerName: this.l('Qty'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                },
                {
                    field: 'openingStockValue',
                    headerName: this.l('Amount'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                }
            ]
        },
        {
            headerName: this.l('Inward'),
            children: [
                {
                    field: 'inwardRate',
                    headerName: this.l('Rate'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                },
                {
                    field: 'inwardQty',
                    headerName: this.l('Qty'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                },
                {
                    field: 'inwardValue',
                    headerName: this.l('Amount'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                }
            ]
        },
        {
            headerName: this.l('Outward'),
            children: [
                {
                    field: 'outwardRate',
                    headerName: this.l('Rate'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                },
                {
                    field: 'outwardQty',
                    headerName: this.l('Qty'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                },
                {
                    field: 'outwardValue',
                    headerName: this.l('Amount'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                }
            ]
        },
        {
            headerName: this.l('Closing'),
            children: [
                {
                    field: 'closingQty',
                    headerName: this.l('Qty'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                },
                {
                    field: 'closingValue',
                    headerName: this.l('Amount'),
                    sortable: true,
                    filter: false,
                    enableRowGroup: true,
                    flex: 3,
                    valueFormatter: ProductProfitReportComponent.numberFormatter
                }
            ]
        },
        {
            field: 'profitAmount',
            headerName: this.l('Profit'),
            sortable: true,
            filter: false,
            enableRowGroup: true,
            flex: 5,
            headerClass: 'light-blue-header',
            valueFormatter: ProductProfitReportComponent.numberFormatter
        },
    ];

    onCellDoubleClicked(params) {
        localStorage.setItem('accountledgerwise', JSON.stringify(this.myForm.value));
        this._router.navigate(['/app/main/reports/account-wise', params.data.accountLedgerId]);
    }
}
