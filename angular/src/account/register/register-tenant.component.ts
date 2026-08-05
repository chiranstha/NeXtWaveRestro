import { AfterViewInit, Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EditionSelectDto,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    RegisterTenantOutput,
    TenantRegistrationServiceProxy,
    PaymentPeriodType,
    SubscriptionPaymentGatewayType,
    SubscriptionStartType,
} from '@shared/service-proxies/service-proxies';
import { RegisterTenantModel } from './register-tenant.model';
import { TenantRegistrationHelperService } from './tenant-registration-helper.service';
import { finalize } from 'rxjs/operators';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { NgClass } from '@angular/common';
import { PasswordDirective } from '@shared/ui-compat';
import { EqualValidator } from '../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../shared/utils/validation/password-complexity-validator.directive';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './register-tenant.component.html',
    animations: [accountModuleAnimation()],
    imports: [
        RouterLink,
        FormsModule,
        AutoFocusDirective,
        ValidationMessagesComponent,
        NgClass,
        PasswordDirective,
        EqualValidator,
        PasswordComplexityValidator,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class RegisterTenantComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _tenantRegistrationService = inject(TenantRegistrationServiceProxy);
    private _router = inject(Router);
    private _profileService = inject(ProfileServiceProxy);
    private _tenantRegistrationHelper = inject(TenantRegistrationHelperService);
    private _activatedRoute = inject(ActivatedRoute);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    model: RegisterTenantModel = new RegisterTenantModel();
    passwordComplexitySetting: PasswordComplexitySetting = new PasswordComplexitySetting();
    subscriptionStartType = SubscriptionStartType;
    paymentPeriodType = PaymentPeriodType;
    selectedPaymentPeriodType: PaymentPeriodType = PaymentPeriodType.Monthly;
    subscriptionPaymentGateway = SubscriptionPaymentGatewayType;
    paymentId = '';
    saving = false;

    ngOnInit() {
        this.model.editionId = this._activatedRoute.snapshot.queryParams['editionId'];
        this.model.paymentPeriodType = this._activatedRoute.snapshot.queryParams['paymentPeriodType'];
        if (this.model.editionId) {
            this.model.subscriptionStartType = this._activatedRoute.snapshot.queryParams['subscriptionStartType'];
        }
        //Prevent to create tenant in a tenant context
        if (this.appSession.tenant != null) {
            this._router.navigate(['account/login']);
            return;
        }
        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting = result.setting;
        });
    }
    ngAfterViewInit() {
        if (this.model.editionId) {
            this._tenantRegistrationService.getEdition(this.model.editionId).subscribe((result: EditionSelectDto) => {
                this.model.edition = result;
            });
        }
        this._recaptchaWrapperService.setCaptchaVisibilityOnRegister();
    }
    save(): void {
        const recaptchaCallback = (token: string) => {
            this.saving = true;
            this.model.captchaResponse = token;
            this.model.successUrl = `${abp.appPath}account/buy-succeed`;
            this.model.errorUrl = `${abp.appPath}account/payment-failed`;
            this._tenantRegistrationService
                .registerTenant(this.model)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result: RegisterTenantOutput) => {
                    this.notify.success(this.l('SuccessfullyRegistered'));
                    this._tenantRegistrationHelper.registrationResult = result;
                    if (parseInt(this.model.subscriptionStartType.toString()) === SubscriptionStartType.Paid) {
                        this._router.navigate(['account/gateway-selection'], {
                            queryParams: {
                                paymentId: result.paymentId,
                            },
                        });
                    } else {
                        this._router.navigate(['account/register-tenant-result']);
                    }
                });
        };
        if (this._recaptchaWrapperService.useCaptchaOnRegister()) {
            this._recaptchaWrapperService.execute('register_tenant').subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
}
