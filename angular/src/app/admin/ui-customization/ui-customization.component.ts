import {
    ChangeDetectorRef,
    Component,
    OnInit,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ThemeSettingsDto, UiCustomizationSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective, TabHeadingDirective } from 'ngx-bootstrap/tabs';
import { DefaultThemeUiSettingsComponent } from './default-theme-ui-settings.component';
import { Theme2ThemeUiSettingsComponent } from './theme2-theme-ui-settings.component';
import { Theme3ThemeUiSettingsComponent } from './theme3-theme-ui-settings.component';
import { Theme4ThemeUiSettingsComponent } from './theme4-theme-ui-settings.component';
import { Theme5ThemeUiSettingsComponent } from './theme5-theme-ui-settings.component';
import { Theme6ThemeUiSettingsComponent } from './theme6-theme-ui-settings.component';
import { Theme7ThemeUiSettingsComponent } from './theme7-theme-ui-settings.component';
import { Theme8ThemeUiSettingsComponent } from './theme8-theme-ui-settings.component';
import { Theme9ThemeUiSettingsComponent } from './theme9-theme-ui-settings.component';
import { Theme10ThemeUiSettingsComponent } from './theme10-theme-ui-settings.component';
import { Theme11ThemeUiSettingsComponent } from './theme11-theme-ui-settings.component';
import { Theme12ThemeUiSettingsComponent } from './theme12-theme-ui-settings.component';
import { Theme13ThemeUiSettingsComponent } from './theme13-theme-ui-settings.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './ui-customization.component.html',
    styleUrls: ['./ui-customization.component.less'],
    animations: [appModuleAnimation],
    encapsulation: ViewEncapsulation.None,
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        TabHeadingDirective,
        DefaultThemeUiSettingsComponent,
        Theme2ThemeUiSettingsComponent,
        Theme3ThemeUiSettingsComponent,
        Theme4ThemeUiSettingsComponent,
        Theme5ThemeUiSettingsComponent,
        Theme6ThemeUiSettingsComponent,
        Theme7ThemeUiSettingsComponent,
        Theme8ThemeUiSettingsComponent,
        Theme9ThemeUiSettingsComponent,
        Theme10ThemeUiSettingsComponent,
        Theme11ThemeUiSettingsComponent,
        Theme12ThemeUiSettingsComponent,
        Theme13ThemeUiSettingsComponent,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class UiCustomizationComponent extends AppComponentBase implements OnInit {
    private _uiCustomizationService = inject(UiCustomizationSettingsServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    themeSettings: ThemeSettingsDto[];
    currentThemeName = '';

    getLocalizedThemeName(str: string): string {
        return this.l(`Theme_${abp.utils.toPascalCase(str)}`);
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        super.ngOnInit();
        this.currentThemeName = this.currentTheme.baseSettings.theme;
        this._uiCustomizationService.getUiManagementSettings().subscribe((settingsResult) => {
            this.themeSettings = settingsResult.sort((a, b) => {
                const aKey = a.theme === 'default' ? 0 : parseInt(a.theme.replace('theme', ''));
                const bKey = b.theme === 'default' ? 0 : parseInt(b.theme.replace('theme', ''));
                return aKey - bKey;
            });
            this._cdr.markForCheck();
        });
    }
}
