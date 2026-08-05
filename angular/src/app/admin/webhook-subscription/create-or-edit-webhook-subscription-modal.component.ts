import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NameValuePair } from '@shared/utils/name-value-pair';
import { WebhookSubscription, WebhookSubscriptionServiceProxy } from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { KeyValueListManagerComponent } from '@app/shared/common/key-value-list-manager/key-value-list-manager.component';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { AutoComplete } from '@shared/ui-compat';
import { KeyValueListManagerComponent as KeyValueListManagerComponent_1 } from '../../shared/common/key-value-list-manager/key-value-list-manager.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'create-or-edit-webhook-subscription',
    templateUrl: './create-or-edit-webhook-subscription-modal.component.html',
    styleUrls: ['./create-or-edit-webhook-subscription-modal.component.css'],
    imports: [
        ModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        AutoComplete,
        KeyValueListManagerComponent_1,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateOrEditWebhookSubscriptionModalComponent extends AppComponentBase {
    private _webhookSubscriptionService = inject(WebhookSubscriptionServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('headerKeyValueManager') headerKeyValueManager: KeyValueListManagerComponent;
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    objectKeys = Object.keys;
    webhookSubscription: WebhookSubscription = new WebhookSubscription();
    webhookSubscriptionId?: string;
    allWebhooks: NameValuePair[];
    filteredWebhooks: NameValuePair[];
    active = false;
    saving = false;
    webhooks: NameValuePair[] = new Array<NameValuePair>();
    headers: { key: string; value: string }[] = [];
    constructor() {
        super();
        this.getAllWebhooks();
    }
    show(subscriptionId?: string): void {
        this.resetValues();
        this.webhookSubscriptionId = subscriptionId;
        if (!subscriptionId) {
            this.active = true;
            this.modal.show();
            this._cdr.markForCheck();
            return;
        }
        this.showMainSpinner();
        this._webhookSubscriptionService.getSubscription(subscriptionId).subscribe(
            (result) => {
                this.webhookSubscription = result;
                this.webhooks = this.webhookSubscription.webhooks.map(
                    (wh) =>
                        new NameValuePair({
                            name: wh,
                            value: wh,
                        }),
                );
                const keys = Object.keys(this.webhookSubscription.headers);
                if (this.webhookSubscription.headers && keys.length > 0) {
                    this.headers = keys.map((x) => ({
                        key: x,
                        value: this.webhookSubscription.headers[x],
                    }));
                }
                this.hideMainSpinner();
                this.active = true;
                this.modal.show();
                this._cdr.markForCheck();
            },
            () => {
                this.hideMainSpinner();
                this._cdr.markForCheck();
            },
        );
    }
    resetValues(): void {
        this.webhookSubscription = new WebhookSubscription();
        this.webhooks = [];
        this.headers = [];
        this._cdr.markForCheck();
    }
    save(): void {
        this.webhookSubscription.webhooks = this.webhooks.map((wh) => wh.value);
        this.webhookSubscription.headers = {};
        this.headerKeyValueManager.getItems().forEach((item) => {
            this.webhookSubscription.headers[item.key] = item.value;
        });
        let observable: Observable<void>;
        if (!this.webhookSubscriptionId) {
            observable = this._webhookSubscriptionService.addSubscription(this.webhookSubscription);
        } else {
            observable = this._webhookSubscriptionService.updateSubscription(this.webhookSubscription);
        }
        observable.subscribe(
            () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.hideMainSpinner();
                this.modalSave.emit(null);
                this.close();
                this._cdr.markForCheck();
            },
            () => {
                this.hideMainSpinner();
                this._cdr.markForCheck();
            },
        );
    }
    getAllWebhooks(): void {
        this._webhookSubscriptionService.getAllAvailableWebhooks().subscribe((webhooks) => {
            this.allWebhooks = webhooks.items.map(
                (wh) =>
                    new NameValuePair({
                        name: wh.displayName ?? wh.name,
                        value: wh.name,
                    }),
            );
            this._cdr.markForCheck();
        });
    }
    filterWebhooks(event) {
        this.filteredWebhooks = this.allWebhooks.filter(
            (item) =>
                item.name.toLowerCase().includes(event.query.toLowerCase()) ||
                item.value.toLowerCase().includes(event.query.toLowerCase()),
        );
    }
    close(): void {
        this.active = false;
        this.modal.hide();
        this._cdr.markForCheck();
    }
    removeHeader(headerKey: string) {
        const item = document.getElementById(`additional-header-${headerKey}`);
        item.remove();
        delete this.webhookSubscription.headers[headerKey];
    }
}
