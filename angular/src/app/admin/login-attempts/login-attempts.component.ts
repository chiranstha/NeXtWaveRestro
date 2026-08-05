import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AbpLoginResultType, UserLoginServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { DateTime } from 'luxon';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { LoginResultTypeComboComponent } from './login-result-type.combo';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './login-attempts.component.html',
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        FormsModule,
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        LoginResultTypeComboComponent,
        BusyIfDirective,
        AgGridAngular,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class LoginAttemptsComponent extends AppComponentBase implements OnInit {
    private _dateTimeService = inject(DateTimeService);
    private _userLoginService = inject(UserLoginServiceProxy);
    public filter: string;
    public dateRange: DateTime[];
    public loginResultFilter: AbpLoginResultType;
    private gridApi!: GridApi;
    public rowData: any[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: this.l('IpAddress'),
            field: 'clientIpAddress',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('Client'),
            field: 'clientName',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('Browser'),
            field: 'browserInfo',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('Time'),
            field: 'creationTime',
            sortable: true,
            filter: true,
            valueFormatter: (params) => {
                return this._dateTimeService.formatDate(params.value, 'F');
            },
        },
        {
            headerName: this.l('Result'),
            field: 'result',
            sortable: true,
            filter: true,
            cellRenderer: (params) => {
                const result = params.value;
                const cssClass = result === 'Success' ? 'text-success' : 'text-warning';
                const text = this.l(`AbpLoginResultType_${result}`);
                return `<span class="${cssClass}">${text}</span>`;
            },
        },
    ];

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.loginResultFilter = '' as any;
        this.dateRange = [this._dateTimeService.getStartOfDay(), this._dateTimeService.getEndOfDay()];
    }
    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.getLoginAttempts();
    }
    getLoginAttempts(): void {
        this.showMainSpinner();
        this._userLoginService
            .getUserLoginAttempts(
                this.filter,
                this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(this.dateRange[1]),
                this.loginResultFilter,
                '', // No sorting for AG Grid
                1000, // Load all for client-side sorting/filtering
                0, // skipCount
            )
            .pipe(finalize(() => this.hideMainSpinner()))
            .subscribe((result) => {
                this.rowData = result.items;
                this.hideMainSpinner();
            });
    }
}
