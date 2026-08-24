import { ChangeDetectionStrategy, Component, Injector, OnInit, TemplateRef } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ReportingServiceProxy, TaxableCustomerServiceProxy } from '@shared/service-proxies/service-proxies';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { delay } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-purchase-party-tax-report',
    templateUrl: './purchasePartyTaxReport.component.html',
})
export class PurchasePartyTaxReportComponent extends AppComponentBase implements OnInit {
    loading = false;
    tableRows: any[];
    searchTerm = '';
    displayedRows: any[];
    myForm: FormGroup;
    totalItems = 0;
    fromMitiLoad: string;
    toMitiLoad: string;
    pdfdata: string;
    advancedFiltersAreShown = false;
    maxTaxAmount: number = 100000;
    isPdfShow = false;
    modalRef?: BsModalRef;
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 140,
    };
    private numberFormatter = (params: any) => this.formatGridNumber(params.value);
    columnDefs: ColDef[] = [
        { headerName: 'Name', field: 'name', minWidth: 220 },
        { headerName: 'Pan Number', field: 'panNo' },
        { headerName: 'Amount', field: 'amount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Tax Amount', field: 'taxAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Grand Total', field: 'grandTotal', valueFormatter: this.numberFormatter, type: 'rightAligned' },
    ];

    constructor(
        injector: Injector,
        private _proxy: TaxableCustomerServiceProxy,
        private _allproxy: ReportingServiceProxy,
        private _fb: FormBuilder,
        private modalService: BsModalService,
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.refresh();

        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
        });
    }

    refresh() {
        this.loadreport();
        this.getFinancialYears();
    }

    getFinancialYears() {
        this._allproxy.getFinancialYears().subscribe((result) => {
            this.myForm.get('fromMiti').setValue(result.fromMiti);
            this.myForm.get('toMiti').setValue(result.toMiti);
        });
    }

    getAdvanceLedger(fromMiti, toMiti, maxTaxableAmount) {
        this.loading = true;
        this._proxy
            .getTaxableSuppliers(fromMiti, toMiti, maxTaxableAmount)
            .pipe(
                delay(2000)
            )
            .subscribe((result) => {
                this.tableRows = result;
                this.loading = false;

                this.totalItems = result.length;
                this.displayedRows = this.tableRows;
            });
    }

    loadreport() {
        this.loading = true;
        this._proxy
            .getTaxableSuppliers('', '', this.maxTaxAmount)
            .pipe(
                delay(1000)
            )
            .subscribe((result) => {
                this.tableRows = result;
                this.loading = false;

                this.totalItems = result.length;
                this.displayedRows = this.tableRows;
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
            this.displayedRows = this.tableRows.filter((type) => type.name.toLowerCase().search(searchStr.toLowerCase()) !== -1);
        } else {
            this.displayedRows = this.tableRows || [];
        }
    }

    resetSearch(): void {
        this.displayedRows = this.tableRows;
        this.searchTerm = '';
    }

    openDialog(dialog: TemplateRef<any>): void {
        this.pdfdata = '';
        this.isPdfShow = false;
        this.modalRef = this.modalService.show(dialog);
    }

    onAdvanceSearch(form: any) {
        const {fromMiti} = form;
        const {toMiti} = form;
        const {maxTaxableAmount} = form;
        this.getAdvanceLedger(fromMiti, toMiti, maxTaxableAmount);
        document.getElementById('cancellation').click();
    }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString();
    }
}
