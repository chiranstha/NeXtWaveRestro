import { CommonModule } from '@angular/common';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppCommonModule } from '@app/shared/common/app-common.module';

import { UtilsModule } from '@shared/utils/utils.module';

import { NgxBootstrapDatePickerConfigService } from 'assets/ngx-bootstrap/ngx-bootstrap-datepicker-config.service';

// import { DashboardComponent } from "./dashboard/dashboard.component";
import {
    BsDatepickerConfig,
    BsDatepickerModule,
    BsDaterangepickerConfig,
    BsLocaleService
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
import { AgainstModeService } from '../purchase/purchaseMasters/add-purchase-invoice/against-mode.service';
import { AddSalesMasterComponent } from './sales-master/add-sales-master/add-sales-master.component';
import { SalesMasterComponent } from './sales-master/sales-master.component';
import { AddSalesReturnComponent } from './sales-return/add-sales-return/add-sales-return.component';
import { SalesReturnComponent } from './sales-return/sales-return.component';
import { SalesRoutingModule } from './sales-routing.module';
import { NgSelectModule } from '@ng-select/ng-select';

import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';
import { KeyboardShortcutsModule } from 'ng-keyboard-shortcuts';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { DragDropModule } from '@shared/ui-compat';
import { TreeTableModule } from '@shared/ui-compat';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { SalesPdfComponent } from './sales-pdf/sales-pdf.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

@NgModule({
    imports: [
        CommonModule,
        AppCommonModule,
        FormsModule,
        ReactiveFormsModule,
        UtilsModule,
        NgSelectModule,
        NgxExtendedPdfViewerModule,
        SalesRoutingModule,
        FileUploadModule,
        AutoCompleteModule,
        PaginatorModule,
        EditorModule,
        InputMaskModule,
        TableModule,
        TooltipModule,
        NepaliDatepickerModule,
        ModalModule,
        BsDatepickerModule,
        BsDropdownModule,
        PopoverModule,
        TreeTableModule,
        DragDropModule,
        KeyboardShortcutsModule,
        ScrollingModule,
       
        SubHeaderComponent,
        AdminSharedModule
    ],
    declarations: [
        AddSalesMasterComponent,
        AddSalesReturnComponent,
        SalesMasterComponent,
        SalesReturnComponent,
        SalesPdfComponent
    ],
    exports: [
    ],
    providers: [
        AgainstModeService,
        { provide: BsDatepickerConfig, useFactory: NgxBootstrapDatePickerConfigService.getDatepickerConfig },
        { provide: BsDaterangepickerConfig, useFactory: NgxBootstrapDatePickerConfigService.getDaterangepickerConfig },
        { provide: BsLocaleService, useFactory: NgxBootstrapDatePickerConfigService.getDatepickerLocale },
    ],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA]
})

export class SalesModule {
    constructor() {
    }
}
