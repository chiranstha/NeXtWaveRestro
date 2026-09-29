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
import { Theme8ThemeUiSettingsComponent } from './theme8-theme-ui-settings.component';
import { Theme11ThemeUiSettingsComponent } from './theme11-theme-ui-settings.component';
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
        Theme8ThemeUiSettingsComponent,
        Theme11ThemeUiSettingsComponent,
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
        const configuredTheme = this.currentTheme.baseSettings.theme;
        this.currentThemeName = ['default', 'theme8', 'theme11'].includes(configuredTheme)
            ? configuredTheme
            : 'default';
        this._uiCustomizationService.getUiManagementSettings().subscribe((settingsResult) => {
            this.themeSettings = settingsResult
                .filter((setting) => ['default', 'theme8', 'theme11'].includes(setting.theme))
                .sort((a, b) => {
                    const aKey = a.theme === 'default' ? 0 : parseInt(a.theme.replace('theme', ''));
                    const bKey = b.theme === 'default' ? 0 : parseInt(b.theme.replace('theme', ''));
                    return aKey - bKey;
                });
            this._cdr.markForCheck();
        });
    }
}
