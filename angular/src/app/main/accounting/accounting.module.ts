import { CommonModule } from '@angular/common';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { UtilsModule } from '@shared/utils/utils.module';
import { ModalModule } from 'ngx-bootstrap/modal';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { AutoCompleteModule } from '@shared/ui-compat';
import { EditorModule } from '@shared/ui-compat';
import { FileUploadModule } from '@shared/ui-compat';
import { InputMaskModule } from '@shared/ui-compat';
import { PaginatorModule } from '@shared/ui-compat';
import { TableModule } from '@shared/ui-compat';
import { AccountGroupsComponent } from './accountGroups/accountGroups.component';
import { AddOrEditAccountGroupComponent } from './accountGroups/accountGroups/add-or-edit-account-group/add-or-edit-account-group.component';
import { ViewAccountGroupModalComponent } from './accountGroups/view-accountGroup-modal.component';
import { AccountingRoutingModule } from './accounting-routing.module';
import { AccountLedgersComponent } from './accountLedgers/accountLedgers.component';
import { AddAccountLedgersComponent } from './accountLedgers/add-account-ledgers/add-account-ledgers.component';
import { ViewAccountLedgerModalComponent } from './accountLedgers/view-accountLedger-modal.component';
import { AddFinancialyearComponent } from './financialYears/add-financialyear/add-financialyear.component';
import { FinancialYearsComponent } from './financialYears/financialYears.component';
import { ViewFinancialYearModalComponent } from './financialYears/view-financialYear-modal.component';
import { MultipleLedgerComponent } from './multiple-ledger/multiple-ledger.component';
import { MergeLedgerComponent } from './merge-ledger/merge-ledger.component';
import { NgSelectModule } from '@ng-select/ng-select';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { TaxComponent } from './taxes/taxes.component';
import { AddTaxesComponent } from './taxes/addTaxes/addTaxes.component';
import { AddVoucherTypesComponent } from './voucherTypes/addVoucherTypes/addVoucherTypes.component';
import { VoucherTypesComponent } from './voucherTypes/voucherTypes.component';
@NgModule({
    imports: [
        FormsModule,
        ReactiveFormsModule,
        NepaliDatepickerModule,
        AccountingRoutingModule,
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
        NgSelectModule,
        AdminSharedModule,
        SubHeaderComponent,
        AccountLedgersComponent,
        ViewAccountLedgerModalComponent,
        AccountGroupsComponent,
        ViewAccountGroupModalComponent,
        AddOrEditAccountGroupComponent,
        MergeLedgerComponent,
        AddAccountLedgersComponent,
        AddFinancialyearComponent,
        MultipleLedgerComponent,
        FinancialYearsComponent,
        ViewFinancialYearModalComponent,
    ],
    declarations: [TaxComponent, AddTaxesComponent, VoucherTypesComponent, AddVoucherTypesComponent],
    exports: [AddAccountLedgersComponent],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA],
})
export class AccountingModule {
    constructor() {}
}



