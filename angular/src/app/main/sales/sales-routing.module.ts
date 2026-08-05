import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AddSalesMasterComponent } from './sales-master/add-sales-master/add-sales-master.component';
import { SalesMasterComponent } from './sales-master/sales-master.component';
import { AddSalesReturnComponent } from './sales-return/add-sales-return/add-sales-return.component';
import { SalesReturnComponent } from './sales-return/sales-return.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                    {
                        path: 'salesReturnMasters',
                        component: SalesReturnComponent,
                        data: { permission: 'Pages.SalesReturnMasters' }
                    },
                    {
                        path: 'salesReturnMasters/add',
                        component: AddSalesReturnComponent,
                        data: { permission: 'Pages.SalesReturnMasters.Create' }
                    },
                    {
                        path: 'salesReturnMasters/edit/:id',
                        component: AddSalesReturnComponent,
                        data: { permission: 'Pages.SalesReturnMasters.Edit' },
                    },
                    {
                        path: 'salesInvoiceMasters',
                        component: SalesMasterComponent,
                        data: { permission: 'Pages.SalesMasters' }
                    },
                    {
                        path: 'salesInvoiceMasters/add',
                        component: AddSalesMasterComponent,
                        data: { permission: 'Pages.SalesMasters.Create' }
                    },
                    {
                        path: 'salesInvoiceMasters/add/:id/:type',
                        component: AddSalesMasterComponent,
                        data: { permission: 'Pages.SalesMasters.Create' }
                    },
                    // { path: "salesInvoiceMasters/create", component: CreateSalesInvoiceComponent, data: { permission: "Pages.SalesMasters" } },
                    {
                        path: 'salesInvoiceMasters/edit/:id',
                        component: AddSalesMasterComponent,
                        data: { permission: 'Pages.SalesMasters.Edit' }
                    },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class SalesRoutingModule {
}
