import { AfterViewInit, Component, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ComboboxItemDto,
    CommonLookupServiceProxy,
    HostSettingsServiceProxy,
    JsonClaimMapDto,
    SendTestEmailInput,
    SettingScopes,
} from '@shared/service-proxies/service-proxies';
import { KeyValueListManagerComponent } from '@app/shared/common/key-value-list-manager/key-value-list-manager.component';
import { UntypedFormControl, FormsModule } from '@angular/forms';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { TimeZoneComboComponent } from '../../shared/common/timing/timezone-combo.component';
import { NgClass } from '@angular/common';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { PasswordInputWithShowButtonComponent } from '../../shared/common/password-input-with-show-button/password-input-with-show-button.component';
import { KeyValueListManagerComponent as KeyValueListManagerComponent_1 } from '../../shared/common/key-value-list-manager/key-value-list-manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './host-settings.component.html',
    styleUrls: ['./host-settings.component.css'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        TimeZoneComboComponent,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        PasswordInputWithShowButtonComponent,
        KeyValueListManagerComponent_1,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class HostSettingsComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _hostSettingService = inject(HostSettingsServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    @ViewChild('wsFederationClaimsMappingManager') wsFederationClaimsMappingManager: KeyValueListManagerComponent;
    @ViewChild('openIdConnectClaimsMappingManager') openIdConnectClaimsMappingManager: KeyValueListManagerComponent;
    @ViewChild('emailSmtpSettingsForm') emailSmtpSettingsForm: UntypedFormControl;
    @ViewChild('userManagementSettingsForm') userManagementSettingsForm: UntypedFormControl;
    @ViewChild('securitySettingsForm') securitySettingsForm: UntypedFormControl;
    @ViewChild('emailSmtpSettingsTestForm') emailSmtpSettingsTestForm: any;
    loading = false;
    hostSettings: any;
    editions: ComboboxItemDto[] = undefined;
    testEmailAddress: string = undefined;
    showTimezoneSelection = abp.clock?.provider?.supportsMultipleTimezone ?? false;
    defaultTimezoneScope: SettingScopes = SettingScopes.Application;
    usingDefaultTimeZone = false;
    initialTimeZone: string = undefined;
    isFormValid = false;
    enabledSocialLoginSettings: string[];
    wsFederationClaimMappings: { key: string; value: string }[];
    openIdConnectClaimMappings: { key: string; value: string }[];
    initialEmailSettings: string;
    openIdConnectResponseTypeCode: boolean;
    openIdConnectResponseTypeToken: boolean;
    openIdConnectResponseTypeIdToken: boolean;

    loadHostSettings(): void {
        const self = this;
        self._hostSettingService.getAllSettings().subscribe((setting) => {
            self.hostSettings = setting;
            self.initialTimeZone = setting.general.timezone;
            self.usingDefaultTimeZone =
                setting.general.timezoneForComparison === self.setting.get('Abp.Timing.TimeZone');
            this.wsFederationClaimMappings =
                this.hostSettings.externalLoginProviderSettings.openIdConnectClaimsMapping.map((item) => ({
                    key: item.key,
                    value: item.claim,
                }));
            this.openIdConnectClaimMappings =
                this.hostSettings.externalLoginProviderSettings.openIdConnectClaimsMapping.map((item) => ({
                    key: item.key,
                    value: item.claim,
                }));
            if (setting.externalLoginProviderSettings.openIdConnect.responseType) {
                const openIdConnectResponseTypes =
                    setting.externalLoginProviderSettings.openIdConnect.responseType.split(',');
                this.openIdConnectResponseTypeCode = openIdConnectResponseTypes.indexOf('code') > -1;
                this.openIdConnectResponseTypeIdToken = openIdConnectResponseTypes.indexOf('id_token') > -1;
                this.openIdConnectResponseTypeToken = openIdConnectResponseTypes.indexOf('token') > -1;
            }
            this.initialEmailSettings = JSON.stringify(self.hostSettings.email);
        });
    }
    loadEditions(): void {
        const self = this;
        self._commonLookupService.getEditionsForCombobox(false).subscribe((result) => {
            self.editions = result.items;
            const notAssignedEdition = new ComboboxItemDto();
            notAssignedEdition.value = null;
            notAssignedEdition.displayText = self.l('NotAssigned');
            self.editions.unshift(notAssignedEdition);
        });
    }
    init(): void {
        const self = this;
        self.testEmailAddress = self.appSession.user.emailAddress;
        self.showTimezoneSelection = abp.clock?.provider?.supportsMultipleTimezone ?? false;
        self.loadHostSettings();
        self.loadEditions();
        self.loadSocialLoginSettings();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        const self = this;
        self.init();
    }
    ngAfterViewInit(): void {
        if (this.emailSmtpSettingsTestForm) {
            this.emailSmtpSettingsTestForm.form.valueChanges.subscribe(() => {
                this.isFormValid = this.emailSmtpSettingsTestForm.form.valid;
            });
        }
    }
    sendTestEmail(): void {
        const self = this;
        const input = new SendTestEmailInput();
        input.emailAddress = self.testEmailAddress;
        if (this.initialEmailSettings !== JSON.stringify(this.hostSettings.email)) {
            this.message.confirm(this.l('SendEmailWithSavedSettingsWarning'), this.l('AreYouSure'), (isConfirmed) => {
                if (isConfirmed) {
                    self._hostSettingService.sendTestEmail(input).subscribe(() => {
                        self.notify.info(self.l('TestEmailSentSuccessfully'));
                    });
                }
            });
        } else {
            self._hostSettingService.sendTestEmail(input).subscribe(() => {
                self.notify.info(self.l('TestEmailSentSuccessfully'));
            });
        }
    }
    mapClaims(): void {
        if (this.wsFederationClaimsMappingManager) {
            this.hostSettings.externalLoginProviderSettings.wsFederationClaimsMapping =
                this.wsFederationClaimsMappingManager.getItems().map(
                    (item) =>
                        new JsonClaimMapDto({
                            key: item.key,
                            claim: item.value,
                        }),
                );
        }
        if (this.openIdConnectClaimsMappingManager) {
            this.hostSettings.externalLoginProviderSettings.openIdConnectClaimsMapping =
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
        if (!this.isUserManagementSettingsFormValid()) {
            return;
        }
        if (!this.isSecuritySettingsFormValid()) {
            return;
        }
        const self = this;
        self.hostSettings.externalLoginProviderSettings.openIdConnect.responseType =
            this.getSelectedOpenIdConnectResponseTypes();
        self.mapClaims();
        if (
            !self.hostSettings.tenantManagement.defaultEditionId ||
            self.hostSettings.tenantManagement.defaultEditionId.toString() === 'null'
        ) {
            self.hostSettings.tenantManagement.defaultEditionId = null;
        }
        self._hostSettingService.updateAllSettings(self.hostSettings).subscribe(() => {
            self.notify.info(self.l('SavedSuccessfully'));
            if (
                abp.clock?.provider?.supportsMultipleTimezone &&
                self.usingDefaultTimeZone &&
                self.initialTimeZone !== self.hostSettings.general.timezone
            ) {
                self.message.info(self.l('TimeZoneSettingChangedRefreshPageNotification')).then(() => {
                    window.location.reload();
                });
            }
            this.initialEmailSettings = JSON.stringify(self.hostSettings.email);
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
    loadSocialLoginSettings(): void {
        const self = this;
        this._hostSettingService.getEnabledSocialLoginSettings().subscribe((setting) => {
            self.enabledSocialLoginSettings = setting.enabledSocialLoginSettings;
        });
    }
    isSocialLoginEnabled(name: string): boolean {
        return this.enabledSocialLoginSettings && this.enabledSocialLoginSettings.indexOf(name) !== -1;
    }
    isSmtpSettingsFormValid(): boolean {
        return this.emailSmtpSettingsForm.valid;
    }
    isUserManagementSettingsFormValid(): boolean {
        return this.userManagementSettingsForm.valid;
    }
    isSecuritySettingsFormValid(): boolean {
        return this.securitySettingsForm.valid;
    }
}
