import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { EmailActivationRoutingModule } from './email-activation-routing.module';
import { EmailActivationComponent } from './email-activation.component';
import { AccountSharedModule } from '@account/shared/account-shared.module';
@NgModule({
    imports: [
        AppSharedModule,
        AccountSharedModule,
        EmailActivationRoutingModule,
        ReactiveFormsModule,
        EmailActivationComponent,
    ],
})
export class EmailActivationModule {}
