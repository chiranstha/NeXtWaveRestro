import { Component, Input, OnInit, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './default-logo.component.html',
    selector: 'default-logo',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class DefaultLogoComponent extends AppComponentBase implements OnInit {
    private document = inject<Document>(DOCUMENT);
    @Input() customHrefClass = '';
    @Input() skin = null;
    defaultLogo = '';
    defaultSmallLogo = '';
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    ngOnInit(): void {
        this.setLogoUrl();
    }
    onResize() {
        this.setLogoUrl();
    }
    setLogoUrl(): void {
        this.defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/applogo.png`;
        this.defaultSmallLogo = `${AppConsts.appBaseUrl}/assets/common/images/applogo.png`;
    }
}
