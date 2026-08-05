import { Component, Injector, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { DateTime } from 'luxon';
import { GetRecentTenantsOutput, HostDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { BusyIfDirective } from '../../../../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'app-widget-recent-tenants',
    templateUrl: './widget-recent-tenants.component.html',
    styleUrls: ['./widget-recent-tenants.component.css'],
    imports: [BusyIfDirective, AgGridAngular, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class WidgetRecentTenantsComponent extends WidgetComponentBaseComponent {
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);
    loading = true;
    recentTenantsData: GetRecentTenantsOutput;
    // AG Grid properties
    gridApi!: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('TenantName'),
            field: 'name',
            sortable: false,
            filter: false,
            minWidth: 150,
        },
        {
            headerName: this.l('CreationTime'),
            field: 'creationTime',
            sortable: false,
            filter: false,
            minWidth: 150,
            valueFormatter: (params) => {
                if (!params.value) {
                    return '';
                }
                return DateTime.fromISO(params.value).toFormat('F');
            },
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        suppressMovable: true,
    };
    constructor() {
        const injector = inject(Injector);
        super();
        this.loadRecentTenantsData();
    }
    loadRecentTenantsData() {
        this._hostDashboardServiceProxy.getRecentTenantsData().subscribe((data) => {
            this.recentTenantsData = data;
            this.loading = false;
        });
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.gridApi.sizeColumnsToFit();
    }
    gotoAllRecentTenants(): void {
        window.open(
            `${abp.appPath}app/admin/tenants?` +
                `creationDateStart=${encodeURIComponent(this.recentTenantsData.tenantCreationStartDate.toString())}`,
        );
    }
}
