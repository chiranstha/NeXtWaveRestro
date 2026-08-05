import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
const routes: Routes = [
    {
        path: '',
        loadComponent: () =>
            import('./manage-dynamic-entity-property-modal.component').then(
                (m) => m.ManageDynamicEntityPropertyModalComponent,
            ),
        pathMatch: 'full',
    },
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class DynamicEntityPropertiesRoutingModule {}
