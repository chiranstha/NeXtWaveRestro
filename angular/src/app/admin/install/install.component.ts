import { Component, OnInit, ViewEncapsulation, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmailSettingsEditDto,
    HostBillingSettingsEditDto,
    InstallDto,
    InstallServiceProxy,
    NameValue,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './install.component.html',
    animations: [appModuleAnimation],
    styleUrls: ['./install.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [FormsModule, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class InstallComponent extends AppComponentBase implements OnInit {
    private _installSettingService = inject(InstallServiceProxy);
    saving = false;
    setupSettings: InstallDto;
    languages: NameValue[];

    loadAppSettingsJson(): void {
        const self = this;
        self._installSettingService.getAppSettingsJson().subscribe((result) => {
            this.setupSettings.webSiteUrl = result.webSiteUrl;
            this.setupSettings.serverUrl = result.serverSiteUrl;
            this.languages = result.languages;
        });
    }
    init(): void {
        this._installSettingService.checkDatabase().subscribe((result) => {
            if (result.isDatabaseExist) {
                window.location.href = '/';
            }
        });
        this.setupSettings = new InstallDto();
        this.setupSettings.smtpSettings = new EmailSettingsEditDto();
        this.setupSettings.billInfo = new HostBillingSettingsEditDto();
        this.setupSettings.defaultLanguage = 'en';
        this.loadAppSettingsJson();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        const self = this;
        self.init();
    }
    saveAll(): void {
        this.saving = true;
        this._installSettingService
            .setup(this.setupSettings)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                window.location.href = '/';
            });
    }
}
