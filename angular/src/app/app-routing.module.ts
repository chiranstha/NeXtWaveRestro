import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AppRouteGuard } from './shared/common/auth/auth-route-guard';
import { NetworkAwarePreloadingStrategy } from './network-aware-preloading.strategy';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: 'app',
                loadComponent: () => import('./app.component').then((m) => m.AppComponent),
                canActivate: [AppRouteGuard],
                canActivateChild: [AppRouteGuard],
                children: [
                    {
                        path: '',
                        children: [
                            {
                                path: 'notifications',
                                loadComponent: () =>
                                    import('./shared/layout/notifications/notifications.component').then(
                                        (m) => m.NotificationsComponent,
                                    ),
                            },
                            { path: '', redirectTo: '/app/main/restaurant/pos', pathMatch: 'full' },
                        ],
                    },
                    {
                        path: 'main',
                        loadChildren: () => import('app/main/main.module').then((m) => m.MainModule),
                        data: { preload: true }, // Preload main module after initial load
                    },

                    {
                        path: 'admin',
                        loadChildren: () => import('app/admin/admin.module').then((m) => m.AdminModule),
                        data: { preload: false }, // Don't preload admin (restricted access)
                        canLoad: [AppRouteGuard],
                    },
                    {
                        path: '**',
                        redirectTo: 'notifications',
                    },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
    providers: [NetworkAwarePreloadingStrategy],
})
export class AppRoutingModule {}
