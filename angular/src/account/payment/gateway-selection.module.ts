import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { AccountSharedModule } from '@account/shared/account-shared.module';
import { BuyRoutingModule } from './gateway-selection-routing.module';
import { GatewaySelectionComponent } from './gateway-selection.component';
@NgModule({
    imports: [AppSharedModule, AccountSharedModule, BuyRoutingModule, GatewaySelectionComponent],
})
export class GatewaySelectionModule {}
