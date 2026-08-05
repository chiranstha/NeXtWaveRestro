import { AfterViewInit, Component, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountServiceProxy,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    ResetPasswordOutput,
    ResolveTenantIdInput,
} from '@shared/service-proxies/service-proxies';
import { LoginService } from '../login/login.service';
import { ResetPasswordModel } from './reset-password.model';
import { finalize } from 'rxjs/operators';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { EqualValidator } from '../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../shared/utils/validation/password-complexity-validator.directive';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './reset-password.component.html',
    animations: [accountModuleAnimation()],
    imports: [FormsModule, EqualValidator, PasswordComplexityValidator, RouterLink, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ResetPasswordComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    private _loginService = inject(LoginService);
    private _profileService = inject(ProfileServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    @ViewChild('resetPassForm') resetPassForm: NgForm;
    model: ResetPasswordModel = new ResetPasswordModel();
    passwordComplexitySetting: PasswordComplexitySetting = new PasswordComplexitySetting();
    saving = false;
    isSubmitButtonDisabled = true;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting = result.setting;
        });
        if (this._activatedRoute.snapshot.queryParams['c']) {
            this.model.c = this._activatedRoute.snapshot.queryParams['c'];
            this._accountService
                .resolveTenantId(new ResolveTenantIdInput({ c: this.model.c }))
                .subscribe((tenantId) => {
                    this.appSession.changeTenantIfNeeded(tenantId);
                });
        } else {
            this.model.userId = this._activatedRoute.snapshot.queryParams['userId'];
            this.model.resetCode = this._activatedRoute.snapshot.queryParams['resetCode'];
            this.appSession.changeTenantIfNeeded(
                this.parseTenantId(this._activatedRoute.snapshot.queryParams['tenantId']),
            );
        }
    }
    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnRegister();
        // Track form validity changes
        if (this.resetPassForm) {
            this.resetPassForm.form.valueChanges.subscribe(() => {
                this.isSubmitButtonDisabled = !this.resetPassForm.form.valid;
            });
            // Set initial state
            this.isSubmitButtonDisabled = !this.resetPassForm.form.valid;
        }
    }
    save(): void {
        this.saving = true;
        this._accountService
            .resetPassword(this.model)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe((result: ResetPasswordOutput) => {
                if (!result.canLogin) {
                    this._router.navigate(['account/login']);
                    return;
                }
                const recaptchaCallback = (token: string) => {
                    // Autheticate
                    this.saving = true;
                    this._loginService.authenticateModel.userNameOrEmailAddress = result.userName;
                    this._loginService.authenticateModel.password = this.model.password;
                    this._loginService.authenticate(
                        () => {
                            this.saving = false;
                        },
                        null,
                        token,
                    );
                };
                if (this._recaptchaWrapperService.useCaptchaOnResetPassword()) {
                    this._recaptchaWrapperService.execute('login').subscribe((token) => recaptchaCallback(token));
                } else {
                    recaptchaCallback(null);
                }
            });
    }
    parseTenantId(tenantIdAsStr?: string): number {
        let tenantId = !tenantIdAsStr ? undefined : parseInt(tenantIdAsStr, 10);
        if (Number.isNaN(tenantId)) {
            tenantId = undefined;
        }
        return tenantId;
    }
}
