import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Injector, Component, ViewEncapsulation, Inject, DOCUMENT } from '@angular/core';

import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({
    
    animations: [appModuleAnimation()],

    standalone: false,
    templateUrl: './theme3-brand.component.html',
    selector: 'theme3-brand',
    
})
export class Theme3BrandComponent extends AppComponentBase {
    skin = this.appSession.theme.baseSettings.layout.darkMode ? 'dark' : 'light';
    defaultLogo = AppConsts.appBaseUrl + '/assets/common/images/app-logo-on-' + this.skin + '.svg';
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    constructor(injector: Injector, @Inject(DOCUMENT) private document: Document) {
        super(injector);
    }
}
