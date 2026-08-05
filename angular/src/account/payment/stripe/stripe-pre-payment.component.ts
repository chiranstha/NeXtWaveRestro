import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ScriptLoaderService } from '@shared/utils/script-loader.service';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import {
    StripePaymentServiceProxy,
    PaymentServiceProxy,
    SubscriptionPaymentDto,
    StripeConfigurationDto,
    SubscriptionPaymentGatewayType,
    SubscriptionStartType,
    StripeCreatePaymentSessionInput,
} from '@shared/service-proxies/service-proxies';
import { AppConsts } from '@shared/AppConsts';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { DecimalPipe } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'stripe-pre-payment-component',
    templateUrl: './stripe-pre-payment.component.html',
    animations: [accountModuleAnimation()],
    imports: [BusyIfDirective, DecimalPipe, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class StripePrePaymentComponent extends AppComponentBase implements OnInit {
    private _activatedRoute = inject(ActivatedRoute);
    private _stripePaymentAppService = inject(StripePaymentServiceProxy);
    private _paymentAppService = inject(PaymentServiceProxy);
    amount = 0;
    description = '';
    subscriptionPayment: SubscriptionPaymentDto;
    stripeIsLoading = true;
    subscriptionPaymentGateway = SubscriptionPaymentGatewayType;
    subscriptionStartType = SubscriptionStartType;
    paymentId;
    successCallbackUrl;
    errorCallbackUrl;
    payment: SubscriptionPaymentDto = new SubscriptionPaymentDto();

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.spinnerService.show();
        this.paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        this.stripeIsLoading = true;
        new ScriptLoaderService()
            .load('https://js.stripe.com/v3')
            .then(() => {
                this._stripePaymentAppService.getConfiguration().subscribe(
                    (config: StripeConfigurationDto) => {
                        this._stripePaymentAppService
                            .createPaymentSession(
                                new StripeCreatePaymentSessionInput({
                                    paymentId: this.paymentId,
                                    successUrl: `${AppConsts.appBaseUrl}/account/stripe-post-payment`,
                                    cancelUrl: `${AppConsts.appBaseUrl}/account/stripe-cancel-payment`,
                                }),
                            )
                            .subscribe(
                                (sessionId) => {
                                    this._paymentAppService.getPayment(this.paymentId).subscribe(
                                        (result: SubscriptionPaymentDto) => {
                                            this.payment = result;
                                            this.spinnerService.hide();
                                            const stripe = (<any>window).Stripe(config.publishableKey);
                                            const checkoutButton = document.getElementById('stripe-checkout');
                                            checkoutButton.addEventListener('click', () => {
                                                stripe.redirectToCheckout({ sessionId });
                                            });
                                            this.stripeIsLoading = false;
                                        },
                                        (_err) => {
                                            this.spinnerService.hide();
                                        },
                                    );
                                },
                                (_err) => {
                                    this.spinnerService.hide();
                                },
                            );
                    },
                    (_err) => {
                        this.spinnerService.hide();
                    },
                );
            })
            .catch((_err) => {
                this.spinnerService.hide();
            });
    }
}
