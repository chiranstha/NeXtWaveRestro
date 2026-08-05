import { Injectable, inject } from '@angular/core';
import { SettingService } from 'abp-ng2-module';
import { ReCaptchaV3Service } from 'ngx-captcha';
import { Observable } from 'rxjs';
import { AppConsts } from '@shared/AppConsts';
@Injectable()
export class ReCaptchaV3WrapperService {
    private setting = inject(SettingService);
    private _reCaptchaV3Service = inject(ReCaptchaV3Service);
    constructor() {}
    execute(action: string): Observable<string> {
        return new Observable((observer) => {
            this._reCaptchaV3Service.execute(AppConsts.recaptchaSiteKey, action, (token) => {
                observer.next(token);
                observer.complete();
            });
        });
    }
    useCaptchaOnLogin(): boolean {
        return this.setting.getBoolean('App.UserManagement.UseCaptchaOnLogin');
    }
    setCaptchaVisibilityOnLogin(): void {
        const recpatchaElements = document.getElementsByClassName('grecaptcha-badge');
        if (!recpatchaElements || recpatchaElements.length <= 0) {
            return;
        }
        if (this.useCaptchaOnLogin()) {
            recpatchaElements[0].classList.remove('d-none');
        } else {
            recpatchaElements[0].classList.add('d-none');
        }
    }
    useCaptchaOnRegister(): boolean {
        return this.setting.getBoolean('App.UserManagement.UseCaptchaOnRegistration');
    }
    setCaptchaVisibilityOnRegister(): void {
        const recpatchaElements = document.getElementsByClassName('grecaptcha-badge');
        if (!recpatchaElements || recpatchaElements.length <= 0) {
            return;
        }
        if (this.useCaptchaOnRegister()) {
            recpatchaElements[0].classList.remove('d-none');
        } else {
            recpatchaElements[0].classList.add('d-none');
        }
    }
    useCaptchaOnResetPassword(): boolean {
        return this.setting.getBoolean('App.UserManagement.UseCaptchaOnResetPassword');
    }
    setCaptchaVisibilityOnResetPassword(): void {
        const recpatchaElements = document.getElementsByClassName('grecaptcha-badge');
        if (!recpatchaElements || recpatchaElements.length <= 0) {
            return;
        }
        if (this.useCaptchaOnResetPassword()) {
            recpatchaElements[0].classList.remove('d-none');
        } else {
            recpatchaElements[0].classList.add('d-none');
        }
    }
    useCaptchaOnEmailActivation(): boolean {
        return this.setting.getBoolean('App.TenantManagement.UseCaptchaOnEmailActivation');
    }
    setCaptchaVisibilityOnEmailActivation(): void {
        const recpatchaElements = document.getElementsByClassName('grecaptcha-badge');
        if (!recpatchaElements || recpatchaElements.length <= 0) {
            return;
        }
        if (this.useCaptchaOnEmailActivation()) {
            recpatchaElements[0].classList.remove('d-none');
        } else {
            recpatchaElements[0].classList.add('d-none');
        }
    }
}
