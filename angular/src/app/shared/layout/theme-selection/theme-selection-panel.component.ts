import { Component, ViewEncapsulation, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UiCustomizationSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { NgClass } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './theme-selection-panel.component.html',
    selector: 'theme-selection-panel',
    encapsulation: ViewEncapsulation.None,
    imports: [NgClass, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ThemeSelectionPanelComponent extends AppComponentBase implements OnInit {
    private _uiCustomizationService = inject(UiCustomizationSettingsServiceProxy);
    currentThemeName = '';

    ngOnInit() {
        const configuredTheme = this.currentTheme.baseSettings.theme;
        this.currentThemeName = ['default', 'theme8', 'theme11'].includes(configuredTheme)
            ? configuredTheme
            : 'default';
    }
    getLocalizedThemeName(str: string): string {
        return this.l(`Theme_${abp.utils.toPascalCase(str)}`);
    }
    changeTheme(themeName: string) {
        this._uiCustomizationService.changeThemeWithDefaultValues(themeName).subscribe(() => {
            window.location.reload();
        });
    }
}
