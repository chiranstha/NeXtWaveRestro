import { ChangeDetectorRef, Component, NgZone, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { AppComponentBase } from '@shared/common/app-component-base';
import { WebhookSubscriptionServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { CreateOrEditWebhookSubscriptionModalComponent } from './create-or-edit-webhook-subscription-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Router } from '@angular/router';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './webhook-subscription.component.html',
    styleUrls: ['./webhook-subscription.component.css'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        AgGridAngular,
        CreateOrEditWebhookSubscriptionModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class WebhookSubscriptionComponent extends AppComponentBase {
    private _webhookSubscriptionService = inject(WebhookSubscriptionServiceProxy);
    private _router = inject(Router);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @ViewChild('createOrEditWebhookSubscriptionModal', { static: true })
    createOrEditWebhookSubscriptionModal: CreateOrEditWebhookSubscriptionModalComponent;
    // AG Grid properties
    gridApi!: GridApi;
    rowData: any[] = [];
    loading = false;
    columnDefs: ColDef[] = [
        {
            headerName: '',
            field: 'actions',
            cellRenderer: (params: any) => {
                const button = document.createElement('button');
                button.className = 'btn btn-sm btn-primary';
                button.innerText = this.l('Details');
                button.onclick = () => this._zone.run(() => this.goToSubscriptionDetail(params.data.id));
                return button;
            },
            width: 100,
            sortable: false,
            filter: false,
            suppressMovable: true,
        },
        {
            headerName: this.l('WebhookEndpoint'),
            field: 'webhookUri',
            sortable: false,
            filter: false,
            minWidth: 200,
        },
        {
            headerName: this.l('WebhookEvents'),
            field: 'webhooks',
            cellRenderer: (params: any) => {
                if (!params.value || params.value.length === 0) {
                    return '';
                }
                const container = document.createElement('div');
                params.value.forEach((webhook: string) => {
                    const div = document.createElement('div');
                    div.innerText = webhook;
                    container.appendChild(div);
                });
                return container;
            },
            sortable: false,
            filter: false,
            minWidth: 150,
        },
        {
            headerName: this.l('IsActive'),
            field: 'isActive',
            cellRenderer: (params: any) => {
                const span = document.createElement('span');
                span.className = params.value ? 'badge badge-success m-1' : 'badge badge-dark m-1';
                span.innerText = params.value ? this.l('Yes') : this.l('No');
                return span;
            },
            sortable: false,
            filter: false,
            minWidth: 100,
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        suppressMovable: true,
    };

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.getSubscriptions();
    }
    getSubscriptions(): void {
        this.loading = true;
        this._cdr.markForCheck();
        this._webhookSubscriptionService
            .getAllSubscriptions()
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this._cdr.markForCheck();
                }),
            )
            .subscribe((result) => {
                this.rowData = result.items || [];
                this._cdr.markForCheck();
            });
    }
    createSubscription(): void {
        this.createOrEditWebhookSubscriptionModal.show();
    }
    goToSubscriptionDetail(subscriptionId: string): void {
        this._router.navigate(['app/admin/webhook-subscriptions-detail'], {
            queryParams: {
                id: subscriptionId,
            },
        });
    }
}
