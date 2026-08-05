import { Component, OnInit, ViewEncapsulation, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { LoginService } from './login/login.service';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppPreBootstrap } from 'AppPreBootstrap';
import { TenantChangeComponent } from './shared/tenant-change.component';
import { LanguageSwitchComponent } from './language-switch.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
@Component({
    templateUrl: './account.component.html',
    styleUrls: ['./account.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [TenantChangeComponent, RouterOutlet, LanguageSwitchComponent],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class AccountComponent extends AppComponentBase implements OnInit {
    private _router = inject(Router);
    private _loginService = inject(LoginService);
    private _uiCustomizationService = inject(AppUiCustomizationService);
    private _dateTimeService = inject(DateTimeService);
    currentYear: number;
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
    skin = this.appSession.theme.baseSettings.layout.darkMode ? 'dark' : 'light';
    defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${this.skin}.svg`;
    backgroundImageName = this.appSession.theme.baseSettings.layout.darkMode ? 'login-dark' : 'login';
    tenantChangeDisabledRoutes: string[] = [
        'select-edition',
        'gateway-selection',
        'register-tenant',
        'stripe-pre-payment',
        'stripe-post-payment',
        'paypal-pre-payment',
        'paypal-post-payment',
        'stripe-cancel-payment',
        'buy-succeed',
        'extend-succeed',
        'upgrade-succeed',
        'payment-failed',
        'session-locked',
    ];
    public;
    showTenantChange(): boolean {
        if (!this._router.url) {
            return false;
        }
        if (this.tenantChangeDisabledRoutes.some((route) => this._router.url.indexOf(`/account/${route}`) >= 0)) {
            return false;
        }
        return abp.multiTenancy.isEnabled && !this.supportsTenancyNameInUrl();
    }
    isSelectEditionPage(): boolean {
        return this._router.url.indexOf('/account/select-edition') >= 0;
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this._loginService.init();
        document.body.className = this._uiCustomizationService.getAccountModuleBodyClass();
        this.currentYear = this._dateTimeService.getYear();
    }
    goToHome(): void {
        window.location.href = '/';
    }
    getBgUrl(): string {
        return `url(./assets/metronic/themes/${this.currentTheme.baseSettings.theme}/images/bg/bg-4.jpg)`;
    }
    getLogoSkin(): string {
        return this.currentTheme.baseSettings.layout.darkMode ? 'light' : 'dark';
    }
    private supportsTenancyNameInUrl() {
        return AppPreBootstrap.resolveTenancyName(AppConsts.appBaseUrlFormat) != null;
    }
}
