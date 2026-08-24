import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ReportingServiceProxy, TaxReportServiceProxy } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';

import { delay, tap } from 'rxjs/operators';
import { ColDef, GridOptions, GridApi } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-sales-tax-report',
    templateUrl: './sales-tax-report.component.html',
})
export class SalesTaxReportComponent extends AppComponentBase implements OnInit {
    loading = false;

    displayedTableRows: any[] = [];
    tableRows: any[] = [];
    amount: string;
    localTaxableAmount: string;
    localTaxAmount: string;
    importTaxAmount: string;
    importTaxableAmount: string;
    taxableCapitalizeAmount: string;
    taxableCapitalizeTax: string;
    tableData: any;

    myForm: FormGroup;
    advancedFiltersAreShown = false;
    allTypes = [
        {id: 0, displayName: 'Both Invoice and Returns'},
        {id: 1, displayName: 'Invoice Only'},
        {id: 2, displayName: 'Returns Only'},
    ];
    gross: any;
    discount: any;
    net: any;
    nonTaxable: any;

    // AG Grid properties
    gridOptions: GridOptions;
    gridApi: GridApi;
    columnDefs: ColDef[];
    rowData: any[] = [];
    pinnedBottomRowData: any[] = [];

    constructor(
        injector: Injector,
        private _proxy: TaxReportServiceProxy,
        private _fb: FormBuilder,
        private _allProxy: ReportingServiceProxy,
        private _fileDownloadService: FileDownloadService,
    ) {
        super(injector);
        this.getSetting();
        this.initializeGrid();
    }

    async ngOnInit(): Promise<void> {
        await this.createForm();
        await this.getFinancialYear();
        await this.loadreport('', '', 0);
    }

    createForm() {

        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
            type: [0],
            showValue: [false],
            showProducts: [false],
        });

        // Subscribe to form changes to update grid columns
        this.myForm.get('showValue')?.valueChanges.subscribe(() => {
            if (this.gridApi) {
                this.updateGridColumns();
                this.updatePinnedBottomRow();
            }
        });

        this.myForm.get('showProducts')?.valueChanges.subscribe(() => {
            if (this.gridApi) {
                this.updateGridColumns();
                this.updatePinnedBottomRow();
            }
        });
    }

    getFinancialYear() {
        this._allProxy.getFinancialYears().subscribe((data) => {
            this.myForm.get('fromMiti').setValue(data.fromMiti);
        });
    }

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }

    searchFilter(e) {
        const searchStr = e.target.value;
        if (this.gridApi) {
            this.gridApi.setGridOption('quickFilterText', searchStr);
        } else {
            // Fallback for table view
            if (e) {
                this.displayedTableRows = this.tableRows.filter((type) => type.ledgerName.toLowerCase().search(searchStr.toLowerCase()) !== -1);
            } else {
                this.displayedTableRows = this.tableRows;
            }
        }
    }

    calculateTotal() {
        this.amount = this.tableRows.reduce((sum, item) => sum + item.amount, 0).toFixed(2);
        this.nonTaxable = this.tableRows.reduce((sum, item) => sum + item.nonTaxableAmount, 0).toFixed(2);
        this.gross = this.tableRows.reduce((sum, item) => sum + item.grossAmount, 0).toFixed(2);
        this.discount = this.tableRows.reduce((sum, item) => sum + item.discount, 0).toFixed(2);
        this.net = this.tableRows.reduce((sum, item) => sum + item.netAmount, 0).toFixed(2);
        this.localTaxableAmount = this.tableRows.reduce((sum, item) => sum + item.localTaxableAmount, 0).toFixed(2);
        this.localTaxAmount = this.tableRows.reduce((sum, item) => sum + item.localTaxAmount, 0).toFixed(2);
        this.importTaxAmount = this.tableRows.reduce((sum, item) => sum + item.importTaxAmount, 0).toFixed(2);
        this.importTaxableAmount = this.tableRows.reduce((sum, item) => sum + item.importTaxableAmount, 0).toFixed(2);
        this.taxableCapitalizeAmount = this.tableRows.reduce((sum, item) => sum + item.taxableCapitalizeAmount, 0).toFixed(2);
        this.taxableCapitalizeTax = this.tableRows.reduce((sum, item) => sum + item.taxableCapitalizeTax, 0).toFixed(2);
    }

    getReport() {
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        const type = this.myForm.get('type').value;
        this.loadreport(fromMiti, toMiti, type);
    }

    loadreport(fromMiti, toMiti, type) {
        this.loading = true;
        this._proxy.getSalesReportNew(fromMiti, toMiti, type).pipe(
            tap(() => {
            }),
            delay(200)
        )
            .subscribe((result) => {
                this.loading = false;
                const rows = result ?? [];
                this.displayedTableRows = rows;
                this.tableRows = rows;
                this.rowData = this.setGridRowData(this.gridApi, rows);
                this.calculateTotal();
                this.updateGridColumns();
                this.updatePinnedBottomRow();
            });
    }

    numberWithCommas(x) {
        return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    getExcel() {
        const {toMiti, fromMiti, type} = this.myForm.value;
        this._proxy.getSalesTaxToExcel(fromMiti, toMiti, type).subscribe((res) => {
            this._fileDownloadService.downloadTempFile(res);
        });
    }

    initializeGrid() {
        this.columnDefs = this.getColumnDefs();
        this.gridOptions = {
            columnDefs: this.columnDefs,
            rowData: this.rowData,
            pinnedBottomRowData: this.pinnedBottomRowData,
            suppressMenuHide: true,
            suppressMovableColumns: true,
            suppressCellFocus: true,
            rowSelection: { mode: 'multiRow', enableClickSelection: false },
            enableCellTextSelection: true,
            domLayout: 'normal',
            headerHeight: 50,
            rowHeight: 40,
            defaultColDef: {
                sortable: true,
                filter: true,
                resizable: true,
                minWidth: 100,
                cellClass: 'text-center'
            }
        };
    }

    getColumnDefs(): ColDef[] {
        const baseColumns: ColDef[] = [
            {
                headerName: this.l('S.N'),
                field: 'serialNumber',
                width: 80,
                valueGetter: (params) => params.node ? params.node.rowIndex + 1 : null,
                cellClass: 'text-center',
                pinned: 'left'
            },
            {
                headerName: this.l('DATE MITI'),
                field: 'dateMiti',
                width: 120,
                cellClass: 'text-center'
            },
            {
                headerName: this.l('BILL NO'),
                field: 'voucherNo',
                width: 120,
                cellClass: 'text-center font-weight-bold'
            },
            {
                headerName: this.l('LEDGER'),
                field: 'ledgerName',
                width: 200,
                cellClass: 'text-left'
            },
            {
                headerName: this.l('PAN'),
                field: 'pan',
                width: 120,
                cellClass: 'text-center'
            }
        ];

        // Conditional product column
        const showProducts = this.myForm?.get('showProducts')?.value || false;
        if (showProducts) {
            baseColumns.push({
                headerName: this.l('PRODUCT'),
                field: 'productName',
                width: 150,
                cellClass: 'text-left'
            });
        }

        // Continue with remaining columns
        const remainingColumns: ColDef[] = [
            {
                headerName: this.l('PRODUCT CATEGORY'),
                field: 'productCategoryName',
                width: 150,
                cellClass: 'text-left'
            },
            {
                headerName: this.l('UNIT'),
                field: 'unitsName',
                width: 100,
                cellClass: 'text-center'
            },
            {
                headerName: this.l('QTY'),
                field: 'quantity',
                width: 100,
                cellClass: 'text-right',
                valueFormatter: (params) => this.numberWithCommas(params.value)
            },
            {
                headerName: this.l('Gross Amount'),
                field: 'grossAmount',
                width: 130,
                cellClass: 'text-right',
                valueFormatter: (params) => this.numberWithCommas(params.value)
            },
            {
                headerName: this.l('Discount'),
                field: 'discount',
                width: 120,
                cellClass: 'text-right',
                valueFormatter: (params) => this.numberWithCommas(params.value)
            },
            {
                headerName: this.l('NET Amount'),
                field: 'netAmount',
                width: 130,
                cellClass: 'text-right',
                valueFormatter: (params) => this.numberWithCommas(params.value)
            },
            {
                headerName: this.l('Non Taxable'),
                field: 'nonTaxableAmount',
                width: 130,
                cellClass: 'text-right',
                valueFormatter: (params) => this.numberWithCommas(params.value)
            },
            {
                headerName: this.l('TAXABLE'),
                field: 'localTaxableAmount',
                width: 130,
                cellClass: 'text-right',
                valueFormatter: (params) => this.numberWithCommas(params.value),
                columnGroupShow: 'open'
            },
            {
                headerName: this.l('TAX'),
                field: 'localTaxAmount',
                width: 120,
                cellClass: 'text-right',
                valueFormatter: (params) => this.numberWithCommas(params.value),
                columnGroupShow: 'open'
            }
        ];

        // Add conditional import columns
        const showValue = this.myForm?.get('showValue')?.value || false;
        if (showValue) {
            remainingColumns.push(
                {
                    headerName: this.l('TAXABLE'),
                    field: 'importTaxableAmount',
                    width: 130,
                    cellClass: 'text-right',
                    valueFormatter: (params) => this.numberWithCommas(params.value),
                    columnGroupShow: 'open'
                },
                {
                    headerName: this.l('TAX'),
                    field: 'importTaxAmount',
                    width: 120,
                    cellClass: 'text-right',
                    valueFormatter: (params) => this.numberWithCommas(params.value),
                    columnGroupShow: 'open'
                },
                {
                    headerName: this.l('TAXABLE'),
                    field: 'taxableCapitalizeAmount',
                    width: 130,
                    cellClass: 'text-right',
                    valueFormatter: (params) => this.numberWithCommas(params.value),
                    columnGroupShow: 'open'
                },
                {
                    headerName: this.l('TAX'),
                    field: 'taxableCapitalizeTax',
                    width: 120,
                    cellClass: 'text-right',
                    valueFormatter: (params) => this.numberWithCommas(params.value),
                    columnGroupShow: 'open'
                }
            );
        }

        // Add final column
        remainingColumns.push({
            headerName: this.l('Grand Total'),
            field: 'amount',
            width: 130,
            cellClass: 'text-right',
            valueFormatter: (params) => this.numberWithCommas(params.value),
            pinned: 'right'
        });

        return [...baseColumns, ...remainingColumns];
    }

    onGridReady(params) {
        this.gridApi = params.api;
        this.gridApi.sizeColumnsToFit();
    }

    updateGridColumns() {
        this.columnDefs = this.getColumnDefs();
        this.gridApi?.setGridOption('columnDefs', this.columnDefs);
    }

    updatePinnedBottomRow() {
        const showProducts = this.myForm?.get('showProducts')?.value || false;
        const totalRow = {
            serialNumber: '',
            dateMiti: '',
            voucherNo: '',
            ledgerName: '',
            pan: '',
            productName: '',
            productCategoryName: '',
            unitsName: showProducts ? '' : 'Total',
            quantity: showProducts ? 'Total' : '',
            grossAmount: this.gross,
            discount: this.discount,
            netAmount: this.net,
            nonTaxableAmount: this.nonTaxable,
            localTaxableAmount: this.localTaxableAmount,
            localTaxAmount: this.localTaxAmount,
            importTaxableAmount: this.importTaxableAmount,
            importTaxAmount: this.importTaxAmount,
            taxableCapitalizeAmount: this.taxableCapitalizeAmount,
            taxableCapitalizeTax: this.taxableCapitalizeTax,
            amount: this.amount
        };

        this.pinnedBottomRowData = [totalRow];
        this.gridApi?.setGridOption('pinnedBottomRowData', this.pinnedBottomRowData);
    }
}
