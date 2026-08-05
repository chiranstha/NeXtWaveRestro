import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { InstallRoutingModule } from './install-routing-module';
import { InstallComponent } from './install.component';
@NgModule({
    imports: [AppSharedModule, AdminSharedModule, InstallRoutingModule, InstallComponent],
})
export class InstallModule {}
