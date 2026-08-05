import { Component, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme8-brand.component.html',
    selector: 'theme8-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme8BrandComponent extends AppComponentBase {
    private document = inject<Document>(DOCUMENT);
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
}
