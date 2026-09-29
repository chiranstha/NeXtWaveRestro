import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { RestaurantChannelsComponent } from './restaurant-channels/restaurant-channels.component';
import { RestaurantKdsComponent } from './restaurant-kds/restaurant-kds.component';
import { RestaurantInventoryComponent } from './restaurant-inventory/restaurant-inventory.component';
import { RestaurantMenuComponent } from './restaurant-menu/restaurant-menu.component';
import { RestaurantPayrollComponent } from './restaurant-payroll/restaurant-payroll.component';
import { RestaurantPosComponent } from './restaurant-pos/restaurant-pos.component';
import { RestaurantReportsComponent } from './restaurant-reports/restaurant-reports.component';
import { RestaurantSetupComponent } from './restaurant-setup/restaurant-setup.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                    { path: 'setup', component: RestaurantSetupComponent, data: { permission: 'Pages.Restaurant.Setup' } },
                    { path: 'menu', component: RestaurantMenuComponent, data: { permission: 'Pages.Restaurant.Menu' } },
                    {
                        path: 'inventory',
                        component: RestaurantInventoryComponent,
                        data: { permission: 'Pages.Restaurant.Inventory' },
                    },
                    {
                        path: 'channels',
                        component: RestaurantChannelsComponent,
                        data: { permission: 'Pages.Restaurant.Channels' },
                    },
                    { path: 'pos', component: RestaurantPosComponent, data: { permission: 'Pages.Restaurant.Pos' } },
                    { path: 'guest-orders', loadComponent: () => import('./restaurant-guest-orders.component').then((m) => m.RestaurantGuestOrdersComponent), data: { permission: 'Pages.Restaurant.Pos' } },
                    { path: 'reservations', loadComponent: () => import('./restaurant-reservations.component').then((m) => m.RestaurantReservationsComponent), data: { permission: 'Pages.Restaurant.Reservations' } },
                    { path: 'print-station', loadComponent: () => import('./restaurant-print-station.component').then((m) => m.RestaurantPrintStationComponent), data: { permission: 'Pages.Restaurant.PrinterSetup' } },
                    { path: 'kds', component: RestaurantKdsComponent, data: { permission: 'Pages.Restaurant.Kds' } },
                    { path: 'reports', component: RestaurantReportsComponent, data: { permission: 'Pages.Restaurant.Reports' } },
                    { path: 'payroll', component: RestaurantPayrollComponent, data: { permission: 'Pages.Restaurant.Payroll' } },
                    { path: '', redirectTo: 'pos', pathMatch: 'full' },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class RestaurantRoutingModule {}
