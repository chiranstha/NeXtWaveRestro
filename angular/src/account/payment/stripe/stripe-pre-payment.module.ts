import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { AccountSharedModule } from '@account/shared/account-shared.module';
import { StripePrePaymentRoutingModule } from './stripe-pre-payment-routing.module';
import { StripePrePaymentComponent } from './stripe-pre-payment.component';
@NgModule({
    imports: [AppSharedModule, AccountSharedModule, StripePrePaymentRoutingModule, StripePrePaymentComponent],
})
export class StripePrePaymentModule {}
