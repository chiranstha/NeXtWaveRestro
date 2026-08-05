import { Component, OnInit, Injector, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { StripePaymentServiceProxy } from '@shared/service-proxies/service-proxies';
import { ActivatedRoute, Router } from '@angular/router';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'stripe-post-payment',
    templateUrl: './stripe-post-payment.component.html',
    imports: [BusyIfDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class StripePostPaymentComponent extends AppComponentBase implements OnInit {
    private _stripePaymentService = inject(StripePaymentServiceProxy);
    private _activatedRoute = inject(ActivatedRoute);
    private _router = inject(Router);
    paymentId: number;
    controlTimeout = 1000 * 5;
    maxControlCount = 5;
    constructor() {
        const _injector = inject(Injector);
        super();
    }
    ngOnInit() {
        this.paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        this.getPaymentResult();
    }
    getPaymentResult(): void {
        this._stripePaymentService.getPaymentResult(this.paymentId).subscribe(
            (paymentResult) => {
                if (paymentResult.paymentDone) {
                    if (paymentResult.callbackUrl) {
                        this._router.navigate([paymentResult.callbackUrl], {
                            queryParams: {
                                paymentId: this.paymentId,
                            },
                        });
                    }
                } else {
                    this.controlAgain();
                }
            },
            (_err) => {
                this.controlAgain();
            },
        );
    }
    controlAgain() {
        if (this.maxControlCount === 0) {
            return;
        }
        setTimeout(() => {
            this.getPaymentResult();
        }, this.controlTimeout);
        this.controlTimeout *= 2;
        this.maxControlCount--;
    }
}
