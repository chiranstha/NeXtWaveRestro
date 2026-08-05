import { IAjaxResponse, TokenService } from 'abp-ng2-module';
import {
    ChangeDetectorRef,
    Component,
    ElementRef,
    OnInit,
    AfterViewInit,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    JsonClaimMapDto,
    SendTestEmailInput,
    SettingScopes,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    ReportingServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileUploader, FileUploaderOptions, FileUploadModule } from 'ng2-file-upload';
import { finalize } from 'rxjs/operators';
import { KeyValueListManagerComponent } from '@app/shared/common/key-value-list-manager/key-value-list-manager.component';
import { UntypedFormControl, FormsModule } from '@angular/forms';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective, TabHeadingDirective } from 'ngx-bootstrap/tabs';
import { TimeZoneComboComponent } from '../../shared/common/timing/timezone-combo.component';
import { TooltipDirective } from 'ngx-bootstrap/tooltip';
import { NgClass } from '@angular/common';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { NgSelectComponent, NgOptionComponent } from '@ng-select/ng-select';
import { PasswordInputWithShowButtonComponent } from '../../shared/common/password-input-with-show-button/password-input-with-show-button.component';
import { KeyValueListManagerComponent as KeyValueListManagerComponent_1 } from '../../shared/common/key-value-list-manager/key-value-list-manager.component';
import { RouterLink } from '@angular/router';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './tenant-settings.component.html',
    styleUrls: ['./tenant-settings.component.css'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        TabHeadingDirective,
        TimeZoneComboComponent,
        FormsModule,
        TooltipDirective,
        FileUploadModule,
        NgClass,
        ValidationMessagesComponent,
        NgSelectComponent,
        NgOptionComponent,
        PasswordInputWithShowButtonComponent,
        KeyValueListManagerComponent_1,
        RouterLink,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class TenantSettingsComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _tenantSettingsService = inject(TenantSettingsServiceProxy);
    private _tokenService = inject(TokenService);
    private _dateTimeService = inject(DateTimeService);
    private _cdr = inject(ChangeDetectorRef);
    private proxy = inject(ReportingServiceProxy);
    @ViewChild('wsFederationClaimsMappingManager') wsFederationClaimsMappingManager: KeyValueListManagerComponent;
    @ViewChild('openIdConnectClaimsMappingManager') openIdConnectClaimsMappingManager: KeyValueListManagerComponent;
    @ViewChild('emailSmtpSettingsForm') emailSmtpSettingsForm: UntypedFormControl;
    @ViewChild('securitySettingsForm') securitySettingsForm: UntypedFormControl;
    @ViewChild('emailSmtpSettingsTestForm') emailSmtpSettingsTestForm: any;
    @ViewChild('uploadCustomCSSInputLabel') uploadCustomCSSInputLabel: ElementRef;
    @ViewChild('darkLogoImg') darkLogoImg: ElementRef;
    @ViewChild('darkLogoMinimalImg') darkLogoMinimalImg: ElementRef;
    @ViewChild('lightLogoImg') lightLogoImg: ElementRef;
    @ViewChild('lightLogoMinimalImg') lightLogoMinimalImg: ElementRef;
    @ViewChild('darkLogoUploaderFileInput') darkLogoUploaderFileInput: any;
    @ViewChild('darkLogoMinimalUploaderFileInput') darkLogoMinimalUploaderFileInput: any;
    @ViewChild('lightLogoUploaderFileInput') lightLogoUploaderFileInput: any;
    @ViewChild('lightLogoMinimalUploaderFileInput') lightLogoMinimalUploaderFileInput: any;
    usingDefaultTimeZone = false;
    initialTimeZone: string = null;
    testEmailAddress: string = undefined;
    setRandomPassword: boolean;
    isFormValid = false;
    isMultiTenancyEnabled: boolean = this.multiTenancy.isEnabled;
    showTimezoneSelection: boolean = abp.clock?.provider?.supportsMultipleTimezone ?? false;
    activeTabIndex: number = abp.clock?.provider?.supportsMultipleTimezone ? 0 : 1;
    loading = false;
    settings: any = undefined;
    accountLedgerList: any[] = [];
    darkLogoUploader: FileUploader;
    darkLogoMinimalUploader: FileUploader;
    lightLogoUploader: FileUploader;
    lightLogoMinimalUploader: FileUploader;
    customCssUploader: FileUploader;
    remoteServiceBaseUrl = AppConsts.remoteServiceBaseUrl;
    defaultTimezoneScope: SettingScopes = SettingScopes.Tenant;
    enabledSocialLoginSettings: string[];
    useFacebookHostSettings: boolean;
    useGoogleHostSettings: boolean;
    useMicrosoftHostSettings: boolean;
    useWsFederationHostSettings: boolean;
    useOpenIdConnectHostSettings: boolean;
    useTwitterHostSettings: boolean;
    wsFederationClaimMappings: { key: string; value: string }[];
    openIdConnectClaimMappings: { key: string; value: string }[];
    openIdConnectResponseTypeCode: boolean;
    openIdConnectResponseTypeToken: boolean;
    openIdConnectResponseTypeIdToken: boolean;
    initialEmailSettings: string;
    emailDomainPattern = '^[a-zA-Z0-9._%+-]+(?<!@)[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$';
    printtype = ['Default'];
    PrintTypeLabelMapping: Record<string, string> = { Default: 'Default' };
    LayoutType = ['Default'];
    LayoutTypeMapping: Record<string, string> = { Default: 'Default' };
    FontFamilyType = ['Default'];
    FontFamilyTypeMapping: Record<string, string> = { Default: 'Default' };
    MarksheetformatType = ['Default'];
    MarksheetMapping: Record<string, string> = { Default: 'Default' };
    AdmitformatType = ['Default'];
    AdmitCardMapping: Record<string, string> = { Default: 'Default' };
    receiptformatType = ['Default'];
    feeReceiptMapping: Record<string, string> = { Default: 'Default' };
    dueformatType = ['Default'];
    feeDueMapping: Record<string, string> = { Default: 'Default' };
    orderType = ['Default'];
    studentOrderType: Record<string, string> = { Default: 'Default' };

    // Add heading templates with icons
    tabHeadings = {
        general: '<i class="fas fa-cog me-2"></i>General',
        appearance: '<i class="fas fa-palette me-2"></i>Appearance',
        userManagement: '<i class="fas fa-users me-2"></i>User Management',
        security: '<i class="fas fa-shield-alt me-2"></i>Security',
        email: '<i class="fas fa-envelope me-2"></i>Email SMTP',
        whatsApp: '<i class="fab fa-whatsapp me-2"></i>WhatsApp',
        firebase: '<i class="fab fa-google me-2"></i>Firebase',
        sparrowSms: '<i class="fas fa-sms me-2"></i>Sparrow SMS',
        print: '<i class="fas fa-print me-2"></i>Print',
        invoice: '<i class="fas fa-file-invoice me-2"></i>Invoice',
        school: '<i class="fas fa-school me-2"></i>School',
        otherSettings: '<i class="fas fa-cogs me-2"></i>Other Settings',
        // Social login tab headings
        facebook: '<i class="fab fa-facebook me-2"></i>Facebook',
        google: '<i class="fab fa-google me-2"></i>Google',
        microsoft: '<i class="fab fa-microsoft me-2"></i>Microsoft',
        wsFederation: '<i class="fas fa-id-card me-2"></i>WsFederation',
        openId: '<i class="fas fa-key me-2"></i>OpenId',
        twitter: '<i class="fab fa-twitter me-2"></i>Twitter',
    };

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.testEmailAddress = this.appSession.user.emailAddress;
        this.getSettings();
        this.getAccountLedgers();
        this.initUploaders();
        this.loadSocialLoginSettings();
    }
    ngAfterViewInit(): void {
        if (this.emailSmtpSettingsTestForm) {
            this.emailSmtpSettingsTestForm.form.valueChanges.subscribe(() => {
                this.isFormValid = this.emailSmtpSettingsTestForm.form.valid;
                this._cdr.markForCheck();
            });
        }
    }

    getSettings(): void {
        this.loading = true;
        this._tenantSettingsService
            .getAllSettings()
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this._cdr.markForCheck();
                }),
            )
            .subscribe({
                next: (result: TenantSettingsEditDto) => {
                    this.settings = result;
                    this.ensureExtendedSettings();
                    if (this.settings.general) {
                        this.initialTimeZone = this.settings.general.timezone;
                        this.usingDefaultTimeZone =
                            this.settings.general.timezoneForComparison === abp.setting.values['Abp.Timing.TimeZone'];
                    }
                    this.useFacebookHostSettings = !(
                        this.settings.externalLoginProviderSettings.facebook.appId ||
                        this.settings.externalLoginProviderSettings.facebook_IsDeactivated
                    );
                    this.useGoogleHostSettings = !(
                        this.settings.externalLoginProviderSettings.google.clientId ||
                        this.settings.externalLoginProviderSettings.google_IsDeactivated
                    );
                    this.useMicrosoftHostSettings = !(
                        this.settings.externalLoginProviderSettings.microsoft.clientId ||
                        this.settings.externalLoginProviderSettings.microsoft_IsDeactivated
                    );
                    this.useWsFederationHostSettings = !(
                        this.settings.externalLoginProviderSettings.wsFederation.clientId ||
                        this.settings.externalLoginProviderSettings.wsFederation_IsDeactivated
                    );
                    this.useOpenIdConnectHostSettings = !(
                        this.settings.externalLoginProviderSettings.openIdConnect.clientId ||
                        this.settings.externalLoginProviderSettings.openIdConnect_IsDeactivated
                    );
                    this.useTwitterHostSettings = !(
                        this.settings.externalLoginProviderSettings.twitter.consumerKey ||
                        this.settings.externalLoginProviderSettings.twitter_IsDeactivated
                    );
                    this.wsFederationClaimMappings =
                        this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping.map((item) => ({
                            key: item.key,
                            value: item.claim,
                        }));
                    this.openIdConnectClaimMappings =
                        this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping.map((item) => ({
                            key: item.key,
                            value: item.claim,
                        }));
                    if (this.settings.externalLoginProviderSettings.openIdConnect.responseType) {
                        const openIdConnectResponseTypes =
                            this.settings.externalLoginProviderSettings.openIdConnect.responseType.split(',');
                        this.openIdConnectResponseTypeCode = openIdConnectResponseTypes.indexOf('code') > -1;
                        this.openIdConnectResponseTypeIdToken = openIdConnectResponseTypes.indexOf('id_token') > -1;
                        this.openIdConnectResponseTypeToken = openIdConnectResponseTypes.indexOf('token') > -1;
                    }
                    this.initialEmailSettings = JSON.stringify(this.settings.email);
                    this._cdr.markForCheck();
                },
                error: () => {
                    this.notify.error('Unable to load tenant settings.');
                    this._cdr.markForCheck();
                },
            });
    }
    private ensureExtendedSettings(): void {
        this.settings.sparrowSms ??= {};
        this.settings.smsTypeSettings ??= {};
        this.settings.allSettingsBundleDto ??= {};
        this.settings.firebase ??= {};
        this.settings.userManagement ??= {};
    }
    getAccountLedgers(): void {
        this.proxy.getAllAccountLedgers().subscribe((result) => {
            this.accountLedgerList = result;
            this._cdr.markForCheck();
        });
    }
    initUploaders(): void {
        this.darkLogoUploader = this.createUploader('/TenantCustomization/UploadDarkLogo', (result) => {
            this.appSession.tenant.darkLogoFileType = result.fileType;
            this.appSession.tenant.darkLogoId = result.id;
            this.refreshLogo('dark');
        });
        this.darkLogoMinimalUploader = this.createUploader('/TenantCustomization/UploadDarkLogoMinimal', (result) => {
            this.appSession.tenant.darkLogoMinimalFileType = result.fileType;
            this.appSession.tenant.darkLogoMinimalId = result.id;
            this.refreshLogo('dark-sm');
        });
        this.lightLogoUploader = this.createUploader('/TenantCustomization/UploadLightLogo', (result) => {
            this.appSession.tenant.lightLogoFileType = result.fileType;
            this.appSession.tenant.lightLogoId = result.id;
            this.refreshLogo('light');
        });
        this.lightLogoMinimalUploader = this.createUploader('/TenantCustomization/UploadLightLogoMinimal', (result) => {
            this.appSession.tenant.lightLogoMinimalFileType = result.fileType;
            this.appSession.tenant.lightLogoMinimalId = result.id;
            this.refreshLogo('light-sm');
        });
        this.customCssUploader = this.createUploader('/TenantCustomization/UploadCustomCss', (result) => {
            this.appSession.tenant.customCssId = result.id;
            const oldTenantCustomCss = document.getElementById('TenantCustomCss');
            if (oldTenantCustomCss) {
                oldTenantCustomCss.remove();
            }
            const tenantCustomCss = document.createElement('link');
            tenantCustomCss.setAttribute('id', 'TenantCustomCss');
            tenantCustomCss.setAttribute('rel', 'stylesheet');
            tenantCustomCss.setAttribute(
                'href',
                `${AppConsts.remoteServiceBaseUrl}/TenantCustomization/GetCustomCss?tenantId=${
                    this.appSession.tenant.id
                }`,
            );
            document.head.appendChild(tenantCustomCss);
        });
    }
    createUploader(url: string, success?: (result: any) => void): FileUploader {
        const uploaderOptions: FileUploaderOptions = { url: AppConsts.remoteServiceBaseUrl + url };
        uploaderOptions.authToken = `Bearer ${this._tokenService.getToken()}`;
        uploaderOptions.removeAfterUpload = true;
        const uploader = new FileUploader(uploaderOptions);
        uploader.onAfterAddingFile = (file) => {
            file.withCredentials = false;
        };
        uploader.onSuccessItem = (item, response, _status) => {
            const ajaxResponse = <IAjaxResponse>JSON.parse(response);
            if (ajaxResponse.success) {
                this.notify.info(this.l('SavedSuccessfully'));
                if (success) {
                    success(ajaxResponse.result);
                }
            } else {
                this.message.error(ajaxResponse.error.message);
            }
            this._cdr.markForCheck();
        };
        return uploader;
    }
    uploadDarkLogo(): void {
        this.darkLogoUploader.uploadAll();
        this.darkLogoUploaderFileInput.nativeElement.value = '';
    }
    uploadDarkLogoMinimal(): void {
        this.darkLogoMinimalUploader.uploadAll();
        this.darkLogoMinimalUploaderFileInput.nativeElement.value = '';
    }
    uploadLightLogo(): void {
        this.lightLogoUploader.uploadAll();
        this.lightLogoUploaderFileInput.nativeElement.value = '';
    }
    uploadLightLogoMinimal(): void {
        this.lightLogoMinimalUploader.uploadAll();
        this.lightLogoMinimalUploaderFileInput.nativeElement.value = '';
    }
    uploadCustomCss(): void {
        this.customCssUploader.uploadAll();
    }
    clearDarkLogo(): void {
        this._tenantSettingsService.clearDarkLogo().subscribe(() => {
            this.appSession.tenant.darkLogoFileType = null;
            this.appSession.tenant.darkLogoId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.refreshLogo('dark');
            this._cdr.markForCheck();
        });
    }
    clearDarkLogoMinimal(): void {
        this._tenantSettingsService.clearDarkLogoMinimal().subscribe(() => {
            this.appSession.tenant.darkLogoMinimalFileType = null;
            this.appSession.tenant.darkLogoMinimalId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.refreshLogo('dark-sm');
            this._cdr.markForCheck();
        });
    }
    clearLightLogo(): void {
        this._tenantSettingsService.clearLightLogo().subscribe(() => {
            this.appSession.tenant.lightLogoFileType = null;
            this.appSession.tenant.lightLogoId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.refreshLogo('light');
            this._cdr.markForCheck();
        });
    }
    clearLightLogoMinimal(): void {
        this._tenantSettingsService.clearLightLogoMinimal().subscribe(() => {
            this.appSession.tenant.lightLogoMinimalFileType = null;
            this.appSession.tenant.lightLogoMinimalId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.refreshLogo('light-sm');
            this._cdr.markForCheck();
        });
    }
    clearCustomCss(): void {
        this._tenantSettingsService.clearCustomCss().subscribe(() => {
            this.appSession.tenant.customCssId = null;
            const oldTenantCustomCss = document.getElementById('TenantCustomCss');
            if (oldTenantCustomCss) {
                oldTenantCustomCss.remove();
            }
            this.notify.info(this.l('ClearedSuccessfully'));
            this._cdr.markForCheck();
        });
    }
    mapClaims(): void {
        if (this.wsFederationClaimsMappingManager) {
            this.settings.externalLoginProviderSettings.wsFederationClaimsMapping =
                this.wsFederationClaimsMappingManager.getItems().map(
                    (item) =>
                        new JsonClaimMapDto({
                            key: item.key,
                            claim: item.value,
                        }),
                );
        }
        if (this.openIdConnectClaimsMappingManager) {
            this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping =
                this.openIdConnectClaimsMappingManager.getItems().map(
                    (item) =>
                        new JsonClaimMapDto({
                            key: item.key,
                            claim: item.value,
                        }),
                );
        }
    }
    saveAll(): void {
        if (!this.isSmtpSettingsFormValid()) {
            return;
        }
        if (!this.isSecuritySettingsFormValid()) {
            return;
        }
        this.settings.externalLoginProviderSettings.openIdConnect.responseType =
            this.getSelectedOpenIdConnectResponseTypes();
        this.mapClaims();
        this._tenantSettingsService.updateAllSettings(this.settings).subscribe(() => {
            this.notify.info(this.l('SavedSuccessfully'));
            if (
                abp.clock.provider.supportsMultipleTimezone &&
                this.usingDefaultTimeZone &&
                this.initialTimeZone !== this.settings.general.timezone
            ) {
                this.message.info(this.l('TimeZoneSettingChangedRefreshPageNotification')).then(() => {
                    window.location.reload();
                });
            }
            this.initialEmailSettings = JSON.stringify(this.settings.email);
            this._cdr.markForCheck();
        });
    }
    getSelectedOpenIdConnectResponseTypes(): string {
        let openIdConnectResponseTypes = '';
        if (this.openIdConnectResponseTypeToken) {
            openIdConnectResponseTypes += 'token';
        }
        if (this.openIdConnectResponseTypeIdToken) {
            if (openIdConnectResponseTypes.length > 0) {
                openIdConnectResponseTypes += ',';
            }
            openIdConnectResponseTypes += 'id_token';
        }
        if (this.openIdConnectResponseTypeCode) {
            if (openIdConnectResponseTypes.length > 0) {
                openIdConnectResponseTypes += ',';
            }
            openIdConnectResponseTypes += 'code';
        }
        return openIdConnectResponseTypes;
    }
    sendTestEmail(): void {
        const input = new SendTestEmailInput();
        input.emailAddress = this.testEmailAddress;
        if (this.initialEmailSettings !== JSON.stringify(this.settings.email)) {
            this.message.confirm(this.l('SendEmailWithSavedSettingsWarning'), this.l('AreYouSure'), (isConfirmed) => {
                if (isConfirmed) {
                    this._tenantSettingsService.sendTestEmail(input).subscribe(() => {
                        this.notify.info(this.l('TestEmailSentSuccessfully'));
                    });
                }
            });
        } else {
            this._tenantSettingsService.sendTestEmail(input).subscribe(() => {
                this.notify.info(this.l('TestEmailSentSuccessfully'));
            });
        }
    }
    loadSocialLoginSettings(): void {
        const self = this;
        this._tenantSettingsService.getEnabledSocialLoginSettings().subscribe({
            next: (setting) => {
                self.enabledSocialLoginSettings = setting.enabledSocialLoginSettings;
                self._cdr.markForCheck();
            },
            error: () => {
                self.enabledSocialLoginSettings = [];
                self._cdr.markForCheck();
            },
        });
    }
    clearFacebookSettings(): void {
        this.settings.externalLoginProviderSettings.facebook.appId = '';
        this.settings.externalLoginProviderSettings.facebook.appSecret = '';
        this.settings.externalLoginProviderSettings.facebook_IsDeactivated = false;
    }
    clearGoogleSettings(): void {
        this.settings.externalLoginProviderSettings.google.clientId = '';
        this.settings.externalLoginProviderSettings.google.clientSecret = '';
        this.settings.externalLoginProviderSettings.google.userInfoEndpoint = '';
        this.settings.externalLoginProviderSettings.google_IsDeactivated = false;
    }
    clearMicrosoftSettings(): void {
        this.settings.externalLoginProviderSettings.microsoft.clientId = '';
        this.settings.externalLoginProviderSettings.microsoft.clientSecret = '';
        this.settings.externalLoginProviderSettings.microsoft_IsDeactivated = false;
    }
    clearWsFederationSettings(): void {
        this.settings.externalLoginProviderSettings.wsFederation.clientId = '';
        this.settings.externalLoginProviderSettings.wsFederation.authority = '';
        this.settings.externalLoginProviderSettings.wsFederation.wtrealm = '';
        this.settings.externalLoginProviderSettings.wsFederation.metaDataAddress = '';
        this.settings.externalLoginProviderSettings.wsFederation.tenant = '';
        this.settings.externalLoginProviderSettings.wsFederationClaimsMapping = [];
        this.settings.externalLoginProviderSettings.wsFederation_IsDeactivated = false;
    }
    clearOpenIdSettings(): void {
        this.settings.externalLoginProviderSettings.openIdConnect.clientId = '';
        this.settings.externalLoginProviderSettings.openIdConnect.clientSecret = '';
        this.settings.externalLoginProviderSettings.openIdConnect.authority = '';
        this.settings.externalLoginProviderSettings.openIdConnect.loginUrl = '';
        this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping = [];
        this.settings.externalLoginProviderSettings.openIdConnect_IsDeactivated = false;
    }
    clearTwitterSettings(): void {
        this.settings.externalLoginProviderSettings.twitter.consumerKey = '';
        this.settings.externalLoginProviderSettings.twitter.consumerSecret = '';
    }
    isSocialLoginEnabled(name: string): boolean {
        return this.enabledSocialLoginSettings && this.enabledSocialLoginSettings.indexOf(name) !== -1;
    }
    isSmtpSettingsFormValid(): boolean {
        if (!this.emailSmtpSettingsForm) {
            return true;
        }
        return this.emailSmtpSettingsForm.valid;
    }
    isSecuritySettingsFormValid(): boolean {
        return this.securitySettingsForm.valid;
    }
    onUploadDarkLogoInputChange() {
        this.uploadDarkLogo();
    }
    onUploadDarkLogoMinimalInputChange() {
        this.uploadDarkLogoMinimal();
    }
    onUploadLightLogoInputChange() {
        this.uploadLightLogo();
    }
    onUploadLightLogoMinimalInputChange() {
        this.uploadLightLogoMinimal();
    }
    onUploadCustomCSSInputChange() {
        this.uploadCustomCss();
    }
    getLogo(skin: string) {
        return `${this.remoteServiceBaseUrl}/TenantCustomization/GetTenantLogo?skin=${skin}&tenantId=${this.appSession.tenant.id}`;
    }
    private refreshLogo(skin: string): void {
        const now = this._dateTimeService.getDate();
        switch (skin) {
            case 'dark':
                this.darkLogoImg.nativeElement.src = `${this.getLogo(skin)}&c=${now}`;
                break;
            case 'dark-sm':
                this.darkLogoMinimalImg.nativeElement.src = `${this.getLogo(skin)}&c=${now}`;
                break;
            case 'light':
                this.lightLogoImg.nativeElement.src = `${this.getLogo(skin)}&c=${now}`;
                break;
            case 'light-sm':
                this.lightLogoMinimalImg.nativeElement.src = `${this.getLogo(skin)}&c=${now}`;
                break;
            default:
                this.lightLogoImg.nativeElement.src = `${this.getLogo(skin)}&c=${now}`;
                break;
        }
        this._cdr.markForCheck();
    }
}
