import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PurchaseOrderReportListDto,
    ReportingServiceProxy,
    SalesReturnReportServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { forkJoin, Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-sales-return-report',
    templateUrl: './salesReturnReport.component.html',
})
export class SalesReturnReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    public destroy$ = new Subject<void>();
    pdfUrl: string;
    printPdf: boolean = false;
    displayedTableRows: any[] = [];
    advancedFiltersAreShown = false;
    showDetails = false;
    myForm: FormGroup;
    tableRows: PurchaseOrderReportListDto[] = [];
    allLedgers: UniversalDropdownDto[] = [];
    allProduct: UniversalDropdownDto[] = [];
    allGroup: UniversalDropdownDto[] = [];
    title: string = 'sales report';
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 140,
    };
    private numberFormatter = (params: any) => this.formatGridNumber(params.value);
    columnDefs: ColDef[] = [];

    constructor(
        injector: Injector,
        private _proxy: SalesReturnReportServiceProxy,
        private _allproxy: ReportingServiceProxy,
        private _fb: FormBuilder
    ) {
        super(injector);
        this.getSetting();

        this.myForm = this._fb.group({
            productId: [''],
            productGroupId: [''],
            cashOrPartyId: [''],
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
        });
    }

    ngOnInit() {
        this.createForm();
        this.refreshLoad();
    }

    createForm() {
        this.myForm = this._fb.group({
            fromDate: [this.today],
            toDate: [this.today],
            ledgerId: [this.emptyGuId],
            productId: [this.emptyGuId],
            groupId: [this.emptyGuId],
            showDetails: [false]
        });
        this.updateColumnDefs();
    }

    cancel() {
        this.printPdf = false;
    }

    refreshLoad() {
        this.showMainSpinner();
        forkJoin({
            financialYear: this._allproxy.getFinancialYears(),
            groups: this._allproxy.getAllProductGroupForTableDropdown(),
            products: this._allproxy.getAllProductsForTableDropdown(),
            ledgers: this._allproxy.getAllAccountLedgerForTableDropdown(),
        })
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this.hideMainSpinner();
                    this.markViewForCheck();
                })
            )
            .subscribe({
                next: (result) => {
                    this.allGroup = result.groups ?? [];
                    this.allProduct = result.products ?? [];
                    this.allLedgers = result.ledgers ?? [];
                    this.myForm.patchValue({
                        fromDate: result.financialYear?.fromMiti || this.today,
                        toDate: result.financialYear?.toMiti || this.today,
                        ledgerId: this.emptyGuId,
                        productId: this.emptyGuId,
                        groupId: this.emptyGuId,
                        showDetails: false,
                    });
                    this.loadReport();
                },
                error: () => {
                    this.notify.error(this.l('FailedToLoadFilters'));
                    this.loadReport();
                },
            });
    }

    refresh() {
        this.loadReport();
        if (this.myForm.get('showDetails').value === true) {
            this.showDetails = false;
            this.myForm.get('showDetails').setValue(false);
        }
    }

    getFinancialYear() {
        this._allproxy.getFinancialYears().subscribe((data) => {
            this.myForm.get('fromDate').setValue(data.fromMiti);
            this.myForm.get('toDate').setValue(data.toMiti);
        });
    }

    loadReport() {
        this.showMainSpinner();
        const fromDate = this.myForm.get('fromDate').value;
        const toDate = this.myForm.get('toDate').value;
        const ledgerId = this.myForm.get('ledgerId').value;
        const productId = this.myForm.get('productId').value;
        const productGroup = this.myForm.get('groupId').value;
        const type = this.myForm.get('showDetails').value;
        this.updateColumnDefs();

        this._proxy.getSalesReturnReport(fromDate, toDate, ledgerId, productId, productGroup, type)
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this.hideMainSpinner();
                    this.markViewForCheck();
                })
            )
            .subscribe({
                next: (result) => {
                    this.tableRows = result ?? [];
                    this.displayedTableRows = this.tableRows;
                },
                error: () => {
                    this.notify.error(this.l('FailedToLoadReport'));
                },
            });
    }

    searchFilter(searchStr: string) {
        if (searchStr && this.tableRows) {
            this.displayedTableRows = this.tableRows.filter((type) => {
                return (
                    type.voucherNo?.toString().toLocaleLowerCase().includes(searchStr.toLocaleLowerCase()) ||
                    type.ledgerName?.toLocaleLowerCase().includes(searchStr.toLocaleLowerCase()) ||
                    type.dateMiti?.toLocaleLowerCase().includes(searchStr.toLocaleLowerCase()) ||
                    type.totalAmount?.toString().toLocaleLowerCase().includes(searchStr.toLocaleLowerCase())
                );

            });
        } else {
            this.displayedTableRows = this.tableRows || [];
        }
        this.markViewForCheck();
    }


    // pdfDownload() {
    //     const fromDate = this.myForm.get('fromDate').value;
    //     const toDate = this.myForm.get('toDate').value;
    //     const ledgerId = this.myForm.get('ledgerId').value;
    //     const productId = this.myForm.get('productId').value;
    //     const productGroup = this.myForm.get('groupId').value;
    //     const type = this.myForm.get('showDetails').value;
    //     this._proxy.getPdfDownload(fromDate, toDate, ledgerId, productId, productGroup, type).subscribe((data) => {
    //         const linkSource = 'data:application/pdf;base64,' + data;
    //         this.pdfUrl = linkSource;
    //         this.printPdf = true;
    //     });
    // }

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }

    getledger() {
        this._allproxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allLedgers = data ?? [];
            this.myForm.get('ledgerId').setValue(this.emptyGuId);
        });
    }

    getProductId() {
        this._allproxy.getAllProductsForTableDropdown().subscribe((data) => {
            this.allProduct = data ?? [];
            this.myForm.get('productId').setValue(this.emptyGuId);
        });
    }

    getGroupId() {
        this._allproxy.getAllProductGroupForTableDropdown().subscribe((data) => {
            this.allGroup = data ?? [];
            this.myForm.get('groupId').setValue(this.emptyGuId);
        });
    }

    private updateColumnDefs(): void {
        const showDetails = this.myForm?.get('showDetails')?.value;
        this.columnDefs = [
            { headerName: 'Date', field: 'dateMiti' },
            { headerName: 'Voucher No', field: 'voucherNo' },
            { headerName: 'Ledger Name', field: 'ledgerName', minWidth: 220 },
            { headerName: 'Total Amount', field: 'totalAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
            {
                headerName: 'Product Details',
                hide: !showDetails,
                minWidth: 360,
                autoHeight: true,
                valueGetter: (params) => this.formatProductDetails(params.data?.details),
                cellStyle: { 'white-space': 'pre-line', 'line-height': '20px' },
            },
        ];
    }

    private formatProductDetails(details: any[] = []): string {
        return details
            .map((product) => `ProductName: ${product.productName || ''}\nRate: ${this.formatGridNumber(product.rate)} Qty: ${product.qty || ''} Amount: ${this.formatGridNumber(product.amount)}`)
            .join('\n\n');
    }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString();
    }
}
