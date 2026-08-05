import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./stripe-post-payment.component').then((m) => m.StripePostPaymentComponent),
        pathMatch: 'full',
    },
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class StripePostPaymentRoutingModule {}
