import { CommonModule } from '@angular/common';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { UtilsModule } from '@shared/utils/utils.module';
import {
    BsDatepickerModule,
} from 'ngx-bootstrap/datepicker';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { ModalModule } from 'ngx-bootstrap/modal';
import { PopoverModule } from 'ngx-bootstrap/popover';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { AutoCompleteModule } from '@shared/ui-compat';
import { EditorModule } from '@shared/ui-compat';
import { FileUploadModule } from '@shared/ui-compat';
import { InputMaskModule } from '@shared/ui-compat';
import { PaginatorModule } from '@shared/ui-compat';
import { TableModule } from '@shared/ui-compat';
import { NgSelectModule } from '@ng-select/ng-select';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { PurchaseRoutingModule } from './purchase-routing.module';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { PurchaseOrderComponent } from './purchaseOrderMasters/purchaseOrderMasters.component';
import { AddPurchaseOrderMastersComponent } from './purchaseOrderMasters/add-purchase-order-masters/add-purchase-order-masters.component';
import { PurchaseMastersComponent } from './purchaseMasters/purchaseMasters.component';
import { AddPurchaseInvoiceComponent } from './purchaseMasters/add-purchase-invoice/add-purchase-invoice.component';
import { PurchaseReturnsComponent } from './purchaseReturns/purchaseReturns.component';
import { AddPurchaseInvoiceReturnComponent } from './purchaseReturns/add-purchase-invoice-return/add-purchase-invoice-return.component';
import { PurchasePdfsComponent } from './purchase-pdf/puchase-pdf.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
@NgModule({
    imports: [
        PurchaseRoutingModule,
        FileUploadModule,
        AutoCompleteModule,
        PaginatorModule,
        EditorModule,
        InputMaskModule,
        TableModule,
        CommonModule,
        ModalModule,
        TooltipModule,
        AppCommonModule,
        UtilsModule,
        ReactiveFormsModule,
        FormsModule,
        NepaliDatepickerModule,
        BsDatepickerModule,
        BsDropdownModule,
        PopoverModule,
        NgSelectModule,
        BsDatepickerModule,
        AdminSharedModule,
        AppSharedModule,
        
        SubHeaderComponent
    ],
    declarations: [
        PurchaseOrderComponent,
        AddPurchaseOrderMastersComponent,
        PurchaseMastersComponent,
        AddPurchaseInvoiceComponent,
        PurchaseReturnsComponent,
        AddPurchaseInvoiceReturnComponent,
        PurchasePdfsComponent
    ],
    providers: [],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA]
})

export class PurchaseModule {
}
