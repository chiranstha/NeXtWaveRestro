import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AppRouteGuard } from '@app/shared/common/auth/auth-route-guard';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                    {
                        path: 'dashboard',
                        loadChildren: () => import('./dashboard/dashboard.module').then((m) => m.DashboardModule),
                        data: { permission: 'Pages.Restaurant.Reports' },
                    },
                    {
                        path: 'accounting',
                        loadChildren: () =>
                            import('app/main/accounting/accounting.module').then((m) => m.AccountingModule),
                    },
                    {
                        path: 'inventory',
                        loadChildren: () =>
                            import('app/main/inventory/inventory.module').then((m) => m.InventoryModule),
                    },
                    {
                        path: 'purchase',
                        loadChildren: () => import('app/main/purchase/purchase.module').then((m) => m.PurchaseModule),
                    },
                    {
                        path: 'sales',
                        loadChildren: () => import('app/main/sales/sales.module').then((m) => m.SalesModule),
                    },
                    {
                        path: 'restaurant',
                        loadChildren: () =>
                            import('app/main/restaurant/restaurant.module').then((m) => m.RestaurantModule),
                    },
                    {
                        path: 'transaction',
                        loadChildren: () =>
                            import('app/main/transaction/transaction.module').then((m) => m.TransactionModule),
                    },
                    {
                        path: 'financialStatement',
                        loadChildren: () =>
                            import('app/main/financialStatement/financialStatement.module').then(
                                (m) => m.FinancialStatementModule,
                            ),
                        data: { preload: false },
                    },
                    {
                        path: 'reports',
                        loadChildren: () => import('app/main/reports/reports.module').then((m) => m.ReportsModule),
                        canLoad: [AppRouteGuard],
                    },
                    { path: '', redirectTo: 'restaurant/pos', pathMatch: 'full' },
                    { path: '**', redirectTo: 'restaurant/pos' },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class MainRoutingModule {}
