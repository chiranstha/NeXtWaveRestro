import { Component, Input, inject, ChangeDetectionStrategy } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
@Component({
    selector: 'quick-theme-selection',
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './quick-theme-selection.component.html',
})
export class QuickThemeSelectionComponent extends ThemesLayoutBaseComponent {
    @Input() customStyle =
        'btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px w-md-40px h-md-40px position-relative';
    @Input() iconStyle = 'fa-duotone fa-regular fa-palette fs-4';
    isQuickThemeSelectEnabled: boolean = this.setting.getBoolean('App.UserManagement.IsQuickThemeSelectEnabled');
    public constructor() {
        const _dateTimeService = inject(DateTimeService);
        super();
    }
}
