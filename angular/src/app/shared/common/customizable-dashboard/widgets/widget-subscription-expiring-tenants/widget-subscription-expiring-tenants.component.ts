import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { GetExpiringTenantsOutput, HostDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { BusyIfDirective } from '../../../../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'app-widget-subscription-expiring-tenants',
    templateUrl: './widget-subscription-expiring-tenants.component.html',
    styleUrls: ['./widget-subscription-expiring-tenants.component.css'],
    imports: [BusyIfDirective, AgGridAngular, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class WidgetSubscriptionExpiringTenantsComponent extends WidgetComponentBaseComponent implements OnInit {
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);
    dataLoading = true;
    expiringTenantsData: GetExpiringTenantsOutput;
    // AG Grid properties
    gridApi!: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('TenantName'),
            field: 'tenantName',
            sortable: false,
            filter: false,
            minWidth: 150,
        },
        {
            headerName: this.l('RemainingDay'),
            field: 'remainingDayCount',
            sortable: false,
            filter: false,
            minWidth: 120,
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        suppressMovable: true,
    };

    ngOnInit() {
        this.getData();
    }
    getData() {
        this._hostDashboardServiceProxy.getSubscriptionExpiringTenantsData().subscribe((data) => {
            this.expiringTenantsData = data;
            this.dataLoading = false;
        });
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.gridApi.sizeColumnsToFit();
    }
    gotoAllExpiringTenants(): void {
        const url =
            `${abp.appPath}app/admin/tenants?` +
            `subscriptionEndDateStart=${encodeURIComponent(
                this.expiringTenantsData.subscriptionEndDateStart.toString(),
            )}&` +
            `subscriptionEndDateEnd=${encodeURIComponent(this.expiringTenantsData.subscriptionEndDateEnd.toString())}`;
        window.open(url);
    }
}
