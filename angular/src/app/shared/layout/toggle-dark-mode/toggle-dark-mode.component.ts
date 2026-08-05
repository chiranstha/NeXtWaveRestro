import { Component, Input, inject, ChangeDetectionStrategy } from '@angular/core';
import { UiCustomizationSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
@Component({
    selector: 'toggle-dark-mode',
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './toggle-dark-mode.component.html',
})
export class ToggleDarkModeComponent {
    private _uiCustomizationAppService = inject(UiCustomizationSettingsServiceProxy);
    @Input() isDarkModeActive = false;
    @Input() customStyle =
        'btn btn-icon btn-icon-muted btn-active-light btn-active-color-primary w-30px h-30px w-md-40px h-md-40px';
    constructor() {}
    toggleDarkMode(isDarkModeActive: boolean): void {
        this._uiCustomizationAppService.changeDarkModeOfCurrentTheme(isDarkModeActive).subscribe(() => {
            window.location.reload();
        });
    }
}
