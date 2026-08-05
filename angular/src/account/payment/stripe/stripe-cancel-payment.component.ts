import { Component, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'stripe-cancel-payment',
    templateUrl: './stripe-cancel-payment.component.html',
    imports: [LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class StripeCancelPaymentComponent extends AppComponentBase {
    constructor() {
        super();
    }
}
