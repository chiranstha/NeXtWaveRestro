import { AgainstModeService } from '../purchase/purchaseMasters/add-purchase-invoice/against-mode.service';
import { PaymentMastersComponent } from './paymentMasters/paymentMasters.component';
import {
    BsDatepickerConfig,
    BsDatepickerModule,
    BsDaterangepickerConfig,
    BsLocaleService
} from 'ngx-bootstrap/datepicker';

import { NgxBootstrapDatePickerConfigService } from 'assets/ngx-bootstrap/ngx-bootstrap-datepicker-config.service';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { TransactionRoutingModule } from './transaction-routing.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { AddPaymentNewComponent } from './paymentMasters/add-new-payment/add-new-payment.component';





// import { DashboardComponent } from "./dashboard/dashboard.component";



import { KeyboardShortcutsModule } from 'ng-keyboard-shortcuts';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { ReceiptMasterComponent } from './receiptMasters/receiptMasters.component';
import { AddReceiptNewComponent } from './receiptMasters/add-receipt-new/add-receipt-new.component';
import { JournalMastersComponent } from './journalMasters/journalMasters.component';
import { AddNewJournalMasterComponent } from './journalMasters/addJournalMaster/addJournalMaster.component';
import { ContraMastersComponent } from './contraMasters/contraMasters.component';
import { AddContraMasterComponent } from './contraMasters/addContraMasters/addContraMasters.component';
import { PdcPayableComponent } from './pdcPayable/pdcPayable.component';
import { AddPdcPayableComponent } from './pdcPayable/addPdcPayable/addPdcPayable.component';
import { PdcReceivableComponent } from './pdcReceivable/pdcReceivable.component';
import { AddPdcReceivableComponent } from './pdcReceivable/addPdcReceivable/addPdcReceivable.component';
import { PdcClearanceComponent } from './pdcClearance/pdcClearance.component';
import { AddPdcClearanceComponent } from './pdcClearance/addPdcClearance/addPdcClearance.component';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { TransactionPdfComponent } from './transaction-pdf/transaction-pdf.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { RestaurantStockUsedComponent } from './restaurantStockUsed/restaurant-stock-used.component';
import { AddRestaurantStockUsedComponent } from './restaurantStockUsed/add-restaurant-stock-used/add-restaurant-stock-used.component';

@NgModule({
    imports: [
        TransactionRoutingModule,
        AdminSharedModule,
        CommonModule,
        AppCommonModule,
        FormsModule,
        ReactiveFormsModule,
        NgSelectModule,
        BsDatepickerModule,
        NepaliDatepickerModule,
        RouterModule,

        SubHeaderComponent,

        KeyboardShortcutsModule,
    ],
    declarations: [
        PaymentMastersComponent,
        AddPaymentNewComponent,

        ReceiptMasterComponent,
        AddReceiptNewComponent,

        JournalMastersComponent,
        AddNewJournalMasterComponent,

        ContraMastersComponent,
        AddContraMasterComponent,

        PdcPayableComponent,
        AddPdcPayableComponent,

        PdcReceivableComponent,
        AddPdcReceivableComponent,

        PdcClearanceComponent,
        AddPdcClearanceComponent,

        RestaurantStockUsedComponent,
        AddRestaurantStockUsedComponent,

        TransactionPdfComponent
    ],
    providers: [
        AgainstModeService,
        { provide: BsDatepickerConfig, useFactory: NgxBootstrapDatePickerConfigService.getDatepickerConfig },
        { provide: BsDaterangepickerConfig, useFactory: NgxBootstrapDatePickerConfigService.getDaterangepickerConfig },
        { provide: BsLocaleService, useFactory: NgxBootstrapDatePickerConfigService.getDatepickerLocale },
    ],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA],
})
export class TransactionModule {
    constructor() {
    }
}
