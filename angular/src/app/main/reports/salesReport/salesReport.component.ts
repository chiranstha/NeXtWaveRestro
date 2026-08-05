import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit, TemplateRef } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';

import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ReportingServiceProxy,
    SalesMasterReportServiceProxy,
    SalesReportDtoByDate,
} from '@shared/service-proxies/service-proxies';

import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-sales-report',
    templateUrl: './salesReport.component.html',
    styleUrls: ['./salesReport.component.css'],
})
export class SalesReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    public destroy$ = new Subject<void>();
    tableRows: SalesReportDtoByDate[] = [];
    displayedRows: SalesReportDtoByDate[] = [];
    searchTerm = '';
    totalItems = 0;
    productGroupId: number;
    allProductGroups: any[];
    myForm: FormGroup;

    loading = false;
    advancedFiltersAreShown = false;
    modalRef?: BsModalRef;
    detailGridDefaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 120,
    };
    detailColumnDefs: ColDef[] = [
        {
            headerName: 'Product Name',
            field: 'product',
            minWidth: 220,
            valueGetter: (params) => params.data?.imeis ? `${params.data.product} { ${params.data.imeis} }` : params.data?.product,
        },
        { headerName: 'Quantity', field: 'qty', valueFormatter: (params) => this.formatGridNumber(params.value), type: 'rightAligned' },
        { headerName: 'Amount', field: 'amount', valueFormatter: (params) => this.formatGridNumber(params.value), type: 'rightAligned' },
    ];

    constructor(
        injector: Injector,
        private _proxy: SalesMasterReportServiceProxy,
        private _allProxy: ReportingServiceProxy,
        private modalService: BsModalService,
        private _fb: FormBuilder
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti || this.today],
            toMiti: [this.toMiti || this.today],
        });
        this.loadFinancialYearAndReport();
    }

    ngOnDestroy() {
        this.destroy$.next();
        this.destroy$.complete();
    }

    onAdvanceSearch(form: any) {
        const {fromMiti} = form;
        const {toMiti} = form;
        this.getAdvanceLedger(fromMiti, toMiti);
    }

    getAdvanceLedger(fromMiti, toMiti) {
        this.loading = true;
        this._proxy
            .getSalesReportByDate(fromMiti, toMiti)
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                })
            )
            .subscribe({
                next: (result) => {
                    this.tableRows = result ?? [];
                    this.totalItems = this.tableRows.length;
                    this.displayedRows = this.tableRows;
                },
                error: () => {
                    this.notify.error(this.l('FailedToLoadReport'));
                },
            });
    }

    // getAllProductGroups() {
    //     this._allProxy.getAllProductGroupForTableDropdown().subscribe((data) => {
    //         this.allProductGroups = data;
    //         this.productGroupId = this.allProductGroups[0].id;
    //         this.myForm.get("productGroupId").setValue(this.productGroupId);
    //     });
    // }
    // loadreport() {
    //     this.loading = true;
    //     this._proxy
    //         .getSalesReportByDate("2077/10/10", "2078/12/12", 1)
    //         .pipe(
    //             tap(() => {
    //                 this.confirmationReason = "Loading...";
    //             }),
    //             delay(1000)
    //         )
    //         .subscribe((result) => {
    //             this.loading = false;
    //             this.tableRows = result;
    //
    //             this.totalItems = result.length;
    //             this.displayedRows = result;
    //         });
    // }
    refresh() {
        this.displayedRows = [];
        this.tableRows = [];
        this.totalItems = 0;
        this.markViewForCheck();
    }

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }

    searchFilter(e) {
        const searchStr = e?.target?.value;
        if (searchStr && this.tableRows) {
            this.displayedRows = this.tableRows.filter((type) => {
                return (
                    type.billNo?.toString().toLocaleLowerCase().includes(searchStr.toLocaleLowerCase()) ||
                    type.ledgerName?.toLocaleLowerCase().includes(searchStr.toLocaleLowerCase()) ||
                    type.grandTotal?.toString().toLocaleLowerCase().includes(searchStr.toLocaleLowerCase()) ||
                    type.details?.some((detail) => detail.product?.toLocaleLowerCase().includes(searchStr.toLocaleLowerCase()))
                );
                // return type.productName.toLowerCase().search(searchStr.toLowerCase()) !== -1;
            });
        } else {
            this.displayedRows = this.tableRows || [];
        }
    }

    resetSearch(): void {
        this.displayedRows = this.tableRows;
        this.searchTerm = '';
    }

    private loadFinancialYearAndReport(): void {
        this._allProxy
            .getFinancialYears()
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (data) => {
                    this.myForm.patchValue({
                        fromMiti: data.fromMiti || this.myForm.get('fromMiti').value,
                        toMiti: data.toMiti || this.myForm.get('toMiti').value,
                    });
                    this.getAdvanceLedger(this.myForm.get('fromMiti').value, this.myForm.get('toMiti').value);
                },
                error: () => {
                    this.getAdvanceLedger(this.myForm.get('fromMiti').value, this.myForm.get('toMiti').value);
                },
            });
    }

    openDialog(dialog: TemplateRef<any>): void {
        this.modalRef = this.modalService.show(dialog);
    }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }
}
