import {CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA} from '@angular/core';
import {CommonModule} from '@angular/common';
import {TaxReportRoutingModule} from './tax-report-routing.module';
import {SalesRegisterComponent} from './sales-register/sales-register.component';
import {SalesRegisterNewComponent} from './sales-register-new/sales-register-new.component';
import {PurchaseRegisterComponent} from './purchase-register/purchase-register.component';
import {PurchaseRegisterNewComponent} from './purchase-register-new/purchase-register-new.component';
import {TDSReportComponent} from './tds-report/tds-report.component';
import {TDSReportAdvanceComponent} from './tds-report-advance/tds-report-advance.component';
import {PurchaseTaxReportComponent} from './purchase-tax-report/purchase-tax-report.component';
import {SalesTaxReportComponent} from './sales-tax-report/sales-tax-report.component';
import {AppCommonModule} from '@app/shared/common/app-common.module';
import {VatSummaryNewComponent} from './vat-summary-new/vat-summary-new.component';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {TaxForLakhsSalesComponent} from './tax-for-lakhs-sales/tax-for-lakhs-sales.component';
import {TaxForLakhsPurchaseComponent} from './tax-for-lakhs-purchase/tax-for-lakhs-purchase.component';

import {NgSelectModule} from '@ng-select/ng-select';
import {NepaliDatepickerModule} from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AgGridModule } from 'ag-grid-angular';


@NgModule({
    declarations: [
        SalesRegisterComponent,
        SalesRegisterNewComponent,
        PurchaseRegisterComponent,
        PurchaseRegisterNewComponent,
        VatSummaryNewComponent,
        TDSReportComponent,
        TDSReportAdvanceComponent,
        PurchaseTaxReportComponent,
        SalesTaxReportComponent,
        TaxForLakhsSalesComponent,
        TaxForLakhsPurchaseComponent,
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        NepaliDatepickerModule,
        NgSelectModule,
        AppCommonModule,
        TaxReportRoutingModule,
        AdminSharedModule,
        AgGridModule
    ],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA],
})
export class TaxReportModule {
}
