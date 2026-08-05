import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NetworkAwarePreloadingStrategy } from './app/network-aware-preloading.strategy';
const routes: Routes = [
    { path: '', redirectTo: '/app/main/restaurant/pos', pathMatch: 'full' },
    {
        path: 'account',
        loadChildren: () => import('account/account.module').then((m) => m.AccountModule), //Lazy load account module
    },
    { path: '**', redirectTo: '/app/main/restaurant/pos' },
];
@NgModule({
    imports: [
        RouterModule.forRoot(routes, {
            preloadingStrategy: NetworkAwarePreloadingStrategy,
            scrollPositionRestoration: 'top',
            anchorScrolling: 'enabled',
        }),
    ],
    exports: [RouterModule],
    providers: [NetworkAwarePreloadingStrategy],
})
export class RootRoutingModule {}
