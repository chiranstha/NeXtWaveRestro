import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ScriptLoaderService } from '@shared/utils/script-loader.service';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import {
    PayPalPaymentServiceProxy,
    SubscriptionPaymentDto,
    PaymentServiceProxy,
    PayPalConfigurationDto,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { DecimalPipe } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'paypal-pre-payment-component',
    templateUrl: './paypal-pre-payment.component.html',
    animations: [accountModuleAnimation()],
    imports: [BusyIfDirective, DecimalPipe, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class PayPalPrePaymentComponent extends AppComponentBase implements OnInit {
    private _activatedRoute = inject(ActivatedRoute);
    private _payPalPaymentAppService = inject(PayPalPaymentServiceProxy);
    private _paymentAppService = inject(PaymentServiceProxy);
    private _router = inject(Router);
    config: PayPalConfigurationDto;
    payment: SubscriptionPaymentDto = new SubscriptionPaymentDto();
    paypalIsLoading = true;
    paymentId;
    successCallbackUrl;
    errorCallbackUrl;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.setTenantIdCookieIfNeeded();
        this.paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        this._payPalPaymentAppService.getConfiguration().subscribe((config: PayPalConfigurationDto) => {
            this.config = config;
            const disabledFundings = this.GetDisabledFundingsQueryString(config);
            new ScriptLoaderService()
                .load(
                    `https://www.paypal.com/sdk/js?client-id=${config.clientId}&currency=${
                        this.appSession.application.currency
                    }${disabledFundings}`,
                )
                .then(() => {
                    this._paymentAppService.getPayment(this.paymentId).subscribe((result: SubscriptionPaymentDto) => {
                        this.payment = result;
                        this.successCallbackUrl = result.successUrl;
                        this.errorCallbackUrl = result.errorUrl;
                        this.paypalIsLoading = false;
                        this.preparePaypalButton();
                    });
                });
        });
    }
    preparePaypalButton(): void {
        const self = this;
        (<any>window).paypal
            .Buttons({
                createOrder(data, actions) {
                    return actions.order.create({
                        purchase_units: [
                            {
                                amount: {
                                    value: self.payment.totalAmount,
                                    currency_code: self.appSession.application.currency,
                                },
                            },
                        ],
                    });
                },
                onApprove: (data) => {
                    self._payPalPaymentAppService.confirmPayment(self.paymentId, data.orderID).subscribe(() => {
                        if (self.successCallbackUrl) {
                            self._router.navigate([self.successCallbackUrl], {
                                queryParams: {
                                    paymentId: self.paymentId,
                                },
                            });
                        }
                    });
                },
            })
            .render('#paypal-button');
    }
    GetDisabledFundingsQueryString(config: PayPalConfigurationDto): string {
        if (!config.disabledFundings || config.disabledFundings.length <= 0) {
            return '';
        }
        return `&disable-funding=${config.disabledFundings.join()}`;
    }
    setTenantIdCookieIfNeeded(): void {
        if (!this._activatedRoute.snapshot.queryParams['tenantId']) {
            return;
        }
        const tenantId = parseInt(this._activatedRoute.snapshot.queryParams['tenantId']);
        abp.multiTenancy.setTenantIdCookie(tenantId);
    }
}
