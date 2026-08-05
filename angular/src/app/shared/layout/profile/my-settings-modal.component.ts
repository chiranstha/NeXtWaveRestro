import { Component, EventEmitter, Output, ViewChild, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CurrentUserProfileEditDto,
    SettingScopes,
    ProfileServiceProxy,
    SendVerificationSmsInputDto,
    GenerateGoogleAuthenticatorKeyOutput,
    VerifyAuthenticatorCodeInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { SmsVerificationModalComponent } from './sms-verification-modal.component';
import { finalize } from 'rxjs/operators';
import { EnableTwoFactorAuthenticationModalComponent } from './enable-two-factor-authentication-modal.component';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { TooltipDirective } from 'ngx-bootstrap/tooltip';
import { TimeZoneComboComponent } from '../../common/timing/timezone-combo.component';
import { ViewRecoveryCodesModalComponent } from './view-recovery-codes-modal.component';
import { VerifyCodeModalComponent } from './verify-code-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'mySettingsModal',
    templateUrl: './my-settings-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        TooltipDirective,
        TimeZoneComboComponent,
        SmsVerificationModalComponent,
        EnableTwoFactorAuthenticationModalComponent,
        ViewRecoveryCodesModalComponent,
        VerifyCodeModalComponent,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class MySettingsModalComponent extends AppComponentBase implements OnInit {
    private _profileService = inject(ProfileServiceProxy);
    @ViewChild('mySettingsModal', { static: true }) modal: ModalDirective;
    @ViewChild('enableTwoFactor', { static: true })
    enableTwoFactorAuthenticationModal: EnableTwoFactorAuthenticationModalComponent;
    @ViewChild('smsVerificationModal') smsVerificationModal: SmsVerificationModalComponent;
    @ViewChild('verifyCodeModal') verifyCodeModal: SmsVerificationModalComponent;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    public active = false;
    public saving = false;
    public isGoogleAuthenticatorEnabled = false;
    public isPhoneNumberConfirmed: boolean;
    public smsEnabled: boolean;
    public user: CurrentUserProfileEditDto;
    public showTimezoneSelection: boolean = abp.clock?.provider?.supportsMultipleTimezone ?? false;
    public canChangeUserName: boolean;
    public defaultTimezoneScope: SettingScopes = SettingScopes.User;
    public savedPhoneNumber: string;
    public newPhoneNumber: string;
    isMultiTenancyEnabled: boolean = this.multiTenancy.isEnabled;
    isTwoFactorLoginEnabledForApplication = false;
    private _initialTimezone: string = undefined;

    ngOnInit(): void {
        this.isTwoFactorLoginEnabledForApplication = abp.setting.getBoolean(
            'Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled',
        );
    }
    show(): void {
        this.active = true;
        this._profileService.getCurrentUserProfileForEdit().subscribe((result) => {
            this.smsEnabled = this.setting.getBoolean('App.UserManagement.SmsVerificationEnabled');
            this.user = result;
            this._initialTimezone = result.timezone;
            this.canChangeUserName = this.user.userName !== AppConsts.userManagement.defaultAdminUserName;
            this.modal.show();
            this.isGoogleAuthenticatorEnabled = result.isGoogleAuthenticatorEnabled;
            this.isPhoneNumberConfirmed = result.isPhoneNumberConfirmed;
            this.savedPhoneNumber = result.phoneNumber;
        });
    }
    enableTwoFactorAuthentication(): void {
        this._profileService
            .generateGoogleAuthenticatorKey()
            .subscribe((result: GenerateGoogleAuthenticatorKeyOutput) => {
                this.enableTwoFactorAuthenticationModal.model = result;
            });
        this.enableTwoFactorAuthenticationModal.show();
    }
    disableTwoFactorAuthentication(verifyCodeInput: VerifyAuthenticatorCodeInput): void {
        this._profileService
            .disableGoogleAuthenticator(verifyCodeInput)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                this.close();
                this.message.success(this.l('TwoFactorAuthenticationDisabled'));
            });
    }
    smsVerify(): void {
        const input = new SendVerificationSmsInputDto();
        input.phoneNumber = this.user.phoneNumber;
        this._profileService.sendVerificationSms(input).subscribe(() => {
            this.smsVerificationModal.show();
        });
    }
    changePhoneNumberToVerified(): void {
        this.isPhoneNumberConfirmed = true;
        this.savedPhoneNumber = this.user.phoneNumber;
    }
    onShown(): void {
        document.getElementById('Name').focus();
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
    save(): void {
        this.saving = true;
        this._profileService
            .updateCurrentUserProfile(this.user)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                if (this.appSession.user.emailAddress !== this.user.emailAddress) {
                    this.notify.info(this.l('ChangeEmailRequestSentMessage'));
                } else {
                    this.notify.info(this.l('SavedSuccessfully'));
                }
                this.appSession.user.name = this.user.name;
                this.appSession.user.surname = this.user.surname;
                this.appSession.user.userName = this.user.userName;
                this.appSession.user.emailAddress = this.user.emailAddress;
                this.close();
                this.modalSave.emit(null);
                if (abp.clock?.provider?.supportsMultipleTimezone && this._initialTimezone !== this.user.timezone) {
                    this.message.info(this.l('TimeZoneSettingChangedRefreshPageNotification')).then(() => {
                        window.location.reload();
                    });
                }
            });
    }
}
