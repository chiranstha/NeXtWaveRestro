import { ElementRef, Component, OnInit, Inject, DOCUMENT } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ThemesLayoutBaseComponent } from '@app/shared/layout/themes/themes-layout-base.component';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { AppConsts } from '@shared/AppConsts';

@Component({
    
    standalone: false,
    templateUrl: './theme3-layout.component.html',
    selector: 'theme3-layout',
    animations: [appModuleAnimation],
})
export class Theme3LayoutComponent extends ThemesLayoutBaseComponent implements OnInit {
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    constructor(@Inject(DOCUMENT) private document: Document) {
        super();
    }

    ngOnInit() {
        this.installationMode = UrlHelper.isInstallUrl(location.href);
        this.defaultLogo = AppConsts.appBaseUrl + '/assets/common/images/app-logo-on-light.svg';
    }
}
