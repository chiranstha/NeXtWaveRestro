import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormControl } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    LedgerwiseMonthlySalesDto,
    MonthlyPurchaseReportServiceProxy,
    ReportingServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { delay, tap } from 'rxjs/operators';
import { ColDef } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-product-wise-monthly-purchase',
    templateUrl: './productWiseMonthlyPurchase.component.html',
})
export class ProductWiseMonthlyPurchaseComponent extends AppComponentBase implements OnInit {
    loading = false;
    displayedTableRows: LedgerwiseMonthlySalesDto[];
    tableRows: LedgerwiseMonthlySalesDto[];
    tableData: any;
    allProductGroup$: Observable<UniversalDropdownDto[]>;
    productGroupId = new FormControl(this.emptyGuId);
    advancedFiltersAreShown = false;
    pinnedBottomRowData: any[] = [];
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

    // total Variable for total calculation
    totalShrawan: number;
    totalBhadra: number;
    totalAshoj: number;
    totalFirstQuarter: number;
    totalKartik: number;
    totalMangsir: number;
    totalPoush: number;
    totalSecondQuarter: number;
    totalMagh: number;
    totalFalgun: number;
    totalChaitra: number;
    totalThirdQuarter: number;
    totalBaisakh: number;
    totalJestha: number;
    totalAsadh: number;
    totalYearly: number;

    constructor(
        injector: Injector,
        private _reportProxy : ReportingServiceProxy,
        private _proxy: MonthlyPurchaseReportServiceProxy,
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
        this._proxy.getProductwiseReport(id).pipe(
            tap(() => {

            }),
            delay(200)
        )
            .subscribe((result) => {
                this.loading = false;

                this.displayedTableRows = result;
                this.tableRows = result;
                this.tableData = this.tableRows;
                this.calculateTotal();

            });
    }

    getAllProductGroups() {
        this.allProductGroup$ = this._reportProxy.getAllProductGroupForTableDropdown();
    }

    searchFilter(e) {
        const searchStr = typeof e === 'string' ? e : e?.target?.value;
        if (searchStr && this.tableRows) {
            this.displayedTableRows = this.tableRows.filter((type) => type.ledgerName.toLowerCase().search(searchStr.toLowerCase()) !== -1);
        } else {
            this.displayedTableRows = this.tableRows || [];
        }

    }

    openAdvanceFilter($event) {
        if ($event === true) {
            this.advancedFiltersAreShown = true;
        } else {
            this.advancedFiltersAreShown = false;
        }
    }

    calculateTotal() {
        this.totalShrawan = this.tableData.reduce((sum, item) => sum + item.details.shrawan, 0);
        this.totalBhadra = this.tableData.reduce((sum, item) => sum + item.details.bhadra, 0);
        this.totalAshoj = this.tableData.reduce((sum, item) => sum + item.details.shrawan, 0);
        this.totalFirstQuarter = this.tableData.reduce((sum, item) => sum + item.details.firstQuarter, 0);

        this.totalKartik = this.tableData.reduce((sum, item) => sum + item.details.kartik, 0);
        this.totalMangsir = this.tableData.reduce((sum, item) => sum + item.details.mangsir, 0);
        this.totalPoush = this.tableData.reduce((sum, item) => sum + item.details.poush, 0);
        this.totalSecondQuarter = this.tableData.reduce((sum, item) => sum + item.details.secondQuarter, 0);

        this.totalMagh = this.tableData.reduce((sum, item) => sum + item.details.magh, 0);
        this.totalFalgun = this.tableData.reduce((sum, item) => sum + item.details.falgun, 0);
        this.totalChaitra = this.tableData.reduce((sum, item) => sum + item.details.chaitra, 0);
        this.totalThirdQuarter = this.tableData.reduce((sum, item) => sum + item.details.thirdQuarter, 0);

        this.totalBaisakh = this.tableData.reduce((sum, item) => sum + item.details.baisakh, 0);
        this.totalJestha = this.tableData.reduce((sum, item) => sum + item.details.jestha, 0);
        this.totalAsadh = this.tableData.reduce((sum, item) => sum + item.details.asar, 0);
        this.totalYearly = this.tableData.reduce((sum, item) => sum + item.details.yearly, 0);
        this.pinnedBottomRowData = [
            {
                ledgerName: 'Total',
                details: {
                    shrawan: this.totalShrawan,
                    bhadra: this.totalBhadra,
                    aswin: this.totalAshoj,
                    firstQuarter: this.totalFirstQuarter,
                    kartik: this.totalKartik,
                    mangsir: this.totalMangsir,
                    poush: this.totalPoush,
                    secondQuarter: this.totalSecondQuarter,
                    magh: this.totalMagh,
                    falgun: this.totalFalgun,
                    chaitra: this.totalChaitra,
                    thirdQuarter: this.totalThirdQuarter,
                    baisakh: this.totalBaisakh,
                    jestha: this.totalJestha,
                    asar: this.totalAsadh,
                    yearly: this.totalYearly,
                },
            },
        ];
    }

    private formatGridNumber(value: any): string {
        return value === null || value === undefined || value === '' ? '' : Number(value).toLocaleString();
    }
}
