import { Component, ViewChild, AfterViewInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountServiceProxy, SendPasswordResetCodeInput } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './forgot-password.component.html',
    animations: [accountModuleAnimation()],
    imports: [
        FormsModule,
        AutoFocusDirective,
        ValidationMessagesComponent,
        RouterLink,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ForgotPasswordComponent extends AppComponentBase implements AfterViewInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);
    @ViewChild('forgotPassForm') forgotPassForm: NgForm;
    model: SendPasswordResetCodeInput = new SendPasswordResetCodeInput();
    saving = false;
    isSubmitButtonDisabled = true;
    constructor() {
        super();
    }
    ngAfterViewInit(): void {
        // Track form validity changes
        if (this.forgotPassForm) {
            this.forgotPassForm.form.valueChanges.subscribe(() => {
                this.isSubmitButtonDisabled = !this.forgotPassForm.form.valid;
            });
            // Set initial state
            this.isSubmitButtonDisabled = !this.forgotPassForm.form.valid;
        }
    }
    save(): void {
        this.saving = true;
        this._accountService
            .sendPasswordResetCode(this.model)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                this.message.success(this.l('PasswordResetMailSentMessage'), this.l('MailSent')).then(() => {
                    this._router.navigate(['account/login']);
                });
            });
    }
}
