import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AccountGroupsReportComponent } from './accountGroupReport/accountGroupReport.component';
import { AccountLedgerReportComponent } from './accountLedgerReport/accountLedgerReport.component';
import { StockReportComponent } from './stockReport/stockReport.component';
import { ProductProfitReportComponent } from './productProfitReport/productProfitReport.component';
import { PurchaseMasterReportComponent } from './purchaseMasterReport/purchaseMasterReport.component';
import { PurchaseReturnReportComponent } from './purchaseReturnReport/purchaseReturnReport.component';
import { PurchasePartyTaxReportComponent } from './purchasePartyTaxReport/purchasePartyTaxReport.component';
import { ProductWiseMonthlyPurchaseComponent } from './productWiseMonthlyPuchase/productWiseMonthlyPurchase.component';
import { SalesReportComponent } from './salesReport/salesReport.component';
import { SalesReturnReportComponent } from './salesReturnReport/salesReturnReport.component';
import { MaterialSalesReportComponent } from './materialSalesReport/materialSalesReport.component';
import { LedgerWiseMonthlySalesReportComponent } from './ledgerWiseMonthlySalesReport/ledgerWiseMonthlySalesReport.component';
import { ProductWiseMonthlySalesReportComponent } from './productWiseMonthlySalesReport/productWiseMonthlySalesReport.component';
import { BookReportComponent } from './bookReport/bookReport.component';
import { DaybookReportComponent } from './daybookReport/daybookReport.component';

const routes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                redirectTo: 'daybook',
                pathMatch: 'full'
            },
            {
                path: 'account-group',
                component: AccountGroupsReportComponent,
                data: { permission: 'Pages.AccountGroupReport' }
            },
            {
                path: 'account-ledger',
                component: AccountLedgerReportComponent,
                data: { permission: 'Pages.AccountLedgerReport' }
            },
            {
                path: 'book-report',
                component: BookReportComponent,
                data: { permission: 'Pages.BookReport' }
            },
            {
                path: 'daybook',
                component: DaybookReportComponent,
                data: { permission: 'Pages.BookReport' }
            },
            {
                path: 'stock-report',
                component: StockReportComponent,
                data: { permission: 'Pages.StockReport' }
            },
            {
                path: 'product-profit',
                component: ProductProfitReportComponent,
                data: { permission: 'Pages.ProductProfitReport' }
            },
            {
                path: 'purchase-report',
                component: PurchaseMasterReportComponent,
                data: { permission: 'Pages.PurchaseReport' }
            },
            {
                path: 'purchase-return',
                component: PurchaseReturnReportComponent,
                data: { permission: 'Pages.PurchaseReturnReport' }
            },
            {
                path: 'purchase-party-tax',
                component: PurchasePartyTaxReportComponent,
                data: {permission: 'Pages.TaxableCustomerReport'}
            },
            {
                path: 'productWiseMonthlyPurchase',
                component: ProductWiseMonthlyPurchaseComponent,
                data: {permission: 'Pages.ProductWiseMonthlyReport'},
            },
            {
                path: 'sales-report',
                component: SalesReportComponent,
                data: { permission: 'Pages.SalesMasterReport'}
            },
            {
                path: 'sales-return-report',
                component: SalesReturnReportComponent,
                data: { permission: 'Pages.SalesReturnReport'}
            },
             {
                path: 'material-sales-report',
                component: MaterialSalesReportComponent,
                data: {permission: 'Pages.MaterialSalesReport'}
            },
            {
                path: 'ledger-wise-sales',
                component: LedgerWiseMonthlySalesReportComponent,
                data: {permission: 'Pages.LedgerWiseSalesReport'},
            },
            {
                path: 'product-wise-sales',
                component: ProductWiseMonthlySalesReportComponent,
                data: {permission: 'Pages.ProductWiseSalesReport'},
            },
            {
                path: 'tax-report',
                loadChildren: () =>
                    import('app/main/reports/tax-report/tax-report.module').then((m) => m.TaxReportModule),
            },
        ],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
    providers: []
})
export class ReportsRoutingModule {
}

