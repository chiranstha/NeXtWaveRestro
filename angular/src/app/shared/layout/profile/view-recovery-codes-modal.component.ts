import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ProfileServiceProxy,
    UpdateGoogleAuthenticatorKeyOutput,
    VerifyAuthenticatorCodeInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { RecoveryCodesComponent } from './recovery-codes.component';
import { VerifyCodeModalComponent } from './verify-code-modal.component';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'viewRecoveryCodesModal',
    templateUrl: './view-recovery-codes-modal.component.html',
    imports: [AppBsModalDirective, RecoveryCodesComponent, VerifyCodeModalComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ViewRecoveryCodesModalComponent extends AppComponentBase {
    private _profileService = inject(ProfileServiceProxy);
    @ViewChild('viewRecoveryCodesModal', { static: true }) modal: ModalDirective;
    @ViewChild('verifyCodeModal', { static: true }) verifyCodeModal: VerifyCodeModalComponent;
    @ViewChild('recoveryCodesComponent', { static: true }) recoveryCodesComponent: RecoveryCodesComponent;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    public active = false;
    public saving = false;

    show(): void {
        this.verifyCodeModal.show();
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
    showRecoveryCodes(verifyCodeInput: VerifyAuthenticatorCodeInput): void {
        this.saving = true;
        this._profileService
            .viewRecoveryCodes(verifyCodeInput)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe((result: UpdateGoogleAuthenticatorKeyOutput) => {
                this.recoveryCodesComponent.model = result;
                this.modal.show();
                this.modalSave.emit();
            });
    }
}
