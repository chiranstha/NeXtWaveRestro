import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormControl } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Observable, Subject } from 'rxjs';
import {
    LedgerwiseMonthlySalesDto,
    MonthlySalesReportServiceProxy,
    ReportingServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { finalize, takeUntil } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-product-wise-monthly-sales',
    templateUrl: './productWiseMonthlySalesReport.component.html',
    styleUrls: ['./productWiseMonthlySalesReport.component.css'],
})
export class ProductWiseMonthlySalesReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    public destroy$ = new Subject<void>();
    loading = false;
    displayedTableRows: LedgerwiseMonthlySalesDto[] = [];
    tableRows: LedgerwiseMonthlySalesDto[] = [];
    allProductGroup$: Observable<UniversalDropdownDto[]>;
    productGroupId = new FormControl(this.emptyGuId);
    advancedFiltersAreShown = false;
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 120,
    };
    private numberFormatter = (params: any) => this.formatGridNumber(params.value);
    monthlyColumnDefs: ColDef[] = [
        { headerName: 'Product', field: 'ledgerName', pinned: 'left', minWidth: 180 },
        { headerName: 'Shrawan', valueGetter: (params) => params.data?.details?.shrawan, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Bhadra', valueGetter: (params) => params.data?.details?.bhadra, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Aswin', valueGetter: (params) => params.data?.details?.aswin, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'FirstQuarter', valueGetter: (params) => params.data?.details?.firstQuarter, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Kartik', valueGetter: (params) => params.data?.details?.kartik, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Mangsir', valueGetter: (params) => params.data?.details?.mangsir, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Poush', valueGetter: (params) => params.data?.details?.poush, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'SecondQuarter', valueGetter: (params) => params.data?.details?.secondQuarter, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Magh', valueGetter: (params) => params.data?.details?.magh, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Falgun', valueGetter: (params) => params.data?.details?.falgun, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Chaitra', valueGetter: (params) => params.data?.details?.chaitra, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'ThirdQuarter', valueGetter: (params) => params.data?.details?.thirdQuarter, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Baisakh', valueGetter: (params) => params.data?.details?.baisakh, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Jestha', valueGetter: (params) => params.data?.details?.jestha, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Ashad', valueGetter: (params) => params.data?.details?.asar, valueFormatter: this.numberFormatter, type: 'rightAligned' },
        { headerName: 'Yearly', valueGetter: (params) => params.data?.details?.yearly, valueFormatter: this.numberFormatter, type: 'rightAligned' },
    ];

    constructor(
        injector: Injector,
         private _proxy: MonthlySalesReportServiceProxy,
         private _reportProxy: ReportingServiceProxy,
        ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.loadreport(this.emptyGuId);
        this.getAllProductGroups();
    }

    loadreport(id) {
        this.loading = true;
        this._proxy
            .getProductwiseReport(id || this.emptyGuId)
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
                    this.displayedTableRows = this.tableRows;
                },
                error: () => {
                    this.notify.error(this.l('FailedToLoadReport'));
                },
            });
    }

    getAllProductGroups() {
        this.allProductGroup$ = this._reportProxy.getAllProductGroupForTableDropdown();
    }

    searchFilter(e) {
        const searchStr = typeof e === 'string' ? e : e?.target?.value;
        if (searchStr && this.tableRows) {
            this.displayedTableRows = this.tableRows.filter((type) => type.ledgerName?.toLowerCase().search(searchStr.toLowerCase()) !== -1);
        } else {
            this.displayedTableRows = this.tableRows || [];
        }
        this.markViewForCheck();
    }

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }

    numberWithCommas(x) {
        return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString();
    }
}
