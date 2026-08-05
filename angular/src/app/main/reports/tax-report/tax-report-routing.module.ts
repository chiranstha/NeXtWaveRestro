import {NgModule} from '@angular/core';
import {RouterModule, Routes} from '@angular/router';
import {PurchaseRegisterNewComponent} from './purchase-register-new/purchase-register-new.component';
import {PurchaseRegisterComponent} from './purchase-register/purchase-register.component';
import {PurchaseTaxReportComponent} from './purchase-tax-report/purchase-tax-report.component';
import {SalesRegisterNewComponent} from './sales-register-new/sales-register-new.component';
import {SalesRegisterComponent} from './sales-register/sales-register.component';
import {SalesTaxReportComponent} from './sales-tax-report/sales-tax-report.component';
import {TaxForLakhsPurchaseComponent} from './tax-for-lakhs-purchase/tax-for-lakhs-purchase.component';
import {TaxForLakhsSalesComponent} from './tax-for-lakhs-sales/tax-for-lakhs-sales.component';
import {TDSReportAdvanceComponent} from './tds-report-advance/tds-report-advance.component';
import {TDSReportComponent} from './tds-report/tds-report.component';
import {VatSummaryNewComponent} from './vat-summary-new/vat-summary-new.component';

const routes: Routes = [];

@NgModule({
    imports: [RouterModule.forChild([
        {
            path: '',
            children: [
                {
                    path: 'salesRegister',
                    component: SalesRegisterComponent,
                    data: {permission: 'Pages.TaxSalesRegisterReport'}
                },
                {
                    path: 'salesRegisterNew',
                    component: SalesRegisterNewComponent,
                    data: {permission: 'Pages.TaxSalesRegisterReport'}
                },
                {
                    path: 'purchaseRegister',
                    component: PurchaseRegisterComponent,
                    data: {permission: 'Pages.TaxPurchaseRegisterReport'}
                },
                {path: 'purchaseRegisterNew', component: PurchaseRegisterNewComponent, data: {permission: 'TaxPurchaseRegisterReport'}},
                {
                    path: 'vatSummaryReport',
                    component: VatSummaryNewComponent,
                    data: {permission: 'Pages.VatSummaryReport'}
                },
                {path: 'vatSummaryNewReport', component: VatSummaryNewComponent, data: {permission: 'Pages.VatSummaryReport'}},
                {path: 'TDSReport', component: TDSReportComponent, data: {permission: 'Pages.TdsReport'}},
                {path: 'TDSReportAdvance', component: TDSReportAdvanceComponent, data: {permission: 'Pages.TdsReport'}},
                {path: 'salesAboveLakhsReport', component: TaxForLakhsSalesComponent, data: {permission: 'Pages.SalesAboveLakhsReport'}},
                {path: 'purchaseAboveLakhsReport', component: TaxForLakhsPurchaseComponent, data: {permission: 'Pages.PurchaseAboveLakhsReport'}},
                {path: 'salesTaxReport', component: SalesTaxReportComponent, data: {permission: 'Pages.SalesTaxReport'}},
                {path: 'purchaseTaxReport', component: PurchaseTaxReportComponent, data: {permission: 'Pages.PurchaseTaxReport'}},
            ],
        },
    ])],
    exports: [RouterModule]
})
export class TaxReportRoutingModule {
}
