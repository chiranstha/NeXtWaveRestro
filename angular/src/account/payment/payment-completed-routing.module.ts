import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./payment-completed.component').then((m) => m.PaymentCompletedComponent),
        pathMatch: 'full',
    },
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PaymentCompletedRoutingModule {}
