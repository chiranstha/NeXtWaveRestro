import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PurchaseMasterReportServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { takeUntil } from 'rxjs/operators';
import { BehaviorSubject, Subject } from 'rxjs';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'appPurchaseMasterReport',
    templateUrl: './purchaseMasterReport.component.html',
    styleUrls: ['./purchaseMasterReport.component.css']
})
export class PurchaseMasterReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    public destroy$ = new Subject<void>();

    totaltax: number;
    totalamount: number;
    totalgrandTotals: number;
    drag$: BehaviorSubject<number> = new BehaviorSubject(null);
    pdfdata: string;
    loading = false;
    advancedFiltersAreShown = false;
    myForm: FormGroup;
    isPdfShow = false;
    allLedgers: UniversalDropdownDto[];
    allProducts: any[];
    tableRows: any[];
    displayedTableRows: any[];
    searchDetails: any;
    pinnedBottomRowData: any[] = [];
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 140,
    };
    private numberFormatter = (params: any) => this.formatGridNumber(params.value);
    columnDefs: ColDef[] = [];

    constructor(injector: Injector,
                private _proxy: PurchaseMasterReportServiceProxy,
                private _fb: FormBuilder,
                private _filedownloadService: FileDownloadService) {
        super(injector);
        this.getSetting();
    }

    ngOnInit() {
        this.createForm();
        this.refreshLoad();
    }

    refreshLoad() {
        this.getFinancialYear();
        this.getledger();
        this.getProduct();
        setTimeout(() => {
            this.loadReport();
        }, 1000);
    }

    getFinancialYear() {
        this._proxy.getFinancialYears().subscribe((data) => {
            this.myForm.get('fromMiti').setValue(data.fromMiti);
            this.myForm.get('toMiti').setValue(data.toMiti);
        });
    }

    createForm() {
        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
            ledgerId: [this.emptyGuId],
            productId: [this.emptyGuId],
            showDetails: [false]
        });
        this.updateColumnDefs();
    }

    searchFilter(e) {
        const searchStr = e?.target?.value;
        const rows = this.searchDetails || this.tableRows || [];
        if (searchStr) {
            this.displayedTableRows = rows.filter((type) => type.ledgerName.toLowerCase().search(searchStr.toLowerCase()) !== -1);
        } else {
            this.displayedTableRows = rows;
        }
        this.calculateTotal();
    }

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }


    getledger() {
        this._proxy.getAllLedgers().subscribe((data) => {
            this.allLedgers = data;
            const ledgerId = this.allLedgers[0].id;
            this.myForm.get('ledgerId').setValue(ledgerId);
        });
    }

    getProduct() {
        this._proxy.getAllProducts().subscribe((data) => {
            this.allProducts = data;
            const productId = this.allProducts[0].id;
            this.myForm.get('productId').setValue(productId);
        });
    }

    loadReport() {
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        const ledgerId = this.myForm.get('ledgerId').value;
        const productId = this.myForm.get('productId').value;
        const type = this.myForm.get('showDetails').value;
        this.updateColumnDefs();
        this._proxy.getPurchaseReport(fromMiti, toMiti, ledgerId, productId, type)
            .pipe(takeUntil(this.destroy$))
            .subscribe((result) => {
                this.hideMainSpinner();
                this.tableRows = result;
                this.displayedTableRows = this.tableRows;
                this.calculateTotal();
            });
    }

    calculateTotal() {
        this.totaltax = this.displayedTableRows.reduce((sum, item) => sum + item.totalTaxAmount, 0);
        this.totalamount = this.displayedTableRows.reduce((sum, item) => sum + item.totalAmount, 0);
        this.totalgrandTotals = this.displayedTableRows.reduce((sum, item) => sum + item.grandTotal, 0);
        this.pinnedBottomRowData = [
            {
                date: 'Total',
                totalAmount: this.totalamount,
                totalTaxAmount: this.totaltax,
                grandTotal: this.totalgrandTotals,
            },
        ];
    }

    exportToExcel(e: any) {
        if (e) {
            const fromMiti = this.myForm.get('fromMiti').value;
            const toMiti = this.myForm.get('toMiti').value;
            const ledgerId = this.myForm.get('ledgerId').value;
            const productId = this.myForm.get('productId').value;
            const sType = this.myForm.get('type').value;
            this._proxy.getPurchaseReportExcel(fromMiti, toMiti, ledgerId, productId, sType).subscribe((data) => {
                this._filedownloadService.downloadTempFile(data);
            });
        }
    }

    exportToExcelfromAPI() {

            const fromMiti = this.myForm.get('fromMiti').value;
            const toMiti = this.myForm.get('toMiti').value;
            const ledgerId = this.myForm.get('ledgerId').value;
            const productId = this.myForm.get('productId').value;
            const sType = this.myForm.get('showDetails').value;
            this._proxy
                .getPurchaseReportExcel(fromMiti, toMiti, ledgerId, productId, sType)
                .subscribe((result) => {
                    this._filedownloadService.downloadTempFile(result);
                });

    }

    // downloadPdf() {
    //     const fromMiti = this.myForm.get('fromMiti').value;
    //     const toMiti = this.myForm.get('toMiti').value;
    //     const ledgerId = this.myForm.get('ledgerId').value;
    //     const productId = this.myForm.get('productId').value;
    //     const sType = this.myForm.get('showDetails').value;
    //     this._proxy
    //         .getPurchaseReportPdf(fromMiti, toMiti, ledgerId, productId, sType)
    //         .subscribe((data) => {
    //             this.pdfdata = 'data:application/pdf;base64,' + data;
    //         });

    //     this.isPdfShow = true;
    // }

    private updateColumnDefs(): void {
        const showDetails = this.myForm?.get('showDetails')?.value;
        this.columnDefs = [
            { headerName: 'Date', field: 'date' },
            { headerName: 'Voucher No', field: 'invoiceNo' },
            { headerName: 'Ledger Name', field: 'ledgerName', minWidth: 220 },
            {
                headerName: 'Product Details',
                hide: !showDetails,
                minWidth: 320,
                autoHeight: true,
                valueGetter: (params) => this.formatProductDetails(params.data?.purchaseDetail),
                cellStyle: { 'white-space': 'pre-line', 'line-height': '20px' },
            },
            { headerName: 'Total Amount', field: 'totalAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
            { headerName: 'Tax Amount', field: 'totalTaxAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
            { headerName: 'Grand Total', field: 'grandTotal', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        ];
    }

    private formatProductDetails(details: any[] = []): string {
        return details
            .map((product) => `Name: ${product.productName}\nQty: ${product.qty} Amount: ${this.formatGridNumber(product.amount)}`)
            .join('\n\n');
    }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString();
    }
}
