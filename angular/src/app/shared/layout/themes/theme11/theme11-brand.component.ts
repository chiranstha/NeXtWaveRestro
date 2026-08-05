import { Component, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme11-brand.component.html',
    selector: 'theme11-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme11BrandComponent extends AppComponentBase {
    private document = inject<Document>(DOCUMENT);
    skin = this.appSession.theme.baseSettings.layout.darkMode ? 'dark' : 'light';
    defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${this.skin}.svg`;
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
}
