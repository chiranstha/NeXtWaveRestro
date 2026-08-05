import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { VATSummaryReportServiceProxy } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-vat-summary-new',
    templateUrl: './vat-summary-new.component.html',
    styleUrls: ['./vat-summary-new.component.css']
})
export class VatSummaryNewComponent extends AppComponentBase implements OnInit {

    // any
    displayedTableRows: any;
    tableRows: any;

    // boolean
    loading: false;

    // string
    confirmationString: string;

    // AG Grid properties
    salesGridApi: GridApi;
    purchaseGridApi: GridApi;
    summaryGridApi: GridApi;

    salesRowData: any[] = [];
    purchaseRowData: any[] = [];
    summaryRowData: any[] = [];

    // Column definitions for sales table
    salesColumnDefs: ColDef[] = [
        {
            headerName: 'Sales',
            field: 'category',
            pinned: 'left',
            width: 200,
            cellClass: 'fw-bold'
        },
        { headerName: 'Shrawan', field: 'shrawan', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Bhadra', field: 'bhadra', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Aswin', field: 'asoj', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Kartik', field: 'kartik', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Mangsir', field: 'mangsir', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Poush', field: 'poush', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Magh', field: 'magh', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Falgun', field: 'falgun', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Chaitra', field: 'chaitra', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Baisakh', field: 'baisakh', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Jestha', field: 'jestha', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Ashad', field: 'asar', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Total', field: 'total', width: 120, valueFormatter: (params) => this.numberFormatter(params), cellClass: 'fw-bold' }
    ];

    // Column definitions for purchase table
    purchaseColumnDefs: ColDef[] = [
        {
            headerName: 'Purchase',
            field: 'category',
            pinned: 'left',
            width: 200,
            cellClass: 'fw-bold'
        },
        { headerName: 'Shrawan', field: 'shrawan', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Bhadra', field: 'bhadra', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Aswin', field: 'asoj', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Kartik', field: 'kartik', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Mangsir', field: 'mangsir', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Poush', field: 'poush', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Magh', field: 'magh', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Falgun', field: 'falgun', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Chaitra', field: 'chaitra', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Baisakh', field: 'baisakh', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Jestha', field: 'jestha', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Ashad', field: 'asar', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Total', field: 'total', width: 120, valueFormatter: (params) => this.numberFormatter(params), cellClass: 'fw-bold' }
    ];

    // Column definitions for summary table
    summaryColumnDefs: ColDef[] = [
        {
            headerName: '',
            field: 'category',
            pinned: 'left',
            width: 250,
            cellClass: 'fw-bold'
        },
        { headerName: 'Shrawan', field: 'shrawan', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Bhadra', field: 'bhadra', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Aswin', field: 'asoj', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Kartik', field: 'kartik', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Mangsir', field: 'mangsir', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Poush', field: 'poush', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Magh', field: 'magh', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Falgun', field: 'falgun', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Chaitra', field: 'chaitra', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Baisakh', field: 'baisakh', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Jestha', field: 'jestha', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Ashad', field: 'asar', width: 120, valueFormatter: (params) => this.numberFormatter(params) },
        { headerName: 'Total', field: 'total', width: 120, valueFormatter: (params) => this.numberFormatter(params), cellClass: 'fw-bold' }
    ];

    defaultColDef: ColDef = {
        sortable: false,
        filter: false,
        resizable: true,
        suppressMovable: true
    };

    constructor(injector: Injector,
        private _proxy: VATSummaryReportServiceProxy,
        private _fileDownloadService: FileDownloadService) {
        super(injector)
    }

    ngOnInit() {
        this.loadReport();
    }

    loadReport() {
        this._proxy.finalReport().subscribe((result) => {
            this.displayedTableRows = result;
            this.tableRows = result;
            this.prepareGridData(result);
        })
    }

    prepareGridData(data: any) {
        if (!data) {return;}

        // Prepare sales data
        this.salesRowData = [
            {
                category: 'Taxable Sales',
                ...data.taxableSales,
                isTotal: false
            },
            {
                category: 'salesVat',
                ...data.salesVat,
                isTotal: false
            },
            {
                category: 'Exempt Sales',
                ...data.exemptSales,
                isTotal: false
            },
            {
                category: 'Zero VAT Sales',
                ...data.zeroVatSales,
                isTotal: false
            },
            {
                category: 'Total Sales',
                ...data.totalSales,
                isTotal: true
            },
            {
                category: 'Credit Note Total',
                ...data.creditNoteTotal,
                isTotal: false
            },
            {
                category: 'Credit Note VAT',
                ...data.creditNoteVat,
                isTotal: false
            },
            {
                category: 'Net Sales',
                ...data.netSales,
                isTotal: true
            },
            {
                category: 'Net VAT Collection on Sales',
                ...data.netVatCollectionOnSales,
                isTotal: true
            }
        ];

        // Prepare purchase data
        this.purchaseRowData = [
            {
                category: 'Taxable Purchase',
                ...data.taxablePurchase,
                isTotal: false
            },
            {
                category: 'VAT',
                ...data.purchaseVat,
                isTotal: false
            },
            {
                category: 'Capital Purchase',
                ...data.capitalPurchase,
                isTotal: false
            },
            {
                category: 'Capital Purchase VAT',
                ...data.capitalPurchaseVat,
                isTotal: false
            },
            {
                category: 'Exempt Purchase',
                ...data.exemptPurchase,
                isTotal: false
            },
            {
                category: 'Total Purchase',
                ...data.totalPurchase,
                isTotal: true
            },
            {
                category: 'Debit Note Total',
                ...data.debitNoteTotal,
                isTotal: false
            },
            {
                category: 'Debit Note VAT',
                ...data.debitNoteVat,
                isTotal: false
            },
            {
                category: 'Net Purchase',
                ...data.netPurchase,
                isTotal: true
            },
            {
                category: 'Net VAT Paid on Purchase',
                ...data.netVatPaidOnPurchase,
                isTotal: true
            }
        ];

        // Prepare summary data
        this.summaryRowData = [
            {
                category: 'Net Opening For The Month',
                ...data.netOpeningForTheMonth,
                isTotal: true
            },
            {
                category: 'Net VAT Payable/Receivable',
                ...data.netVatForTheMonth,
                isTotal: true
            }
        ];
    }

    numberFormatter(params: any): string {
        if (params.value == null || params.value === '') {return '';}
        return this.numberWithCommas(params.value);
    }

    onSalesGridReady(params: GridReadyEvent) {
        this.salesGridApi = params.api;
        params.api.sizeColumnsToFit();
    }

    onPurchaseGridReady(params: GridReadyEvent) {
        this.purchaseGridApi = params.api;
        params.api.sizeColumnsToFit();
    }

    onSummaryGridReady(params: GridReadyEvent) {
        this.summaryGridApi = params.api;
        params.api.sizeColumnsToFit();
    }

    getRowClass(params: any): string {
        if (params.data.isTotal) {
            return 'fw-bold';
        }
        return '';
    }

    numberWithCommas(x) {
        return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    exportToExcel(e) {
        if (e) {
            this._proxy.createVatSummeryReportToExcel().subscribe((res) => {
                this._fileDownloadService.downloadTempFile(res);
            })
        }
    }
}
