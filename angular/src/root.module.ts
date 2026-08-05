import { PlatformLocation, registerLocaleData } from '@angular/common';
import { Injector } from '@angular/core';
import { AppAuthService } from '@app/shared/common/auth/app-auth.service';
import { AppConsts } from '@shared/AppConsts';
import { AppSessionService } from '@shared/common/session/app-session.service';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import {
    UiCustomizationSettingsDto,
    ThemeSettingsDto,
    ThemeMenuSettingsDto,
    ThemeLayoutSettingsDto,
    ThemeHeaderSettingsDto,
    ThemeSubHeaderSettingsDto,
    ThemeFooterSettingsDto,
    ApplicationInfoDto,
} from '@shared/service-proxies/service-proxies';
import * as localForage from 'localforage';
import { AppPreBootstrap } from './AppPreBootstrap';
import { DomHelper } from '@shared/helpers/DomHelper';
import { CookieConsentService } from '@shared/common/session/cookie-consent.service';
import { NgxBootstrapDatePickerConfigService } from 'assets/ngx-bootstrap/ngx-bootstrap-datepicker-config.service';
import { LocaleMappingService } from '@shared/locale-mapping.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
export function appInitializerFactory(injector: Injector, platformLocation: PlatformLocation) {
    return () => {
        const spinnerService = injector.get(NgxSpinnerService);
        spinnerService.show();
        return new Promise<boolean>((resolve, reject) => {
            AppConsts.appBaseHref = getBaseHref(platformLocation);
            const appBaseUrl = getDocumentOrigin() + AppConsts.appBaseHref;
            initializeLocalForage();
            AppPreBootstrap.run(
                appBaseUrl,
                injector,
                () => {
                    handleLogoutRequest(injector.get(AppAuthService));
                    if (UrlHelper.isInstallUrl(location.href)) {
                        doConfigurationForInstallPage(injector);
                        spinnerService.hide();
                        resolve(true);
                    } else {
                        const appSessionService: AppSessionService = injector.get(AppSessionService);
                        appSessionService.init().then(
                            (result) => {
                                initializeAppCssClasses(injector, result);
                                initializeTenantResources(injector);
                                initializeCookieConsent(injector);
                                registerLocales(resolve, reject, spinnerService);
                            },
                            (err) => {
                                spinnerService.hide();
                                reject(err);
                            },
                        );
                    }
                },
                resolve,
                reject,
            );
        });
    };
}
function initializeLocalForage() {
    const lf: any = (localForage as any).default ?? localForage;
    lf.config({
        driver: lf.LOCALSTORAGE,
        name: 'Erp',
        version: 1.0,
        storeName: 'abpzerotemplate_local_storage',
        description: 'Cached data for Erp',
    });
}
function getDefaultThemeForInstallPage(): UiCustomizationSettingsDto {
    const theme = new UiCustomizationSettingsDto();
    theme.baseSettings = new ThemeSettingsDto();
    theme.baseSettings.theme = 'default';
    theme.baseSettings.menu = new ThemeMenuSettingsDto();
    theme.baseSettings.menu.asideSkin = 'light';
    theme.baseSettings.header = new ThemeHeaderSettingsDto();
    theme.baseSettings.subHeader = new ThemeSubHeaderSettingsDto();
    theme.baseSettings.layout = new ThemeLayoutSettingsDto();
    theme.baseSettings.layout.layoutType = 'fluid';
    theme.baseSettings.header = new ThemeHeaderSettingsDto();
    theme.baseSettings.footer = new ThemeFooterSettingsDto();
    return theme;
}
function setApplicationInfoForInstallPage(injector, theme: UiCustomizationSettingsDto) {
    const appSessionService: AppSessionService = injector.get(AppSessionService);
    const dateTimeService: DateTimeService = injector.get(DateTimeService);
    appSessionService.theme = theme;
    appSessionService.application = new ApplicationInfoDto();
    appSessionService.application.releaseDate = dateTimeService.getStartOfDay();
}
function doConfigurationForInstallPage(injector) {
    const theme = getDefaultThemeForInstallPage();
    setApplicationInfoForInstallPage(injector, theme);
    initializeAppCssClasses(injector, theme);
}
function initializeAppCssClasses(injector: Injector, theme: UiCustomizationSettingsDto) {
    const appUiCustomizationService = injector.get(AppUiCustomizationService);
    appUiCustomizationService.init(theme);
    // Css classes based on the layout
    if (abp.session.userId) {
        document.body.className = appUiCustomizationService.getAppModuleBodyClass();
        document.body.setAttribute('style', appUiCustomizationService.getAppModuleBodyStyle());
    } else {
        document.body.className = appUiCustomizationService.getAccountModuleBodyClass();
        document.body.setAttribute('style', appUiCustomizationService.getAccountModuleBodyStyle());
    }
}
function initializeTenantResources(injector: Injector) {
    const appSessionService: AppSessionService = injector.get(AppSessionService);
    if (appSessionService.tenant && appSessionService.tenant.customCssId) {
        document.head.appendChild(
            DomHelper.createElement('link', [
                {
                    key: 'id',
                    value: 'TenantCustomCss',
                },
                {
                    key: 'rel',
                    value: 'stylesheet',
                },
                {
                    key: 'href',
                    value: `${AppConsts.remoteServiceBaseUrl}/TenantCustomization/GetCustomCss?tenantId=${
                        appSessionService.tenant.id
                    }`,
                },
            ]),
        );
    }
    const metaImage = DomHelper.getElementByAttributeValue('meta', 'property', 'og:image');
    if (metaImage) {
        //set og share image meta tag
        if (!appSessionService.tenant?.HasLogo()) {
            const ui: AppUiCustomizationService = injector.get(AppUiCustomizationService);
            metaImage.setAttribute(
                'content',
                `${window.location.origin}/assets/common/images/app-logo-on-${
                    abp.setting?.get
                        ? abp.setting.get(
                              `${appSessionService.theme.baseSettings.theme}.` + 'App.UiManagement.Left.AsideSkin',
                          )
                        : 'light'
                }.svg`,
            );
        } else {
            metaImage.setAttribute(
                'content',
                `${AppConsts.remoteServiceBaseUrl}/TenantCustomization/GetLogo?tenantId=${appSessionService.tenant.id}`,
            );
        }
    }
}
function initializeCookieConsent(injector: Injector) {
    const cookieConsentService: CookieConsentService = injector.get(CookieConsentService);
    cookieConsentService.init();
}
function getDocumentOrigin() {
    if (!document.location.origin) {
        return `${document.location.protocol}//${
            document.location.hostname
        }${document.location.port ? `:${document.location.port}` : ''}`;
    }
    return document.location.origin;
}
function registerLocales(
    resolve: (value?: boolean | Promise<boolean>) => void,
    reject: any,
    spinnerService: NgxSpinnerService,
) {
    if (shouldLoadLocale()) {
        const angularLocale = convertAbpLocaleToAngularLocale(abp.localization.currentLanguage.name);
        loadAngularLocaleData(angularLocale)
            .then((module) => {
                if (module?.default) {
                    registerLocaleData(module.default);
                }
                return NgxBootstrapDatePickerConfigService.registerNgxBootstrapDatePickerLocales();
            })
            .then(() => {
                resolve(true);
                spinnerService.hide();
            })
            .catch(reject);
    } else {
        NgxBootstrapDatePickerConfigService.registerNgxBootstrapDatePickerLocales().then((_) => {
            resolve(true);
            spinnerService.hide();
        });
    }
}
type AngularLocaleModule = { default: unknown };
const ANGULAR_LOCALE_LOADERS: Record<string, () => Promise<AngularLocaleModule>> = {
    // Keep this list small and explicit so Vite can statically analyze the imports.
    // Add more locales here if your app supports them.
    'en-gb': () => import('@angular/common/locales/en-GB'),
    es: () => import('@angular/common/locales/es'),
    'pt-br': () => import('@angular/common/locales/pt'),
    'zh-hans': () => import('@angular/common/locales/zh-Hans'),
    vi: () => import('@angular/common/locales/vi'),
    ta: () => import('@angular/common/locales/ta'),
    cs: () => import('@angular/common/locales/cs'),
    de: () => import('@angular/common/locales/de'),
    fr: () => import('@angular/common/locales/fr'),
    it: () => import('@angular/common/locales/it'),
    ja: () => import('@angular/common/locales/ja'),
    ko: () => import('@angular/common/locales/ko'),
    pl: () => import('@angular/common/locales/pl'),
    ru: () => import('@angular/common/locales/ru'),
    sv: () => import('@angular/common/locales/sv'),
    tr: () => import('@angular/common/locales/tr'),
    'zh-hant': () => import('@angular/common/locales/zh-Hant'),
};
function loadAngularLocaleData(locale: string): Promise<AngularLocaleModule | null> {
    const key = (locale || '').toLowerCase();
    // Angular includes en-US by default; no need to load extra locale data.
    if (!key || key === 'en-us') {
        return Promise.resolve(null);
    }
    const loader = ANGULAR_LOCALE_LOADERS[key];
    if (!loader) {
        return Promise.resolve(null);
    }
    return loader();
}
export function shouldLoadLocale(): boolean {
    return abp.localization.currentLanguage.name && abp.localization.currentLanguage.name !== 'en-US';
}
export function convertAbpLocaleToAngularLocale(locale: string): string {
    return new LocaleMappingService().map('angular', locale);
}
export function getRemoteServiceBaseUrl(): string {
    return AppConsts.remoteServiceBaseUrl;
}
export function getCurrentLanguage(): string {
    return convertAbpLocaleToAngularLocale(abp.localization.currentLanguage.name);
}
export function getCurrencyCode(injector: Injector): string {
    const appSessionService: AppSessionService = injector.get(AppSessionService);
    return appSessionService.application.currency;
}
export function getBaseHref(platformLocation: PlatformLocation): string {
    const baseUrl = platformLocation.getBaseHrefFromDOM();
    if (baseUrl) {
        return baseUrl;
    }
    return '/';
}
function handleLogoutRequest(authService: AppAuthService) {
    const currentUrl = UrlHelper.initialUrl;
    const returnUrl = UrlHelper.getReturnUrl();
    if (currentUrl.indexOf('account/logout') >= 0 && returnUrl) {
        authService.logout(true, returnUrl);
    }
}
