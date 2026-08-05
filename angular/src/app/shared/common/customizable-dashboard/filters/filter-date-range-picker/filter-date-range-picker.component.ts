import { Component, Injector, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateTime } from 'luxon';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';
@Component({
    selector: 'app-filter-date-range-picker',
    templateUrl: './filter-date-range-picker.component.html',
    styleUrls: ['./filter-date-range-picker.component.css'],
    imports: [
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        LuxonFormatPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class FilterDateRangePickerComponent extends AppComponentBase {
    private _dateTimeService = inject(DateTimeService);
    date: Date;
    selectedDateRange: DateTime[];
    constructor() {
        const injector = inject(Injector);
        super();
        this.selectedDateRange = [this._dateTimeService.getStartOfDay(), this._dateTimeService.getEndOfDay()];
    }
    onChange() {
        abp.event.trigger('app.dashboardFilters.dateRangePicker.onDateChange', this.selectedDateRange);
    }
}
