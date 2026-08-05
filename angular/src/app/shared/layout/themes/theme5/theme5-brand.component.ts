import { Component, OnInit, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme5-brand.component.html',
    selector: 'theme5-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme5BrandComponent extends AppComponentBase implements OnInit {
    private document = inject<Document>(DOCUMENT);
    defaultLogo = '';
    skin = this.currentTheme.baseSettings.layout.darkMode ? 'dark' : 'light';
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    ngOnInit(): void {
        this.defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${this.skin}.svg`;
    }
}
