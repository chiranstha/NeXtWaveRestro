import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PurchaseReturnReportServiceProxy,
    ReportingServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-purchase-return-report',
    templateUrl: './purchaseReturnReport.component.html',
})
export class PurchaseReturnReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    public destroy$ = new Subject<void>();
    allLedgers: UniversalDropdownDto[];
    allProducts: UniversalDropdownDto[];
    allGroup: UniversalDropdownDto[];

    tableRows: any[];
    displayedTableRows: any[];
    advancedFiltersAreShown = false;
    loading = false;
    myForm: FormGroup;
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
        private _proxy: PurchaseReturnReportServiceProxy,
        private _allproxy: ReportingServiceProxy,
        private _fb: FormBuilder
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit() {
        this.showMainSpinner();
        this.createForm();
        this.refreshLoad();
    }

    refreshLoad() {
        this.getLedger();
        this.getProductId();
        this.getGroupId();
        this.getFinancialYears();
        setTimeout(() => {
            this.loadReport();
        }, 1000);
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    getLedger() {
        this._allproxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allLedgers = data;
            const ledgerId = this.allLedgers[0].id;
            this.myForm.get('ledgerId').setValue(ledgerId);
        });
    }

    getProductId() {
        this._allproxy.getAllProductsForTableDropdown().subscribe((data) => {
            this.allProducts = data;
            const productId = this.allProducts[0].id;
            this.myForm.get('productId').setValue(productId);
        });
    }

    getGroupId() {
        this._allproxy.getAllProductGroupForTableDropdown().subscribe((data) => {
            this.allGroup = data;
            const productGroupId = this.allGroup[0].id;
            this.myForm.get('productGroupId').setValue(productGroupId);
        });
    }

    createForm() {
        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
            productId: [this.emptyGuId],
            productGroupId: [this.emptyGuId],
            showDetails: [false],
            branchId: [this.emptyGuId],
            ledgerId: [this.emptyGuId],
        });
        this.updateColumnDefs();
    }

    loadReport() {
        this.showMainSpinner();
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        const ledgerId = this.myForm.get('ledgerId').value;
        const productId = this.myForm.get('productId').value;
        const productGroupId = this.myForm.get('productGroupId').value;
        const type = this.myForm.get('showDetails').value;
        this.updateColumnDefs();
        this._proxy.getPurchaseReturnReport(fromMiti, toMiti, ledgerId, productId, productGroupId, type)
            .pipe(takeUntil(this.destroy$))
            .subscribe((result) => {
                this.hideMainSpinner();
                this.tableRows = result;
                this.displayedTableRows = this.tableRows;
            });
    }

    getFinancialYears() {
        this._allproxy.getFinancialYears().subscribe((result) => {
            this.myForm.get('fromMiti').setValue(result.fromMiti);
            this.myForm.get('toMiti').setValue(result.toMiti);
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
        const searchStr = typeof e === 'string' ? e : e?.target?.value;
        if (searchStr && this.tableRows) {
            this.displayedTableRows = this.tableRows.filter(
                (type) => type.voucherNo.toLowerCase().search(searchStr.toLowerCase()) !== -1
            );
        } else {
            this.displayedTableRows = this.tableRows || [];
        }
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
            .map((product) => `ProductName: ${product.productName}\nRate: ${this.formatGridNumber(product.rate)} Qty: ${product.qty} Amount: ${this.formatGridNumber(product.amount)}`)
            .join('\n\n');
    }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString();
    }
}
