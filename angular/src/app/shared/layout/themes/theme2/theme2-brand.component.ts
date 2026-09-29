import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Injector, Component, ViewEncapsulation, Inject, DOCUMENT } from '@angular/core';

import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({
    
    animations: [appModuleAnimation],

    standalone: false,
    templateUrl: './theme2-brand.component.html',
    selector: 'theme2-brand',
    
})
export class Theme2BrandComponent extends AppComponentBase {
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    constructor(injector: Injector, @Inject(DOCUMENT) private document: Document) {
        super(injector);
    }
}
