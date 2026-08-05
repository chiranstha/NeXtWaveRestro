import { AfterViewInit, Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy } from '@shared/service-proxies/service-proxies';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { LoginService } from './login.service';
import { AppConsts } from '@shared/AppConsts';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
interface UserInfo {
    userName: string;
    tenant: string;
    profilePicture: string;
    externalLoginProviderName: string;
}
@Component({
    selector: 'app-session-lock-screen',
    templateUrl: './session-lock-screen.component.html',
    styleUrls: ['session-lock-screen.component.less'],
    animations: [accountModuleAnimation()],
    imports: [FormsModule, ValidationMessagesComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class SessionLockScreenComponent extends AppComponentBase implements AfterViewInit {
    private _profileService = inject(ProfileServiceProxy);
    loginService = inject(LoginService);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    @ViewChild('loginForm') loginForm: NgForm;
    @ViewChild('externalLoginForm') externalLoginForm: NgForm;
    userInfo: UserInfo;
    submitting = false;
    isExternalLoginEnabled: boolean;
    externalLoginProviderName: string;
    isLoginButtonDisabled = true;
    isExternalLoginButtonDisabled = true;
    constructor() {
        super();
        this.getLastUserInfo();
    }
    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
        // Track form validity changes
        if (this.loginForm) {
            this.loginForm.form.valueChanges.subscribe(() => {
                this.isLoginButtonDisabled = !this.loginForm.form.valid;
            });
            // Set initial state
            this.isLoginButtonDisabled = !this.loginForm.form.valid;
        }
        if (this.externalLoginForm) {
            this.externalLoginForm.form.valueChanges.subscribe(() => {
                this.isExternalLoginButtonDisabled = !this.externalLoginForm.form.valid;
            });
            // Set initial state
            this.isExternalLoginButtonDisabled = !this.externalLoginForm.form.valid;
        }
    }
    getLastUserInfo(): void {
        const cookie = abp.utils.getCookieValue('userInfo');
        if (!cookie) {
            location.href = '';
        }
        const userInfo = JSON.parse(cookie);
        if (!userInfo) {
            location.href = '';
        }
        this.loginService.authenticateModel.userNameOrEmailAddress = userInfo.userName;
        this.userInfo = {
            userName: userInfo.userName,
            tenant: userInfo.tenant,
            profilePicture: '',
            externalLoginProviderName: userInfo.externalLoginProviderName,
        };
        this.setIsExternalLoginEnabled(userInfo.externalLoginProviderName);
        this.externalLoginProviderName = userInfo.externalLoginProviderName;
        this._profileService.getProfilePictureByUserName(userInfo.userName).subscribe(
            (data) => {
                if (data.profilePicture) {
                    this.userInfo.profilePicture = `data:image/jpeg;base64,${data.profilePicture}`;
                } else {
                    this.userInfo.profilePicture = `${AppConsts.appBaseUrl}/assets/common/images/default-profile-picture.png`;
                }
            },
            () => {
                this.userInfo.profilePicture = `${AppConsts.appBaseUrl}/assets/common/images/default-profile-picture.png`;
            },
        );
    }
    login(): void {
        const recaptchaCallback = (token: string) => {
            this.showMainSpinner();
            this.submitting = true;
            this.loginService.authenticate(
                () => {
                    this.submitting = false;
                    this.hideMainSpinner();
                },
                null,
                token,
            );
        };
        if (this._recaptchaWrapperService.useCaptchaOnLogin()) {
            this._recaptchaWrapperService.execute('login').subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
    externalLogin(): void {
        const foundProvider = this.loginService.externalLoginProviders.find(
            (p) => p.name === this.externalLoginProviderName,
        );
        this.loginService.externalAuthenticate(foundProvider);
    }
    setIsExternalLoginEnabled(externalLoginProviderName: string): void {
        this.isExternalLoginEnabled = externalLoginProviderName != null;
    }
}
