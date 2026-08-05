import { Component, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationInfoDto,
    CreateInvoiceDto,
    EditionWithFeaturesDto,
    InvoiceServiceProxy,
    PaymentPeriodType,
    PaymentServiceProxy,
    SessionServiceProxy,
    StartExtendSubscriptionInput,
    StartUpgradeSubscriptionInput,
    SubscriptionPaymentType,
    SubscriptionServiceProxy,
    SubscriptionStartType,
    TenantLoginInfoDto,
    TenantRegistrationServiceProxy,
    UserLoginInfoDto,
} from '@shared/service-proxies/service-proxies';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { BsDropdownDirective, BsDropdownToggleDirective, BsDropdownMenuDirective } from 'ngx-bootstrap/dropdown';
import { AgGridAngular } from 'ag-grid-angular';
import { ShowDetailModalComponent } from './show-detail-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './subscription-management.component.html',
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        FormsModule,
        NgClass,
        RouterLink,
        BsDropdownDirective,
        BsDropdownToggleDirective,
        BsDropdownMenuDirective,
        AgGridAngular,
        ShowDetailModalComponent,
        LocalizePipe,
    ],
    schemas: [NO_ERRORS_SCHEMA],
})
export class SubscriptionManagementComponent extends AppComponentBase implements OnInit {
    private _sessionService = inject(SessionServiceProxy);
    private _paymentServiceProxy = inject(PaymentServiceProxy);
    private _invoiceServiceProxy = inject(InvoiceServiceProxy);
    private _subscriptionServiceProxy = inject(SubscriptionServiceProxy);
    private _tenantRegistrationAppService = inject(TenantRegistrationServiceProxy);
    private _activatedRoute = inject(ActivatedRoute);
    private _router = inject(Router);
    private _cdr = inject(ChangeDetectorRef);
    subscriptionStartType: typeof SubscriptionStartType = SubscriptionStartType;
    subscriptionPaymentType: typeof SubscriptionPaymentType = SubscriptionPaymentType;
    loading: boolean;
    user: UserLoginInfoDto = new UserLoginInfoDto();
    tenant: TenantLoginInfoDto = new TenantLoginInfoDto();
    application: ApplicationInfoDto = new ApplicationInfoDto();
    filterText = '';
    editions: EditionWithFeaturesDto[];
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
                button.innerHTML = `<i class="fa fa-cog"></i> <span class="caret"></span> ${this.l('Actions')}`;
                container.appendChild(button);
                const menu = document.createElement('ul');
                menu.className = 'dropdown-menu';
                // Detail option
                const detailItem = document.createElement('li');
                const detailLink = document.createElement('a');
                detailLink.className = 'dropdown-item';
                detailLink.href = 'javascript:;';
                detailLink.innerText = this.l('Detail');
                detailLink.onclick = () => this.createOrShowInvoice(params.data.id, params.data.invoiceNo);
                detailItem.appendChild(detailLink);
                menu.appendChild(detailItem);
                // Show Invoice option (if permission)
                if (this.isGranted('Pages.Administration.Users.Edit')) {
                    const invoiceItem = document.createElement('li');
                    const invoiceLink = document.createElement('a');
                    invoiceLink.className = 'dropdown-item';
                    invoiceLink.href = 'javascript:;';
                    invoiceLink.innerText = this.l('ShowInvoice');
                    invoiceLink.onclick = () => this.createOrShowInvoice(params.data.id, params.data.invoiceNo);
                    invoiceItem.appendChild(invoiceLink);
                    menu.appendChild(invoiceItem);
                }
                container.appendChild(menu);
                return container;
            },
            width: 130,
            sortable: false,
            filter: false,
            suppressMovable: true,
        },
        {
            headerName: this.l('ProcessTime'),
            field: 'creationTime',
            sortable: true,
            filter: false,
            width: 150,
            valueFormatter: (params) => {
                if (!params.value) {
                    return '';
                }
                return new Date(params.value).toLocaleString();
            },
        },
        {
            headerName: this.l('Gateway'),
            field: 'gateway',
            sortable: true,
            filter: false,
            width: 150,
            valueFormatter: (params) => {
                if (!params.value) {
                    return '';
                }
                return this.l(`SubscriptionPaymentGatewayType_${params.value}`);
            },
        },
        {
            headerName: this.l('Amount'),
            field: 'totalAmount',
            sortable: true,
            filter: false,
            width: 100,
            valueFormatter: (params) => {
                if (!params.value) {
                    return '';
                }
                return `${this.appSession.application.currencySign} ${params.value.toFixed(2)}`;
            },
        },
        {
            headerName: this.l('Status'),
            field: 'status',
            sortable: true,
            filter: false,
            width: 150,
            valueFormatter: (params) => {
                if (!params.value) {
                    return '';
                }
                return this.l(`SubscriptionPaymentStatus_${params.value}`);
            },
        },
        {
            headerName: this.l('Period'),
            field: 'paymentPeriodType',
            sortable: true,
            filter: false,
            width: 150,
            valueFormatter: (params) => {
                if (!params.value) {
                    return '';
                }
                return this.l(`PaymentPeriodType_${params.value}`);
            },
        },
        {
            headerName: this.l('DayCount'),
            field: 'dayCount',
            sortable: false,
            filter: false,
            width: 100,
        },
        {
            headerName: this.l('PaymentId'),
            field: 'externalPaymentId',
            sortable: false,
            filter: false,
            width: 250,
        },
        {
            headerName: this.l('InvoiceNo'),
            field: 'invoiceNo',
            sortable: true,
            filter: false,
            width: 150,
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        suppressMovable: true,
    };
    constructor() {
        super();
        this.filterText = this._activatedRoute.snapshot.queryParams['filterText'] || '';
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.getSettings();
        this.getPaymentHistory();
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
    }
    createOrShowInvoice(paymentId: number, invoiceNo: string): void {
        if (invoiceNo) {
            window.open(`/app/admin/invoice/${paymentId}`, '_blank');
        } else {
            this._invoiceServiceProxy
                .createInvoice(new CreateInvoiceDto({ subscriptionPaymentId: paymentId }))
                .subscribe(() => {
                    this.getPaymentHistory();
                    window.open(`/app/admin/invoice/${paymentId}`, '_blank');
                });
        }
    }
    getSettings(): void {
        this.loading = true;
        this.appSession.init().then(() => {
            this.loading = false;
            this.user = this.appSession.user;
            this.tenant = this.appSession.tenant;
            this.application = this.appSession.application;
            this._cdr.markForCheck();
        });
        this._tenantRegistrationAppService.getEditionsForSelect().subscribe((result) => {
            this.editions = result.editionsWithFeatures;
        });
    }
    getPaymentHistory(): void {
        this.gridLoading = true;
        this._paymentServiceProxy
            .getPaymentHistory('', 1000, 0)
            .pipe(finalize(() => (this.gridLoading = false)))
            .subscribe((result) => {
                this.rowData = result.items || [];
            });
    }
    disableRecurringPayments() {
        this._subscriptionServiceProxy.disableRecurringPayments().subscribe(() => {
            this.tenant.subscriptionPaymentType = this.subscriptionPaymentType.RecurringManual;
            this._cdr.markForCheck();
        });
    }
    enableRecurringPayments() {
        this._subscriptionServiceProxy.enableRecurringPayments().subscribe(() => {
            this.tenant.subscriptionPaymentType = this.subscriptionPaymentType.RecurringAutomatic;
            this._cdr.markForCheck();
        });
    }
    hasRecurringSubscription(): boolean {
        return this.tenant.subscriptionPaymentType !== this.subscriptionPaymentType.Manual;
    }
    startUpdateSubscription(editionId: number, paymentPeriodType?: string): void {
        const input = new StartUpgradeSubscriptionInput();
        input.targetEditionId = editionId;
        input.paymentPeriodType = PaymentPeriodType[paymentPeriodType];
        input.successUrl = `${abp.appPath}account/upgrade-succeed`;
        input.errorUrl = `${abp.appPath}account/payment-failed`;
        this._subscriptionServiceProxy.startUpgradeSubscription(input).subscribe((result) => {
            if (result.upgraded) {
                this.message.success(this.l('YourAccountIsUpgraded'));
            } else {
                this._router.navigate(['account/gateway-selection'], {
                    queryParams: {
                        paymentId: result.paymentId,
                    },
                });
            }
        });
    }
    startExtendSubscription(): void {
        const input = new StartExtendSubscriptionInput();
        input.successUrl = `${abp.appPath}account/extend-succeed`;
        input.errorUrl = `${abp.appPath}account/payment-failed`;
        this._subscriptionServiceProxy.startExtendSubscription(input).subscribe((paymentId) => {
            this._router.navigate(['account/gateway-selection'], {
                queryParams: {
                    paymentId,
                },
            });
        });
    }
}
