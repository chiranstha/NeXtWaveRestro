import { CommonModule } from '@angular/common';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { UtilsModule } from '@shared/utils/utils.module';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';
import { NgSelectModule } from '@ng-select/ng-select';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { FinancialStatementRoutingModule } from './financialStatement-routing.module';
import { BalanceSheetCellRendererComponent } from './balance-sheet/balanceSheetCellrender.component';
import { ProfitLossReportComponent } from './profitlosss-report/profitloss-report.component';
import { TrialBalanceReportComponent } from './trial-balance-report/trial-balance-report.component';
import { BalanceSheetComponent } from './balance-sheet/balance-sheet.component';
import { CashFlowReportComponent } from './cash-flow-report/cash-flow-report.component';
import { AgCharts } from 'ag-charts-angular';
import { AgChartsSharedModule } from '@app/shared/common/ag-charts-shared/ag-charts-shared.module';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { AgGridFeatureModule } from '@app/shared/common/ag-grid/ag-grid-feature.module';
@NgModule({
    imports: [
        NgSelectModule,
        NgxExtendedPdfViewerModule,
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        UtilsModule,
        AppCommonModule,
        NgSelectModule,
        AgChartsSharedModule,
        AgGridFeatureModule,
        FormsModule,
        AdminSharedModule,
        NepaliDatepickerModule,
        AgCharts,
        FinancialStatementRoutingModule,
        BalanceSheetCellRendererComponent,
        BalanceSheetComponent,
        CashFlowReportComponent,
        ProfitLossReportComponent,
        TrialBalanceReportComponent,
    ],
    exports: [],
    providers: [],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA],
})
export class FinancialStatementModule {}



