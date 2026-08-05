import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { TenantSettingsRoutingModule } from './tenant-settings-routing.module';
import { TenantSettingsComponent } from '@app/admin/settings/tenant-settings.component';
import { NgSelectModule } from '@ng-select/ng-select';
@NgModule({
    imports: [AppSharedModule, AdminSharedModule, NgSelectModule, TenantSettingsRoutingModule, TenantSettingsComponent],
})
export class TenantSettingsModule {}
