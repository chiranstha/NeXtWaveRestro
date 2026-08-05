import { NgModule } from '@angular/core';
import { RolesRoutingModule } from './roles-routing.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { RolesComponent } from './roles.component';
import { CreateOrEditRoleModalComponent } from './create-or-edit-role-modal.component';
import { UsersModule } from '../users/users.module';
import { AgGridModule } from 'ag-grid-angular';
import { ReactiveFormsModule } from '@angular/forms';
@NgModule({
    imports: [
        AppSharedModule,
        AdminSharedModule,
        RolesRoutingModule,
        UsersModule,
        AgGridModule,
        ReactiveFormsModule,
        RolesComponent,
        CreateOrEditRoleModalComponent,
    ],
})
export class RolesModule {}
