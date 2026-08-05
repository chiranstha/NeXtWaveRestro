import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { CanActivate, Router } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Subscription } from 'rxjs';
import { timer } from 'rxjs';
import { LoginService } from './login.service';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './validate-two-factor-code.component.html',
    styleUrls: ['./validate-two-factor-code.component.less'],
    animations: [accountModuleAnimation()],
    imports: [FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ValidateTwoFactorCodeComponent
    extends AppComponentBase
    implements CanActivate, OnInit, OnDestroy, AfterViewInit
{
    loginService = inject(LoginService);
    private _router = inject(Router);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    @ViewChild('twoFactorForm') twoFactorForm: NgForm;
    code: string;
    submitting = false;
    remainingSeconds = 90;
    timerSubscription: Subscription;
    isSubmitButtonDisabled = true;

    canActivate(): boolean {
        if (this.loginService.authenticateModel && this.loginService.authenticateResult) {
            return true;
        }
        return false;
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        if (!this.canActivate()) {
            this._router.navigate(['account/login']);
            return;
        }
        this.remainingSeconds = this.appSession.application.twoFactorCodeExpireSeconds;
        const timerSource = timer(1000, 1000);
        this.timerSubscription = timerSource.subscribe(() => {
            this.remainingSeconds = this.remainingSeconds - 1;
            if (this.remainingSeconds === 0) {
                this.message.warn(this.l('TimeoutPleaseTryAgain')).then(() => {
                    this.loginService.authenticateModel.twoFactorVerificationCode = null;
                    this._router.navigate(['account/login']);
                });
            }
        });
    }
    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
        // Track form validity changes
        if (this.twoFactorForm) {
            this.twoFactorForm.form.valueChanges.subscribe(() => {
                this.isSubmitButtonDisabled = !this.twoFactorForm.form.valid;
            });
            // Set initial state
            this.isSubmitButtonDisabled = !this.twoFactorForm.form.valid;
        }
    }
    ngOnDestroy(): void {
        if (this.timerSubscription) {
            this.timerSubscription.unsubscribe();
            this.timerSubscription = null;
        }
    }
    submit(): void {
        const recaptchaCallback = (token: string) => {
            this.loginService.authenticateModel.twoFactorVerificationCode = this.code;
            this.loginService.authenticate(() => {}, null, token);
        };
        if (this._recaptchaWrapperService.useCaptchaOnLogin()) {
            this._recaptchaWrapperService.execute('login').subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
}
