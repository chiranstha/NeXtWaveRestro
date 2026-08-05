import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
const routes: Routes = [
    {
        path: '',
        loadComponent: () =>
            import('./webhook-subscription-detail.component').then((m) => m.WebhookSubscriptionDetailComponent),
        pathMatch: 'full',
    },
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WebhookSubscriptionDetailRoutingModule {}
