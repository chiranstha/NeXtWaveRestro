import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
const routes: Routes = [
    {
        path: '',
        loadComponent: () =>
            import('./validate-two-factor-code.component').then((m) => m.ValidateTwoFactorCodeComponent),
        pathMatch: 'full',
    },
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ValidateTwoFactorCodeRoutingModule {}
