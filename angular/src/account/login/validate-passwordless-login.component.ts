import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Subscription } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { timer } from 'rxjs';
import { LoginService } from './login.service';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import {
    AccountServiceProxy,
    PasswordlessAuthenticateModel,
    VerifyPasswordlessLoginCodeInput,
} from '@shared/service-proxies/service-proxies';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './validate-passwordless-login.component.html',
    styleUrls: ['./validate-passwordless-login.component.less'],
    animations: [accountModuleAnimation()],
    imports: [FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ValidatePasswordlessLoginComponent extends AppComponentBase implements OnInit, OnDestroy, AfterViewInit {
    loginService = inject(LoginService);
    private _router = inject(Router);
    private _accountService = inject(AccountServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    private _activatedRoute = inject(ActivatedRoute);
    @ViewChild('passwordlessLoginCodeForm') passwordlessLoginCodeForm: NgForm;
    submitting = false;
    remainingSeconds = 90;
    timerSubscription: Subscription;
    selectedProviderInputValue: string;
    selectedProvider: string;
    passwordlessCode: string;
    isSubmitButtonDisabled = true;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.remainingSeconds = this.appSession.application.passwordlessLoginCodeExpireSeconds;
        const timerSource = timer(1000, 1000);
        this.timerSubscription = timerSource.subscribe(() => {
            this.remainingSeconds = this.remainingSeconds - 1;
            if (this.remainingSeconds === 0) {
                this.message.warn(this.l('TimeoutPleaseTryAgain')).then(() => {
                    this._router.navigate(['account/login']);
                });
            }
        });
        this._activatedRoute.queryParams.subscribe((params) => {
            this.selectedProviderInputValue = params['selectedProviderInputValue'];
            this.selectedProvider = params['selectedProvider'];
            console.log(this.selectedProvider);
        });
    }
    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
        // Track form validity changes
        if (this.passwordlessLoginCodeForm) {
            this.passwordlessLoginCodeForm.form.valueChanges.subscribe(() => {
                this.isSubmitButtonDisabled = !this.passwordlessLoginCodeForm.form.valid;
            });
            // Set initial state
            this.isSubmitButtonDisabled = !this.passwordlessLoginCodeForm.form.valid;
        }
    }
    ngOnDestroy(): void {
        if (this.timerSubscription) {
            this.timerSubscription.unsubscribe();
            this.timerSubscription = null;
        }
    }
    submit(): void {
        const model = new VerifyPasswordlessLoginCodeInput();
        model.providerValue = this.selectedProviderInputValue;
        model.code = this.passwordlessCode;
        this._accountService.verifyPasswordlessLoginCode(model).subscribe(() => {
            const recaptchaCallback = (token: string) => {
                this.showMainSpinner();
                this.submitting = true;
                const passwordlessAuthenticateModel = new PasswordlessAuthenticateModel();
                passwordlessAuthenticateModel.providerValue = this.selectedProviderInputValue;
                passwordlessAuthenticateModel.provider = this.selectedProvider;
                passwordlessAuthenticateModel.verificationCode = this.passwordlessCode;
                passwordlessAuthenticateModel.singleSignIn = UrlHelper.getSingleSignIn();
                passwordlessAuthenticateModel.captchaResponse = token;
                this.loginService.passwordlessAuthenticate(
                    () => {
                        this.submitting = false;
                        this.hideMainSpinner();
                    },
                    passwordlessAuthenticateModel,
                    null,
                    token,
                );
            };
            if (this._recaptchaWrapperService.useCaptchaOnLogin()) {
                this._recaptchaWrapperService.execute('login').subscribe((token) => recaptchaCallback(token));
            } else {
                recaptchaCallback(null);
            }
        });
    }
}
