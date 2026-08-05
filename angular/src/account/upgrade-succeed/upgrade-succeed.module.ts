import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { UpgradeSucceedRoutingModule } from './upgrade-succeed-routing.module';
import { UpgradeSucceedComponent } from './upgrade-succeed.component';
import { AccountSharedModule } from '@account/shared/account-shared.module';
@NgModule({
    imports: [AppSharedModule, AccountSharedModule, UpgradeSucceedRoutingModule, UpgradeSucceedComponent],
})
export class UpgradeSucceedModule {}
