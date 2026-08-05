import { Component, Input, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme13-brand.component.html',
    selector: 'theme13-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme13BrandComponent extends AppComponentBase {
    private document = inject<Document>(DOCUMENT);
    @Input() anchorClass = '';
    @Input() imageClass = 'h-25px logo';
    defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/applogo.png`;
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    triggerAsideToggleClickEvent(): void {
        abp.event.trigger('app.kt_aside_toggler.onClick');
    }
}
