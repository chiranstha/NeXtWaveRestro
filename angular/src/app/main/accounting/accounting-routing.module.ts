import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AccountGroupsComponent } from './accountGroups/accountGroups.component';
import { AddOrEditAccountGroupComponent } from './accountGroups/accountGroups/add-or-edit-account-group/add-or-edit-account-group.component';
import { AccountLedgersComponent } from './accountLedgers/accountLedgers.component';
import { AddAccountLedgersComponent } from './accountLedgers/add-account-ledgers/add-account-ledgers.component';
import { AddFinancialyearComponent } from './financialYears/add-financialyear/add-financialyear.component';
import { FinancialYearsComponent } from './financialYears/financialYears.component';
import { MergeLedgerComponent } from './merge-ledger/merge-ledger.component';
import { MultipleLedgerComponent } from './multiple-ledger/multiple-ledger.component';
import { TaxComponent } from './taxes/taxes.component';
import { AddTaxesComponent } from './taxes/addTaxes/addTaxes.component';
import { VoucherTypesComponent } from './voucherTypes/voucherTypes.component';
import { AddVoucherTypesComponent } from './voucherTypes/addVoucherTypes/addVoucherTypes.component';
@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                    
                    {
                        path: 'financialYears',
                        component: FinancialYearsComponent,
                        data: { permission: 'Pages.FinancialYears' },
                    },
                    {
                        path: 'financialYears/add',
                        component: AddFinancialyearComponent,
                        data: { permission: 'Pages.AccountLedgers.Create' },
                    },
                    {
                        path: 'financialYears/edit/:id',
                        component: AddFinancialyearComponent,
                        data: { permission: 'Pages.FinancialYears.Edit' },
                    },
                    {
                        path: 'accountLedgers',
                        component: AccountLedgersComponent,
                        data: { permission: 'Pages.AccountLedgers' },
                    },
                    {
                        path: 'accountLedgers/addMultiple',
                        component: MultipleLedgerComponent,
                        data: { permission: 'Pages.AccountLedgers.Create' },
                    },
                    {
                        path: 'accountLedgers/addMultiple/:id',
                        component: MultipleLedgerComponent,
                        data: { permission: 'Pages.AccountLedgers.Edit' },
                    },
                    {
                        path: 'accountLedgers/add',
                        component: AddAccountLedgersComponent,
                        data: { permission: 'Pages.AccountLedgers.Create' },
                    },
                    {
                        path: 'accountLedgers/edit/:ledgerId',
                        component: AddAccountLedgersComponent,
                        data: { permission: 'Pages.AccountLedgers.Edit' },
                    },
                    {
                        path: 'accountGroups',
                        component: AccountGroupsComponent,
                        data: { permission: 'Pages.AccountGroups' },
                    },
                    {
                        path: 'accountGroups/add',
                        component: AddOrEditAccountGroupComponent,
                        data: { permission: 'Pages.AccountGroups.Create' },
                    },
                    {
                        path: 'accountGroups/edit/:id',
                        component: AddOrEditAccountGroupComponent,
                        data: { permission: 'Pages.AccountGroups.Edit' },
                    },
                    {
                        path: 'mergeLedger',
                        component: MergeLedgerComponent,
                        data: { permission: 'Pages.AccountGroups.Edit' },
                    },

                    {
                        path: 'taxes',
                        component: TaxComponent,
                        data: { permission: 'Pages.Taxes' },
                    },
                    {
                        path: 'taxes/add',
                        component: AddTaxesComponent,
                        data: { permission: 'Pages.Taxes.Create' },
                    },
                    {
                        path: 'taxes/edit/:id',
                        component: AddTaxesComponent,
                        data: { permission: 'Pages.Taxes.Edit' },
                    },

                    // voucherTypes
                    {
                        path: 'voucherTypes',
                        component: VoucherTypesComponent,
                        data: { permission: 'Pages.VoucherTypes' },
                    },
                    {
                        path: 'voucherTypes/add',
                        component: AddVoucherTypesComponent,
                        data: { permission: 'Pages.VoucherTypes.Create' },
                    },
                    {
                        path: 'voucherTypes/edit/:id',
                        component: AddVoucherTypesComponent,
                        data: { permission: 'Pages.VoucherTypes.Edit' },
                    }
                   
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class AccountingRoutingModule {}



