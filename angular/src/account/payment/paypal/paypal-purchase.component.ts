import { Component, Input, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ScriptLoaderService } from '@shared/utils/script-loader.service';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { XmlHttpRequestHelper } from '@shared/helpers/XmlHttpRequestHelper';
import { TenantRegistrationHelperService } from '@account/register/tenant-registration-helper.service';
import {
    EditionPaymentType,
    PaymentServiceProxy,
    PayPalConfigurationDto,
    PayPalPaymentServiceProxy,
    SubscriptionPaymentDto,
    SubscriptionPaymentGatewayType,
} from '@shared/service-proxies/service-proxies';
@Component({
    selector: 'paypal-purchase-component',
    templateUrl: './paypal-purchase.component.html',
    animations: [accountModuleAnimation()],
})
export class PayPalPurchaseComponent extends AppComponentBase implements OnInit {
    @Input() editionPaymentType: EditionPaymentType;
    config: PayPalConfigurationDto;
    paypalIsLoading = true;
    subscriptionPaymentGateway = SubscriptionPaymentGatewayType;
    totalAmount = 0;
    description = '';
    paymentId;
    redirectUrl;
    successCallbackUrl;
    errorCallbackUrl;
    private _activatedRoute: ActivatedRoute;
    private _payPalPaymentAppService: PayPalPaymentServiceProxy;
    private _paymentAppService: PaymentServiceProxy;
    private _router: Router;
    private _tenantRegistrationHelper: TenantRegistrationHelperService;
    constructor() {
        super();
        this._activatedRoute = inject(ActivatedRoute);
        this._payPalPaymentAppService = inject(PayPalPaymentServiceProxy);
        this._paymentAppService = inject(PaymentServiceProxy);
        this._router = inject(Router);
        this._tenantRegistrationHelper = inject(TenantRegistrationHelperService);
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.setTenantIdCookieIfNeeded();
        this.paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        this.redirectUrl = this._activatedRoute.snapshot.queryParams['redirectUrl'];
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
                        this.description = result.description;
                        this.totalAmount = result.amount;
                        this.successCallbackUrl = result.successUrl;
                        this.errorCallbackUrl = result.errorUrl;
                        this.subscriptionPaymentGateway = result.gateway as any;
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
                                    value: self.totalAmount,
                                    currency_code: self.appSession.application.currency,
                                },
                            },
                        ],
                    });
                },
                onApprove: (data) => {
                    self._payPalPaymentAppService.confirmPayment(self.paymentId, data.orderID).subscribe(() => {
                        XmlHttpRequestHelper.ajax(
                            'post',
                            `${
                                self.successCallbackUrl + (self.successCallbackUrl.includes('?') ? '&' : '?')
                            }paymentId=${self.paymentId}`,
                            null,
                            null,
                            () => {
                                if (self._tenantRegistrationHelper.registrationResult) {
                                    self._tenantRegistrationHelper.registrationResult.isActive = true;
                                }
                                self._router.navigate([self.redirectUrl]);
                            },
                        );
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
