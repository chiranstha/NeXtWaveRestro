import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { AccountSharedModule } from '@account/shared/account-shared.module';
import { SessionLockScreenRoutingModule } from './session-lock-screen-routing.module';
import { SessionLockScreenComponent } from './session-lock-screen.component';
@NgModule({
    imports: [AppSharedModule, AccountSharedModule, SessionLockScreenRoutingModule, SessionLockScreenComponent],
})
export class SessionLockScreenModule {}
