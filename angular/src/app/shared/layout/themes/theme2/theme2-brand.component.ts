import { Component, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme2-brand.component.html',
    selector: 'theme2-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme2BrandComponent extends AppComponentBase {
    private document = inject<Document>(DOCUMENT);
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
    skin = this.currentTheme.baseSettings.layout.darkMode ? 'dark' : 'light';
}
