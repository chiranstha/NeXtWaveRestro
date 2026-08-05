import {
    Component,
    ElementRef,
    EventEmitter,
    Output,
    ViewChild,
    Input,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy, VerifySmsCodeInputDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'smsVerificationModal',
    templateUrl: './sms-verification-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class SmsVerificationModalComponent extends AppComponentBase {
    private _profileService = inject(ProfileServiceProxy);
    @ViewChild('nameInput', { static: true }) nameInput: ElementRef;
    @ViewChild('smsVerificationModal', { static: true }) modal: ModalDirective;
    @Input() newPhoneNumber: string;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    public active = false;
    public saving = false;
    public verifyCodeInput: VerifySmsCodeInputDto = new VerifySmsCodeInputDto();

    show(): void {
        this.active = true;
        this.modal.show();
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
    save(): void {
        this.saving = true;
        this.verifyCodeInput.phoneNumber = this.newPhoneNumber;
        this._profileService
            .verifySmsCode(this.verifyCodeInput)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                this.close();
                this.modalSave.emit();
            });
    }
}
