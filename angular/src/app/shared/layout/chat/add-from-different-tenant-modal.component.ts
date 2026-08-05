import { Component, EventEmitter, OnInit, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateFriendshipWithDifferentTenantInput,
    FriendshipServiceProxy,
    VerifyAuthenticatorCodeInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'addFromDifferentTenantModal',
    templateUrl: './add-from-different-tenant-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class AddFromDifferentTenantModalComponent extends AppComponentBase implements OnInit {
    private _friendshipService = inject(FriendshipServiceProxy);
    @ViewChild('addFromDifferentTenantModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    public active = false;
    public saving = false;
    public verifyCodeInput: VerifyAuthenticatorCodeInput = new VerifyAuthenticatorCodeInput();
    isHostUser = false;
    tenantToHostChatAllowed = false;
    tenantToTenantChatAllowed = false;
    tenancyName = '';
    userName = '';

    ngOnInit(): void {
        this.tenantToHostChatAllowed =
            this.feature.isEnabled('App.ChatFeature.TenantToHost') && this.appSession.tenantId != null;
        this.tenantToTenantChatAllowed =
            this.feature.isEnabled('App.ChatFeature.TenantToTenant') || this.appSession.tenantId == null;
        this.isHostUser = !this.tenantToTenantChatAllowed;
    }
    show(): void {
        this.active = true;
        this.modal.show();
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
    save(): void {
        const input = new CreateFriendshipWithDifferentTenantInput();
        input.tenancyName = this.tenancyName;
        input.userName = this.userName;
        this.saving = true;
        this._friendshipService
            .createFriendshipWithDifferentTenant(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                this.notify.info(this.l('FriendshipRequestAccepted'));
                this.close();
                this.modalSave.emit(null);
            });
    }
    switchTenant(): void {
        this.tenancyName = '';
    }
}
