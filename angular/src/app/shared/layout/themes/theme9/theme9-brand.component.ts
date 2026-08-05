import { Component, Input, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme9-brand.component.html',
    selector: 'theme9-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme9BrandComponent extends AppComponentBase {
    private document = inject<Document>(DOCUMENT);
    @Input() imageClass = 'h-40px';
    skin = this.appSession.theme.baseSettings.layout.darkMode ? 'dark' : 'light';
    defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${this.skin}-sm.svg`;
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
}
