import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';

import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ReportingServiceProxy,
    TaxReportDto,
    TaxReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef, GridApi, GridOptions } from 'ag-grid-enterprise';

import { delay, tap } from 'rxjs/operators';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-purchase-tax-report',
    templateUrl: './purchase-tax-report.component.html',
    styleUrls: ['./purchase-tax-report.component.css']
})
export class PurchaseTaxReportComponent extends AppComponentBase implements OnInit {

    loading = false;

    displayedTableRows: TaxReportDto[] = [];
    tableRows: TaxReportDto[] = [];
    amount: string;
    localTaxableAmount: string;
    localTaxAmount: string;
    importTaxAmount: string;
    importTaxableAmount: string;
    taxableCapitalizeAmount: string;
    taxableCapitalizeTax: string;
    tableData: any;

    advancedFiltersAreShown = false;
    myForm: FormGroup;
    allTypes = [
        {id: 0, displayName: 'Both Invoice and Returns'},
        {id: 1, displayName: 'Invoice Only'},
        {id: 2, displayName: 'Returns Only'},
    ];
    nonTaxable: string;
    gross: string;
    discount: string;
    net: string;

    // AG Grid properties
    gridApi: GridApi;
    gridOptions: GridOptions;
    columnDefs: ColDef[];
    rowData: TaxReportDto[] = [];
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

    initializeGrid() {
        this.columnDefs = [
            {
                headerName: 'S.N',
                valueGetter: 'node.rowIndex + 1',
                width: 80,
                pinned: 'left',
                cellStyle: {'text-align': 'center'}
            },
            {
                headerName: 'DATE MITI',
                field: 'dateMiti',
                width: 120,
                cellStyle: {'text-align': 'center'}
            },
            {
                headerName: 'BILLNO',
                field: 'voucherNo',
                width: 120,
                cellStyle: {'text-align': 'center', 'font-weight': 'bold'}
            },
             {
                headerName: 'Vendor Voucher No',
                field: 'vendorInvoiceNo',
                width: 120,
                cellStyle: {'text-align': 'center', 'font-weight': 'bold'}
            },
            {
                headerName: 'LEDGER',
                field: 'ledgerName',
                width: 200,
                cellStyle: {'text-align': 'left'}
            },
            {
                headerName: 'PAN',
                field: 'pan',
                width: 120,
                cellStyle: {'text-align': 'center'}
            },
            {
                headerName: 'PRODUCT',
                field: 'productName',
                width: 200,
                hide: true, // Will be shown/hidden based on form control
                cellStyle: {'text-align': 'left'}
            },
            {
                headerName: 'PRODUCT CATEGORY',
                field: 'productCategoryName',
                width: 150,
                cellStyle: {'text-align': 'center'}
            },
            {
                headerName: 'UNIT',
                field: 'unitsName',
                width: 100,
                cellStyle: {'text-align': 'center'}
            },
            {
                headerName: 'QTY',
                field: 'quantity',
                width: 100,
                cellStyle: {'text-align': 'right'}
            },
            {
                headerName: 'Gross Amount',
                field: 'grossAmount',
                width: 130,
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Discount',
                field: 'discount',
                width: 120,
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'NET Amount',
                field: 'netAmount',
                width: 130,
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Non Taxable',
                field: 'nonTaxableAmount',
                width: 130,
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Local Taxable',
                field: 'localTaxableAmount',
                width: 130,
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Local Tax',
                field: 'localTaxAmount',
                width: 120,
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Import Taxable',
                field: 'importTaxableAmount',
                width: 130,
                hide: true, // Will be shown/hidden based on form control
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Import Tax',
                field: 'importTaxAmount',
                width: 120,
                hide: true, // Will be shown/hidden based on form control
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Capitalize Taxable',
                field: 'taxableCapitalizeAmount',
                width: 150,
                hide: true, // Will be shown/hidden based on form control
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Capitalize Tax',
                field: 'taxableCapitalizeTax',
                width: 130,
                hide: true, // Will be shown/hidden based on form control
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            },
            {
                headerName: 'Grand Total',
                field: 'amount',
                width: 130,
                pinned: 'right',
                cellStyle: {'text-align': 'right'},
                valueFormatter: params => this.numberWithCommas(params.value)
            }
        ];

        this.gridOptions = {
            columnDefs: this.columnDefs,
            rowData: this.rowData,
            pinnedBottomRowData: this.pinnedBottomRowData,
            defaultColDef: {
                resizable: true,
                sortable: true,
                filter: true
            },
            rowSelection: { mode: 'multiRow', enableClickSelection: false },
            rowHeight: 35,
            headerHeight: 40,
            animateRows: true,
            enableCellTextSelection: true
        };
    }

    async ngOnInit(): Promise<void> {
        await this.createForm();
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
        this._allProxy.getFinancialYears().subscribe((data) => {
            this.myForm.get('fromMiti').setValue(data.fromMiti);
        });

        // Watch for form changes to update column visibility
        this.myForm.get('showProducts').valueChanges.subscribe(() => {
            this.updateColumnVisibility();
        });

        this.myForm.get('showValue').valueChanges.subscribe(() => {
            this.updateColumnVisibility();
        });
    }

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }

    getReport() {
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        const type = this.myForm.get('type').value;
        this.loadreport(fromMiti, toMiti, type);
    }

    loadreport(fromMiti, toMiti, type) {
        this.loading = true;
        this._proxy.getPurchaseReportNew(fromMiti, toMiti, type, undefined).pipe(
            tap(() => {

            }),
            delay(200)
        )
            .subscribe((result) => {
                //
                this.loading = false;

                const rows = result ?? [];
                this.displayedTableRows = rows;
                this.tableRows = rows;
                this.rowData = this.setGridRowData(this.gridApi, rows);
                //
                this.calculateTotal();
                this.updateGridData();
            });
    }

    onGridReady(params) {
        this.gridApi = params.api;
        this.updateColumnVisibility();
    }

    updateGridData() {
        if (this.gridApi) {
            this.gridApi.setGridOption('rowData', this.rowData);
            this.updatePinnedBottomData();
        }
    }

    updateColumnVisibility() {
        if (this.gridApi) {
            const showProducts = this.myForm?.get('showProducts')?.value || false;
            const showValue = this.myForm?.get('showValue')?.value || false;

            this.gridApi.setColumnsVisible(['productName'], showProducts);
            this.gridApi.setColumnsVisible([
                'importTaxableAmount',
                'importTaxAmount',
                'taxableCapitalizeAmount',
                'taxableCapitalizeTax'
            ], showValue);
        }
    }

    updatePinnedBottomData() {
        const totalRow = {
            dateMiti: 'Total',
            grossAmount: parseFloat(this.gross),
            discount: parseFloat(this.discount),
            netAmount: parseFloat(this.net),
            nonTaxableAmount: parseFloat(this.nonTaxable),
            localTaxableAmount: parseFloat(this.localTaxableAmount),
            localTaxAmount: parseFloat(this.localTaxAmount),
            importTaxableAmount: parseFloat(this.importTaxableAmount),
            importTaxAmount: parseFloat(this.importTaxAmount),
            taxableCapitalizeAmount: parseFloat(this.taxableCapitalizeAmount),
            taxableCapitalizeTax: parseFloat(this.taxableCapitalizeTax),
            amount: parseFloat(this.amount)
        };

        this.pinnedBottomRowData = [totalRow];
        if (this.gridApi) {
            this.gridApi.setGridOption('pinnedBottomRowData', this.pinnedBottomRowData);
        }
    }

    getExcel() {
        const {toMiti, fromMiti, type} = this.myForm.value;
        this._proxy.getPurchaseTaxToExcel(fromMiti, toMiti, type, undefined).subscribe((res) => {
            this._fileDownloadService.downloadTempFile(res);
        });
    }

    calculateTotal() {

        this.amount = this.tableRows.reduce((sum, item) => sum + item.amount, 0).toFixed(2);
        this.nonTaxable = this.tableRows.reduce((sum, item) => sum + item.nonTaxableAmount, 0).toFixed(2);
        this.gross = this.tableRows.reduce((sum, item) => sum + item.grossAmount, 0).toFixed(2);
        this.discount = this.tableRows.reduce((sum, item) => sum + item.discount, 0).toFixed(2);
        this.net = this.tableRows.reduce((sum, item) => sum + item.netAmount, 0).toFixed(2);
        this.localTaxableAmount = this.tableRows.reduce((sum, item) => sum + item.localTaxableAmount, 0).toFixed(2);
        this.localTaxAmount = this.tableRows.reduce((sum, item) => sum + item.localTaxAmount, 0).toFixed(2);
        }

    searchFilter(e) {
        const searchStr = e.target ? e.target.value : e;
        if (this.gridApi) {
            this.gridApi.setGridOption('quickFilterText', searchStr);
        } else {
            // Fallback for non-grid view
            if (searchStr) {
                this.displayedTableRows = this.tableRows.filter((type) =>
                    type.ledgerName.toLowerCase().search(searchStr.toLowerCase()) !== -1);
            } else {
                this.displayedTableRows = this.tableRows;
            }
        }
    }

    numberWithCommas(x) {
        return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }


}
