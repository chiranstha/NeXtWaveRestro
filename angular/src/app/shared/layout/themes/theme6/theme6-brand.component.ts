import { Component, Input, OnInit, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme6-brand.component.html',
    selector: 'theme6-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme6BrandComponent extends AppComponentBase implements OnInit {
    private document = inject<Document>(DOCUMENT);
    @Input() anchorClass = 'd-flex align-items-center';
    @Input() skin = 'dark';
    @Input() imageClass = 'h-45px logo';
    defaultLogo = '';
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    ngOnInit(): void {
        this.defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${this.skin}-sm.svg`;
    }
}
