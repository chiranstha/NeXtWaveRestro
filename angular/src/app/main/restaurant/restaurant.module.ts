import { CommonModule } from '@angular/common';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { UtilsModule } from '@shared/utils/utils.module';
import { AgGridModule } from 'ag-grid-angular';
import { AgChartsSharedModule } from '@app/shared/common/ag-charts-shared/ag-charts-shared.module';
import { NgSelectModule } from '@ng-select/ng-select';
import { KeyboardShortcutsModule } from 'ng-keyboard-shortcuts';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { ModalModule } from 'ngx-bootstrap/modal';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { NepaliDatepickerModule } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { RestaurantChannelsComponent } from './restaurant-channels/restaurant-channels.component';
import { RestaurantKdsComponent } from './restaurant-kds/restaurant-kds.component';
import { RestaurantInventoryComponent } from './restaurant-inventory/restaurant-inventory.component';
import { RestaurantMenuComponent } from './restaurant-menu/restaurant-menu.component';
import { RestaurantNavigationComponent } from './restaurant-navigation.component';
import { RestaurantPayrollApiService } from './restaurant-payroll/restaurant-payroll-api.service';
import { RestaurantCashShiftApiService } from './restaurant-cash-shift-api.service';
import { RestaurantReleaseApiService } from './restaurant-release-api.service';
import { RestaurantPayrollComponent } from './restaurant-payroll/restaurant-payroll.component';
import { RestaurantPosComponent } from './restaurant-pos/restaurant-pos.component';
import { RestaurantReportsComponent } from './restaurant-reports/restaurant-reports.component';
import { RestaurantRoutingModule } from './restaurant-routing.module';
import { RestaurantSetupComponent } from './restaurant-setup/restaurant-setup.component';
import { RestaurantRefundsComponent } from './restaurant-refunds/restaurant-refunds.component';
import { RestaurantStylesComponent } from './restaurant-styles.component';
import { RestaurantNepaliDatePipe } from './restaurant-nepali-date.pipe';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        AppCommonModule,
        UtilsModule,
        AdminSharedModule,
        SubHeaderComponent,
        NgSelectModule,
        KeyboardShortcutsModule,
        ModalModule,
        BsDropdownModule,
        TooltipModule,
        NepaliDatepickerModule,
        AgGridModule,
        AgChartsSharedModule,
        RestaurantStylesComponent,
        RestaurantNepaliDatePipe,
        RestaurantRoutingModule,
    ],
    declarations: [
        RestaurantSetupComponent,
        RestaurantRefundsComponent,
        RestaurantMenuComponent,
        RestaurantNavigationComponent,
        RestaurantInventoryComponent,
        RestaurantChannelsComponent,
        RestaurantPosComponent,
        RestaurantKdsComponent,
        RestaurantReportsComponent,
        RestaurantPayrollComponent,
    ],
    providers: [RestaurantPayrollApiService, RestaurantCashShiftApiService, RestaurantReleaseApiService],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA],
})
export class RestaurantModule {}
