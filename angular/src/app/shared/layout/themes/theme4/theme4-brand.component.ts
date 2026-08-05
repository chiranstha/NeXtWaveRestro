import { Component, Input, ViewEncapsulation, DOCUMENT, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    templateUrl: './theme4-brand.component.html',
    selector: 'theme4-brand',
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None,
})
export class Theme4BrandComponent extends AppComponentBase {
    private document = inject<Document>(DOCUMENT);
    @Input() skin = 'dark';
    @Input() customStyle = 'h-55px';
    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
}
