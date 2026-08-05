import { Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { PaymentServiceProxy, SubscriptionPaymentProductDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { PermissionTreeComponent } from '../shared/permission-tree.component';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef } from 'ag-grid-community';
@Component({
    selector: 'showDetailModal',
    templateUrl: './show-detail-modal.component.html',
    imports: [AppBsModalDirective, LocalizePipe, AgGridAngular],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ShowDetailModalComponent extends AppComponentBase {
    private _paymentService = inject(PaymentServiceProxy);
    @ViewChild('showDetailModal', { static: true }) modal: ModalDirective;
    @ViewChild('permissionTree') permissionTree: PermissionTreeComponent;
    products: SubscriptionPaymentProductDto[];
    extraProperties: string;
    resettingPermissions = false;
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        flex: 1,
    };
    productColumnDefs: ColDef[] = [
        { headerName: '#', field: 'count', width: 90, flex: 0 },
        { headerName: this.l('Item'), field: 'description', minWidth: 220 },
        {
            headerName: this.l('Amount'),
            field: 'amount',
            valueFormatter: (params) => this.formatCurrency(params.value),
            type: 'rightAligned',
        },
        {
            headerName: this.l('TotalAmount'),
            valueGetter: (params) => (params.data?.count || 0) * (params.data?.amount || 0),
            valueFormatter: (params) => this.formatCurrency(params.value),
            type: 'rightAligned',
        },
    ];

    show(paymentId: number): void {
        this._paymentService.getPayment(paymentId).subscribe((result) => {
            this.products = result.subscriptionPaymentProducts;
            this.modal.show();
        });
    }
    close(): void {
        this.modal.hide();
    }

    private formatCurrency(value: any): string {
        const amount = value === null || value === undefined || value === '' ? 0 : Number(value);
        return `${this.appSession.application.currencySign}${amount.toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })}`;
    }
}
