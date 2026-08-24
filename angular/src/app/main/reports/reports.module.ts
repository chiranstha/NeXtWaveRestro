import { NgModule, CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ReportsRoutingModule } from './reports-routing.module';
import { AccountGroupsReportComponent } from './accountGroupReport/accountGroupReport.component';
import { AgGridFeatureModule } from '@app/shared/common/ag-grid/ag-grid-feature.module';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { AccountLedgerReportComponent } from './accountLedgerReport/accountLedgerReport.component';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { NgSelectModule } from '@ng-select/ng-select';
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

@NgModule({
    declarations: [
        AccountGroupsReportComponent,
        AccountLedgerReportComponent,
        StockReportComponent,
        ProductProfitReportComponent,
        PurchaseMasterReportComponent,
        PurchaseReturnReportComponent,
        PurchasePartyTaxReportComponent,
        ProductWiseMonthlyPurchaseComponent,
        SalesReportComponent,
        SalesReturnReportComponent,
        MaterialSalesReportComponent,
        LedgerWiseMonthlySalesReportComponent,
        ProductWiseMonthlySalesReportComponent,
        BookReportComponent,
        DaybookReportComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        ReportsRoutingModule,
        AppCommonModule,
        AppSharedModule,
        AdminSharedModule,
        SubHeaderComponent,
        AgGridFeatureModule,
        ScrollingModule,
        NgScrollbarModule,
        NepaliDatepickerModule,
        NgSelectModule,
    ],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA]
})
export class ReportsModule { }
