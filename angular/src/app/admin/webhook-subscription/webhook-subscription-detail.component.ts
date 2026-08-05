import { Component, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { DateTime } from 'luxon';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ActivateWebhookSubscriptionInput,
    WebhookSendAttemptServiceProxy,
    WebhookSubscription,
    WebhookSubscriptionServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { firstValueFrom } from 'rxjs';
import { CreateOrEditWebhookSubscriptionModalComponent } from './create-or-edit-webhook-subscription-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { BreadcrumbItem } from '@app/shared/common/sub-header/sub-header.component';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { BsDropdownDirective, BsDropdownToggleDirective, BsDropdownMenuDirective } from 'ngx-bootstrap/dropdown';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './webhook-subscription-detail.component.html',
    styleUrls: ['./webhook-subscription-detail.component.css'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        BsDropdownDirective,
        BsDropdownToggleDirective,
        BsDropdownMenuDirective,
        BusyIfDirective,
        AgGridAngular,
        CreateOrEditWebhookSubscriptionModalComponent,
        ModalDirective,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class WebhookSubscriptionDetailComponent extends AppComponentBase implements OnInit {
    private _webhookSubscriptionService = inject(WebhookSubscriptionServiceProxy);
    private _webhookSendAttemptService = inject(WebhookSendAttemptServiceProxy);
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    @ViewChild('createOrEditWebhookSubscriptionModal', { static: true })
    createOrEditWebhookSubscriptionModal: CreateOrEditWebhookSubscriptionModalComponent;
    @ViewChild('detailModal', { static: true }) detailModal: ModalDirective;
    subscriptionId = '';
    subscription: WebhookSubscription;
    loading = true;
    isSecretBlurActive = true;
    objectKeys = Object.keys;
    listMaxDataLength = 100;
    detailModalText = '';
    // AG Grid properties
    gridApi!: GridApi;
    rowData: any[] = [];
    gridLoading = false;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Actions'),
            field: 'actions',
            cellRenderer: (params: any) => {
                const container = document.createElement('div');
                container.className = 'btn-group dropdown';
                const button = document.createElement('button');
                button.className = 'btn btn-sm btn-primary dropdown-toggle';
                button.setAttribute('data-bs-toggle', 'dropdown');
                button.innerHTML = '<i class="fa fa-cog"></i> <span class="caret"></span>';
                container.appendChild(button);
                const menu = document.createElement('ul');
                menu.className = 'dropdown-menu';
                // Resend option
                const resendItem = document.createElement('li');
                const resendLink = document.createElement('a');
                resendLink.className = 'dropdown-item';
                resendLink.href = 'javascript:;';
                resendLink.innerText = this.l('Resend');
                resendLink.onclick = () => this.resend(params.data.id);
                resendItem.appendChild(resendLink);
                menu.appendChild(resendItem);
                // View webhook event option
                const viewItem = document.createElement('li');
                const viewLink = document.createElement('a');
                viewLink.className = 'dropdown-item';
                viewLink.href = 'javascript:;';
                viewLink.innerText = this.l('ViewWebhookEvent');
                viewLink.onclick = () => this.goToWebhookDetail(params.data.webhookEventId);
                viewItem.appendChild(viewLink);
                menu.appendChild(viewItem);
                container.appendChild(menu);
                return container;
            },
            width: 100,
            sortable: false,
            filter: false,
            suppressMovable: true,
        },
        {
            headerName: this.l('WebhookEvent'),
            field: 'webhookName',
            sortable: false,
            filter: false,
            minWidth: 150,
        },
        {
            headerName: this.l('WebhookEventId'),
            field: 'webhookEventId',
            sortable: false,
            filter: false,
            minWidth: 200,
        },
        {
            headerName: this.l('CreationTime'),
            field: 'creationTime',
            sortable: false,
            filter: false,
            minWidth: 180,
            valueFormatter: (params) => {
                if (!params.value) {
                    return '';
                }
                return DateTime.fromISO(params.value).toFormat('yyyy-LL-dd HH:mm:ss');
            },
        },
        {
            headerName: this.l('HttpStatusCode'),
            field: 'responseStatusCode',
            sortable: false,
            filter: false,
            width: 130,
            cellStyle: { textAlign: 'center' },
        },
        {
            headerName: this.l('Response'),
            field: 'response',
            cellRenderer: (params: any) => {
                if (!params.value) {
                    return '';
                }
                if (params.value.length <= this.listMaxDataLength) {
                    const span = document.createElement('span');
                    span.innerText = params.value;
                    return span;
                } else {
                    const button = document.createElement('button');
                    button.className = 'btn btn-sm btn-outline-primary';
                    button.innerText = this.l('ShowResponse');
                    button.onclick = () => this.showDetailModal(params.value);
                    return button;
                }
            },
            sortable: false,
            filter: false,
            minWidth: 150,
        },
        {
            headerName: this.l('Data'),
            field: 'data',
            cellRenderer: (params: any) => {
                if (!params.value) {
                    return '';
                }
                if (params.value.length <= this.listMaxDataLength) {
                    const span = document.createElement('span');
                    span.innerText = params.value;
                    return span;
                } else {
                    const hiddenDiv = document.createElement('div');
                    hiddenDiv.className = 'd-none';
                    hiddenDiv.innerText = params.value;
                    const button = document.createElement('button');
                    button.className = 'btn btn-sm btn-outline-primary';
                    button.innerText = this.l('ShowData');
                    button.onclick = () => this.showDetailModal(params.value);
                    const container = document.createElement('span');
                    container.appendChild(hiddenDiv);
                    container.appendChild(button);
                    return container;
                }
            },
            sortable: false,
            filter: false,
            minWidth: 150,
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        suppressMovable: true,
    };
    breadcrumbs: BreadcrumbItem[] = [
        new BreadcrumbItem(this.l('WebhookSubscriptions'), '/app/admin/webhook-subscriptions'),
        new BreadcrumbItem(this.l('WebhookSubscriptionDetail')),
    ];

    ngOnInit() {
        this.subscriptionId = this._activatedRoute.snapshot.queryParams['id'];
        this.getDetail();
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.getSendAttempts();
    }
    getSendAttempts(): void {
        this.gridLoading = true;
        firstValueFrom(this._webhookSendAttemptService.getAllSendAttempts(this.subscriptionId, 1000, 0)).then(
            (result) => {
                this.rowData = result.items || [];
                this.gridLoading = false;
            },
        );
    }
    editSubscription(): void {
        this.createOrEditWebhookSubscriptionModal.show();
    }
    getDetail(): void {
        firstValueFrom(this._webhookSubscriptionService.getSubscription(this.subscriptionId)).then((subscription) => {
            this.subscription = subscription;
            this.loading = false;
        });
    }
    toggleActivity(): void {
        const message = this.subscription.isActive
            ? this.l('DeactivateSubscriptionWarningMessage')
            : this.l('ActivateSubscriptionWarningMessage');
        this.message.confirm(message, this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                const input = new ActivateWebhookSubscriptionInput();
                input.subscriptionId = this.subscription.id;
                input.isActive = !this.subscription.isActive;
                firstValueFrom(this._webhookSubscriptionService.activateWebhookSubscription(input)).then(() => {
                    this.subscription.isActive = !this.subscription.isActive;
                });
            }
        });
    }
    viewSecret(): void {
        this.isSecretBlurActive = false;
    }
    goToWebhookDetail(webhookId: string): void {
        this._router.navigate(['app/admin/webhook-event-detail'], {
            queryParams: {
                id: webhookId,
            },
        });
    }
    resend(id: string): void {
        this.message.confirm(
            this.l('WebhookEventWillBeSendWithSameParameters'),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this.showMainSpinner();
                    firstValueFrom(this._webhookSendAttemptService.resend(id)).then(
                        () => {
                            abp.notify.success(this.l('WebhookSendAttemptInQueue'));
                            this.hideMainSpinner();
                        },
                        () => {
                            this.hideMainSpinner();
                        },
                    );
                }
            },
        );
    }
    showDetailModal(text): void {
        this.detailModalText = text;
        this.detailModal.show();
    }
}
