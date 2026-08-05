import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TrialBalanceReportComponent } from './trial-balance-report/trial-balance-report.component';
import { ProfitLossReportComponent } from './profitlosss-report/profitloss-report.component';
import { BalanceSheetComponent } from './balance-sheet/balance-sheet.component';
import { CashFlowReportComponent } from './cash-flow-report/cash-flow-report.component';
@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                    {
                        path: 'trailbalance',
                        component: TrialBalanceReportComponent,
                        data: { permission: 'Pages.TrialBalanceReport' },
                    },
                    {
                        path: 'profit-loss',
                        component: ProfitLossReportComponent,
                        data: { permission: 'Pages.ProfitAndLossReport' },
                    },
                    {
                        path: 'balance-sheet',
                        component: BalanceSheetComponent,
                        data: { permission: 'Pages.PagesBalanceSheetReport' },
                    },
                    {
                        path: 'cash-flow',
                        component: CashFlowReportComponent,
                        data: { permission: 'Pages.PagesCashFlowReport' },
                    },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class FinancialStatementRoutingModule {}



