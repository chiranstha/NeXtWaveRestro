import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard.component';

@NgModule({
    imports: [
        AppSharedModule,
        AdminSharedModule,
        DashboardRoutingModule,
        DashboardComponent,
    ],
})
export class DashboardModule {}
