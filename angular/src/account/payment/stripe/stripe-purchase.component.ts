import { Component, Input, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ScriptLoaderService } from '@shared/utils/script-loader.service';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import {
    EditionPaymentType,
    PaymentServiceProxy,
    StripeConfigurationDto,
    StripeCreatePaymentSessionInput,
    StripePaymentServiceProxy,
    SubscriptionPaymentDto,
    SubscriptionPaymentGatewayType,
    SubscriptionStartType,
} from '@shared/service-proxies/service-proxies';
import { AppConsts } from '@shared/AppConsts';
@Component({
    selector: 'stripe-purchase-component',
    templateUrl: './stripe-purchase.component.html',
    animations: [accountModuleAnimation()],
})
export class StripePurchaseComponent extends AppComponentBase implements OnInit {
    @Input() editionPaymentType: EditionPaymentType;
    amount = 0;
    description = '';
    subscriptionPayment: SubscriptionPaymentDto;
    stripeIsLoading = true;
    subscriptionPaymentGateway = SubscriptionPaymentGatewayType;
    subscriptionStartType = SubscriptionStartType;
    paymentId;
    successCallbackUrl;
    errorCallbackUrl;
    private _activatedRoute: ActivatedRoute;
    private _stripePaymentAppService: StripePaymentServiceProxy;
    private _paymentAppService: PaymentServiceProxy;
    constructor() {
        super();
        this._activatedRoute = inject(ActivatedRoute);
        this._stripePaymentAppService = inject(StripePaymentServiceProxy);
        this._paymentAppService = inject(PaymentServiceProxy);
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.spinnerService.show();
        this.setTenantIdCookieIfNeeded();
        this.stripeIsLoading = true;
        this.paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        new ScriptLoaderService()
            .load('https://js.stripe.com/v3')
            .then(() => {
                this._stripePaymentAppService.getConfiguration().subscribe(
                    (config: StripeConfigurationDto) => {
                        this._stripePaymentAppService
                            .createPaymentSession(
                                new StripeCreatePaymentSessionInput({
                                    paymentId: this.paymentId,
                                    successUrl: `${AppConsts.appBaseUrl}/account/stripe-payment-result`,
                                    cancelUrl: `${AppConsts.appBaseUrl}/account/stripe-cancel-payment`,
                                }),
                            )
                            .subscribe(
                                (sessionId) => {
                                    this._paymentAppService.getPayment(this.paymentId).subscribe(
                                        (result: SubscriptionPaymentDto) => {
                                            this.spinnerService.hide();
                                            this.amount = result.amount;
                                            this.description = result.description;
                                            this.successCallbackUrl = result.successUrl;
                                            this.errorCallbackUrl = result.errorUrl;
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
    setTenantIdCookieIfNeeded(): void {
        if (!this._activatedRoute.snapshot.queryParams['tenantId']) {
            return;
        }
        const tenantId = parseInt(this._activatedRoute.snapshot.queryParams['tenantId']);
        abp.multiTenancy.setTenantIdCookie(tenantId);
    }
}
