import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit, TemplateRef } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    MaterialSalesReportDto,
    MaterialSalesReportServiceProxy,
    ReportingServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-material-sales-report',
    templateUrl: './materialSalesReport.component.html',
    styleUrls: ['./materialSalesReport.component.css'],
})
export class MaterialSalesReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    public destroy$ = new Subject<void>();
    tableRows: MaterialSalesReportDto[] = [];
    displayedRows: any[] = [];
    searchTerm = '';
    totalItems = 0;
    saving = false;
    myForm: FormGroup;
    advancedFiltersAreShown = false;
    loading = false;
    modalRef?: BsModalRef;
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 140,
    };
    private numberFormatter = (params: any) => this.formatGridNumber(params.value);
    private booleanRenderer = (params: any) => this.formatBooleanIcon(params.value);
    columnDefs: ColDef[] = [
        { headerName: 'Fiscal Year', field: 'fiscalYear' },
        { headerName: 'Customer Name', field: 'customerName', minWidth: 220 },
        { headerName: 'Customer PAN', field: 'customerPan' },
        { headerName: 'Bill Date', field: 'billDate' },
        { headerName: 'Bill No', field: 'billNo' },
        { headerName: 'Payment Method', field: 'paymentMethod' },
        { headerName: 'Gross Amount', field: 'amount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Discount Amount', field: 'discount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Taxable Amount', field: 'taxableAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Tax Amount', field: 'taxAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Grand Total', field: 'totalAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Printed Time', field: 'printedTime' },
        { headerName: 'Entered By', field: 'enteredBy' },
        { headerName: 'Printed By', field: 'printedBy' },
        { headerName: 'Vat Refund Amount', field: 'vatRefundAmount', valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Transaction Id', field: 'transactionId' },
        { headerName: 'Sync IRD', field: 'syncIrd', cellRenderer: this.booleanRenderer, width: 120 },
        { headerName: 'Bill Print', field: 'billPrint', cellRenderer: this.booleanRenderer, width: 120 },
        { headerName: 'Real Time', field: 'realTime', cellRenderer: this.booleanRenderer, width: 120 },
        { headerName: 'Active', field: 'active', cellRenderer: this.booleanRenderer, width: 120 },
    ];

    constructor(
        injector: Injector,
        private _proxy: MaterialSalesReportServiceProxy,
        // private _salesProxy: SalesMastersServiceProxy,
        private _allProxy: ReportingServiceProxy,
        private modalService: BsModalService,
        private _fileDownloadService: FileDownloadService,
        private _fb: FormBuilder
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.createForm();
        this.refreshLoad();
    }

    createForm() {
        this.myForm = this._fb.group({
            fromMiti: [this.fromMiti],
            toMiti: [this.toMiti],
        });
    }

    refreshLoad() {
        this.getFinancialYear(true);
    }

    ngOnDestroy() {
        this.destroy$.next();
        this.destroy$.complete();
    }


    getFinancialYear(loadReport = false) {
        this._allProxy
            .getFinancialYears()
            .pipe(takeUntil(this.destroy$))
            .subscribe((data) => {
                this.myForm.patchValue({
                    fromMiti: data.fromMiti || this.myForm.get('fromMiti').value,
                    toMiti: data.toMiti || this.myForm.get('toMiti').value,
                });
                if (loadReport) {
                    this.loadReport();
                }
                this.markViewForCheck();
            });
    }

    loadReport() {
        this.loading = true;
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        this._proxy
            .getReport(fromMiti, toMiti)
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

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }

    exportToExcel(): void {
        this.loading = true;
        const fromMiti = this.myForm.get('fromMiti').value;
        const toMiti = this.myForm.get('toMiti').value;
        this._proxy
            .createMaterialSalesReportToExcel(fromMiti, toMiti)
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this.loading = false;
                    this.markViewForCheck();
                })
            )
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    searchFilter(e) {
        const searchStr = typeof e === 'string' ? e : e?.target?.value;
        if (searchStr && this.tableRows) {
            this.displayedRows = this.tableRows.filter(
                (type) => type.customerName?.toLowerCase().search(searchStr.toLowerCase()) !== -1
            );
        } else {
            this.displayedRows = this.tableRows || [];
        }
        this.markViewForCheck();
    }

    resetSearch(): void {
        this.displayedRows = this.tableRows;
        this.searchTerm = '';
    }

    openDialog(dialog: TemplateRef<any>): void {
        this.modalRef = this.modalService.show(dialog);
    }

    // syncWithIRD() {
    //     this.saving = true;
    //     this._salesProxy
    //         .syncWithIrd()
    //         .pipe(
    //             finalize(() => {
    //                 this.saving = false;
    //             })
    //         )
    //         .subscribe(() => {
    //             this.notify.success(this.l('SyncedWithIRD'));
    //         });
    // }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString();
    }

    private formatBooleanIcon(value: boolean): string {
        return value
            ? '<i class="pi pi-check-circle text-success" title="True"></i>'
            : '<i class="pi pi-times-circle text-danger" title="False"></i>';
    }
}
