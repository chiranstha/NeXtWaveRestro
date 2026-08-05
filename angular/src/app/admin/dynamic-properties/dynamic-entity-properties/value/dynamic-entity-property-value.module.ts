import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { DynamicEntityPropertyValueRoutingModule } from './dynamic-entity-property-value-routing.module';
import { ManageValuesModalComponent } from './manage-values-modal.component';
import { DynamicEntityPropertyValueComponent } from './dynamic-entity-property-value.component';
import { ManagerComponent } from './manager.component';
@NgModule({
    imports: [
        AppSharedModule,
        AdminSharedModule,
        DynamicEntityPropertyValueRoutingModule,
        ManageValuesModalComponent,
        DynamicEntityPropertyValueComponent,
        ManagerComponent,
    ],
    exports: [ManageValuesModalComponent, ManagerComponent],
})
export class DynamicEntityPropertyValueModule {}
