import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ReportingServiceProxy, TaxReportServiceProxy } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';

import { tap } from 'rxjs';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-tax-for-lakhs-purchase',
    templateUrl: './tax-for-lakhs-purchase.component.html',
    styleUrls: ['./tax-for-lakhs-purchase.component.css']
})
export class TaxForLakhsPurchaseComponent extends AppComponentBase implements OnInit {
    loading = false;

    displayedTableRows: any[] = [];
    tableRows: any[] = [];
    advancedFiltersAreShown: boolean;
    myForm: FormGroup;

    // AG Grid properties
    gridApi: GridApi;
    rowData: any[] = [];

    // Column definitions
    columnDefs: ColDef[] = [];

    allTypes = [
        { id: 0, displayName: 'By Product' },
        { id: 1, displayName: 'By Party' },
    ];

    constructor(
        injector: Injector,
        private _proxy: TaxReportServiceProxy,
        private _allProxy: ReportingServiceProxy,
        private _fb: FormBuilder,
        private _fileDownloadService: FileDownloadService,
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.createForm();
        this.setupColumnDefs();
    }

    setupColumnDefs() {
        // Initial column setup - will be updated based on type selection
        this.updateColumnDefs();
    }

    updateColumnDefs() {
        const type = this.myForm?.value?.type || 0;

        if (type === 0) { // By Product
            this.columnDefs = [
                { headerName: 'S.N', valueGetter: 'node.rowIndex + 1', width: 80, pinned: 'left' },
                { headerName: 'Date Miti', field: 'date', width: 120 },
                { headerName: 'Bill No', field: 'billNo', width: 120, cellClass: 'fw-bold' },
                { headerName: 'Ledger', field: 'partyName', width: 200 },
                { headerName: 'Product', field: 'productName', width: 200 },
                { headerName: 'Gross Amount', field: 'grossAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Discount', field: 'discount', width: 120, valueFormatter: this.numberFormatter },
                { headerName: 'Net Amount', field: 'netAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Non Taxable', field: 'nonTaxableAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Taxable', field: 'taxableAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Tax', field: 'taxAmount', width: 120, valueFormatter: this.numberFormatter },
                { headerName: 'Grand Total', field: 'grandTotal', width: 140, valueFormatter: this.numberFormatter, cellClass: 'fw-bold' }
            ];
        } else { // By Party
            this.columnDefs = [
                { headerName: 'S.N', valueGetter: 'node.rowIndex + 1', width: 80, pinned: 'left' },
                { headerName: 'Ledger', field: 'partyName', width: 200 },
                { headerName: 'Address', field: 'address', width: 200 },
                { headerName: 'PAN', field: 'pan', width: 120 },
                { headerName: 'Gross Amount', field: 'grossAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Discount', field: 'discount', width: 120, valueFormatter: this.numberFormatter },
                { headerName: 'Net Amount', field: 'netAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Non Taxable', field: 'nonTaxableAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Taxable', field: 'taxableAmount', width: 140, valueFormatter: this.numberFormatter },
                { headerName: 'Tax', field: 'taxAmount', width: 120, valueFormatter: this.numberFormatter },
                { headerName: 'Grand Total', field: 'grandTotal', width: 140, valueFormatter: this.numberFormatter, cellClass: 'fw-bold' }
            ];
        }
    }

    createForm() {

        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
            type: [0],
        });
        this._allProxy.getFinancialYears().subscribe((data) => {
            this.myForm.get('fromMiti').setValue(data.fromMiti);
        });
    }


    getReport() {
        const { fromMiti, toMiti, type } = this.myForm.value;
        // Update column definitions when report type changes
        this.updateColumnDefs();

        if (type === 0) {
            this.loadreportByProduct(fromMiti, toMiti);
        } else {
            this.loadreportByParty(fromMiti, toMiti);
        }
    }

    loadreportByProduct(fromMiti, toMiti) {
        this.loading = true;
        this._proxy.getAbove1LakhPurchaseDetail(fromMiti, toMiti, undefined).pipe(
            tap(() => {

            })
        )
            .subscribe((result) => {
                this.loading = false;

                const rows = result ?? [];
                this.displayedTableRows = rows;
                this.tableRows = rows;
                this.rowData = this.setGridRowData(this.gridApi, rows);
            });
    }

    loadreportByParty(fromMiti, toMiti) {
        this.loading = true;
        this._proxy.getAbove1LakhPurchaseParty(fromMiti, toMiti).pipe(
            tap(() => {

            })
        )
            .subscribe((result) => {
                this.loading = false;

                const rows = result ?? [];
                this.displayedTableRows = rows;
                this.tableRows = rows;
                this.rowData = this.setGridRowData(this.gridApi, rows);
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
        const searchStr = e.target?.value || e;
        if (searchStr && this.gridApi) {
            this.gridApi.setGridOption('quickFilterText', searchStr);
        } else if (this.gridApi) {
            this.gridApi.setGridOption('quickFilterText', '');
        }

        // Keep the original logic for backward compatibility
        if (searchStr && this.tableRows) {
            this.displayedTableRows = this.tableRows.filter((type) =>
                type.partyName?.toLowerCase().includes(searchStr.toLowerCase()) ||
                type.billNo?.toLowerCase().includes(searchStr.toLowerCase()) ||
                type.productName?.toLowerCase().includes(searchStr.toLowerCase())
            );
        } else {
            this.displayedTableRows = this.tableRows;
        }
    }

    numberWithCommas(x) {
        return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    // AG Grid number formatter
    numberFormatter = (params) => {
        if (params.value != null && params.value !== '') {
            return this.numberWithCommas(params.value);
        }
        return '';
    }

    // AG Grid ready event
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
    }

    getExcel() {
        const { fromMiti, toMiti, type } = this.myForm.value;
        if (type === 0) { // by product
            this._proxy.getAbove1LakhPurchaseDetailExcel(fromMiti, toMiti, undefined).subscribe((res) => {
                this._fileDownloadService.downloadTempFile(res);
            });
        } else { // by party
            this._proxy.getAbove1LakhPurchasePartyExcel(fromMiti, toMiti).subscribe((res) => {
                this._fileDownloadService.downloadTempFile(res);
            });
        }
    }

}
