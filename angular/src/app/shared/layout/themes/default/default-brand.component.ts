import { Component, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DefaultLogoComponent } from './default-logo.component';
@Component({
    templateUrl: './default-brand.component.html',
    selector: 'default-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [DefaultLogoComponent],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DefaultBrandComponent extends AppComponentBase {
    private document = inject<Document>(DOCUMENT);
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    getMenuSkin(): string {
        return this.appSession.theme.baseSettings.layout.darkMode ||
            this.appSession.theme.baseSettings.menu.asideSkin === 'dark'
            ? 'dark'
            : 'light';
    }
}
