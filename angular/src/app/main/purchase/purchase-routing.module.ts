import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PurchaseOrderComponent } from './purchaseOrderMasters/purchaseOrderMasters.component';
import { AddPurchaseOrderMastersComponent } from './purchaseOrderMasters/add-purchase-order-masters/add-purchase-order-masters.component';
import { PurchaseMastersComponent } from './purchaseMasters/purchaseMasters.component';
import { AddPurchaseInvoiceComponent } from './purchaseMasters/add-purchase-invoice/add-purchase-invoice.component';
import { PurchaseReturnsComponent } from './purchaseReturns/purchaseReturns.component';
import { AddPurchaseInvoiceReturnComponent } from './purchaseReturns/add-purchase-invoice-return/add-purchase-invoice-return.component';
import { PurchasePdfsComponent } from './purchase-pdf/puchase-pdf.component';

@NgModule({
    imports: [RouterModule.forChild([
        {
            path: '',
            children: [
                {
                    path: 'purchaseOrderMasters',
                    component: PurchaseOrderComponent,
                    data: { permission: 'Pages.PurchaseOrderMasters' }
                },
                {
                    path: 'purchaseOrderMasters/add',
                    component: AddPurchaseOrderMastersComponent,
                    data: { permission: 'Pages.PurchaseOrderMasters.Create' },
                },
                {
                    path: 'purchaseOrderMasters/edit/:id',
                    component: AddPurchaseOrderMastersComponent,
                    data: { permission: 'Pages.PurchaseOrderMasters.Edit' },
                },
                {
                    path: 'purchaseMasters',
                    component: PurchaseMastersComponent,
                    data: { permission: 'Pages.PurchaseMasters' }
                },
                {
                    path: 'purchaseMasters/add',
                    component: AddPurchaseInvoiceComponent,
                    data: { permission: 'Pages.PurchaseMasters.Create' }
                },
                {
                    path: 'purchaseMasters/edit/:id',
                    component: AddPurchaseInvoiceComponent,
                    data: { permission: 'Pages.PurchaseMasters.Edit' }
                },
                {
                    path: 'purchaseReturns',
                    component: PurchaseReturnsComponent,
                    data: { permission: 'Pages.PurchaseReturns' }
                },
                {
                    path: 'purchaseReturns/add',
                    component: AddPurchaseInvoiceReturnComponent,
                    data: { permission: 'Pages.PurchaseReturns.Create' },
                },
                {
                    path: 'purchaseReturns/edit/:id',
                    component: AddPurchaseInvoiceReturnComponent,
                    data: { permission: 'Pages.PurchaseReturns.Edit' },
                },
                {
                    path: 'pdf/:enum/:id',
                    component: PurchasePdfsComponent
                }
            ]
        }
    ])],
    exports: [RouterModule]
})

export class PurchaseRoutingModule {

}