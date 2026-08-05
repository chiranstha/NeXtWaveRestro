import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { AccountSharedModule } from '@account/shared/account-shared.module';
import { PasswordlessLoginRoutingModule } from './passwordless-login-routing.module';
import { PasswordlessLoginComponent } from './passwordless-login.component';
@NgModule({
    imports: [AppSharedModule, AccountSharedModule, PasswordlessLoginRoutingModule, PasswordlessLoginComponent],
})
export class PasswordlessLoginModule {}
