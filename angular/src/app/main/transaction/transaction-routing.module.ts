import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PaymentMastersComponent } from './paymentMasters/paymentMasters.component';
import { AddPaymentNewComponent } from './paymentMasters/add-new-payment/add-new-payment.component';
import { ReceiptMasterComponent } from './receiptMasters/receiptMasters.component';
import { AddReceiptNewComponent } from './receiptMasters/add-receipt-new/add-receipt-new.component';
import { JournalMastersComponent } from './journalMasters/journalMasters.component';
import { AddNewJournalMasterComponent } from './journalMasters/addJournalMaster/addJournalMaster.component';
import { ContraMastersComponent } from './contraMasters/contraMasters.component';
import { AddContraMasterComponent } from './contraMasters/addContraMasters/addContraMasters.component';
import { AddPdcPayableComponent } from './pdcPayable/addPdcPayable/addPdcPayable.component';
import { PdcPayableComponent } from './pdcPayable/pdcPayable.component';
import { PdcReceivableComponent } from './pdcReceivable/pdcReceivable.component';
import { AddPdcReceivableComponent } from './pdcReceivable/addPdcReceivable/addPdcReceivable.component';
import { PdcClearanceComponent } from './pdcClearance/pdcClearance.component';
import { AddPdcClearanceComponent } from './pdcClearance/addPdcClearance/addPdcClearance.component';
import { TransactionPdfComponent } from './transaction-pdf/transaction-pdf.component';
import { RestaurantStockUsedComponent } from './restaurantStockUsed/restaurant-stock-used.component';
import { AddRestaurantStockUsedComponent } from './restaurantStockUsed/add-restaurant-stock-used/add-restaurant-stock-used.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                    {
                        path: '',
                        redirectTo: 'paymentMasters',
                        pathMatch: 'full',
                    },
                    {
                        path: 'pdf/:enum/:id',
                        component: TransactionPdfComponent,
                    },
                    {
                        path: 'restaurantStockUsed',
                        component: RestaurantStockUsedComponent,
                        data: { permission: 'Pages.Restaurant.Inventory.StockAdjustment' },
                    },
                    {
                        path: 'restaurantStockUsed/add',
                        component: AddRestaurantStockUsedComponent,
                        data: { permission: 'Pages.Restaurant.Inventory.StockAdjustment' },
                    },
                    {
                        path: 'paymentMasters',
                        component: PaymentMastersComponent,
                        data: { permission: 'Pages.PaymentMasters' },
                    },
                    {
                        path: 'paymentMasters/add', //new
                        component: AddPaymentNewComponent,
                        data: { permission: 'Pages.PaymentMasters.Create' },
                    },

                    {
                        path: 'paymentMasters/edit/:id',
                        component: AddPaymentNewComponent,
                        data: { permission: 'Pages.PaymentMasters.Edit' },
                    },
                    {
                        path: 'receiptMaster',
                        component: ReceiptMasterComponent,
                        data: { permission: 'Pages.ReceiptMasters' },
                    },
                    {
                        path: 'receiptMaster/add', //new
                        component: AddReceiptNewComponent,
                        data: { permission: 'Pages.ReceiptMasters.Create' },
                    },
                    {
                        path: 'receiptMaster/edit/:id',
                        component: AddReceiptNewComponent,
                        data: { permission: 'Pages.ReceiptMasters.Edit' },
                    },
                    {
                        path: 'receiptMasters',
                        redirectTo: 'receiptMaster',
                        pathMatch: 'full',
                    },
                    {
                        path: 'receiptMasters/add',
                        redirectTo: 'receiptMaster/add',
                        pathMatch: 'full',
                    },
                    {
                        path: 'receiptMasters/edit/:id',
                        redirectTo: 'receiptMaster/edit/:id',
                        pathMatch: 'full',
                    },
                    {
                        path: 'journalMasters',
                        component: JournalMastersComponent,
                        data: { permission: 'Pages.JournalMasters' },
                    },
                    {
                        path: 'journalMasters/add', // new
                        component: AddNewJournalMasterComponent,
                        data: { permission: 'Pages.JournalMasters.Create' },
                    },
                    {
                        path: 'journalMasters/edit/:id',
                        component: AddNewJournalMasterComponent,
                        data: { permission: 'Pages.JournalMasters.Edit' },
                    },
                     {
                        path: 'contraMasters',
                        component: ContraMastersComponent,
                        data: { permission: 'Pages.ContraMasters' },
                    },
                    {
                        path: 'contraMasters/add',
                        component: AddContraMasterComponent,
                        data: { permission: 'Pages.ContraMasters.Create' },
                    },
                    {
                        path: 'contraMasters/edit/:id',
                        component: AddContraMasterComponent,
                        data: { permission: 'Pages.ContraMasters.Edit' },
                    },
                    { path: 'PdcPayable', component: PdcPayableComponent, data: { permission: 'Pages.PDCPayables' } },
                    { path: 'pdcPayable', redirectTo: 'PdcPayable', pathMatch: 'full' },
                    {
                        path: 'PdcPayable/add',
                        component: AddPdcPayableComponent,
                        data: { permission: 'Pages.PDCPayables.Create' },
                    },
                    {
                        path: 'pdcPayable/add',
                        redirectTo: 'PdcPayable/add',
                        pathMatch: 'full',
                    },
                    {
                        path: 'PdcPayable/edit/:id',
                        component: AddPdcPayableComponent,
                        data: { permission: 'Pages.PDCPayables.Edit' },
                    },
                    {
                        path: 'pdcPayable/edit/:id',
                        redirectTo: 'PdcPayable/edit/:id',
                        pathMatch: 'full',
                    },
                    {
                        path: 'PdcReceivable',
                        component: PdcReceivableComponent,
                        data: { permission: 'Pages.PDCReceivables' },
                    },
                    { path: 'pdcReceivable', redirectTo: 'PdcReceivable', pathMatch: 'full' },
                    {
                        path: 'PdcReceivable/add',
                        component: AddPdcReceivableComponent,
                        data: { permission: 'Pages.PDCReceivables.Create' },
                    },
                    {
                        path: 'pdcReceivable/add',
                        redirectTo: 'PdcReceivable/add',
                        pathMatch: 'full',
                    },
                    {
                        path: 'PdcReceivable/edit/:id',
                        component: AddPdcReceivableComponent,
                        data: { permission: 'Pages.PDCReceivables.Edit' },
                    },
                    {
                        path: 'pdcReceivable/edit/:id',
                        redirectTo: 'PdcReceivable/edit/:id',
                        pathMatch: 'full',
                    },
                    { path: 'PdcClearance', component: PdcClearanceComponent, data: { permission: 'Pages.PDCClearances' } },
                    { path: 'pdcClearance', redirectTo: 'PdcClearance', pathMatch: 'full' },
                    {
                        path: 'PdcClearance/add',
                        component: AddPdcClearanceComponent,
                        data: { permission: 'Pages.PDCClearances.Create' },
                    },
                    {
                        path: 'pdcClearance/add',
                        redirectTo: 'PdcClearance/add',
                        pathMatch: 'full',
                    },
                    {
                        path: 'PdcClearance/edit/:id',
                        component: AddPdcClearanceComponent,
                        data: { permission: 'Pages.PDCClearances.Edit' },
                    },
                    {
                        path: 'pdcClearance/edit/:id',
                        redirectTo: 'PdcClearance/edit/:id',
                        pathMatch: 'full',
                    },
                    {
                        path: '**',
                        redirectTo: 'paymentMasters',
                    },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class TransactionRoutingModule {
}
