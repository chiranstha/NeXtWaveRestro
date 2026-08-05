import { Component, Input, OnInit, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme3-brand.component.html',
    selector: 'theme3-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme3BrandComponent extends AppComponentBase implements OnInit {
    private document = inject<Document>(DOCUMENT);
    @Input() logoSize = '';
    defaultLogo = '';
    skin = this.currentTheme.baseSettings.layout.darkMode ? 'dark' : 'light';
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    ngOnInit(): void {
        this.defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${this.skin}${
            this.logoSize ? `-${this.logoSize}` : ''
        }.svg`;
    }
    clickTopbarToggle(): void {
        this.document.body.classList.toggle('topbar-mobile-on');
    }
    clickLeftAsideHideToggle(): void {
        this.document.body.classList.toggle('header-menu-wrapper-on');
    }
}
