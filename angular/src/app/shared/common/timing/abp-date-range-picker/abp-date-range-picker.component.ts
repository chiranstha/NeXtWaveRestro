import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    forwardRef,
    inject,
    Input,
    Renderer2,
    ViewChild,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NG_VALUE_ACCESSOR } from '@angular/forms';
import { DateTime } from 'luxon';
import { formatDate } from 'ngx-bootstrap/chronos';
import {
    BsDatepickerConfig,
    BsLocaleService,
    BsDaterangepickerInputDirective,
    BsDaterangepickerDirective,
} from 'ngx-bootstrap/datepicker';
import { AbstractNgModelComponent } from '../../ng-model.component';
import { DateTimeService } from '../date-time.service';
import compare from 'just-compare';
import { NgxBootstrapDatePickerConfigService } from 'assets/ngx-bootstrap/ngx-bootstrap-datepicker-config.service';
@Component({
    selector: 'abp-date-range-picker',
    templateUrl: './abp-date-range-picker.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => AbpDateRangePickerComponent),
            multi: true,
        },
        { provide: DateTimeService, useClass: DateTimeService },
    ],
    imports: [BsDaterangepickerInputDirective, BsDaterangepickerDirective],
    schemas: [NO_ERRORS_SCHEMA],
})
export class AbpDateRangePickerComponent extends AbstractNgModelComponent {
    @ViewChild('datePicker') datePicker: ElementRef;
    @Input() bsConfig: BsDatepickerConfig = null;
    startDate: DateTime | null | undefined;
    endDate: DateTime | null | undefined;
    lastDates: Date[] = null;
    private _dateTimeService = inject(DateTimeService);
    private _renderer = inject(Renderer2);
    private _localeService = inject(BsLocaleService);
    setDate(dates: Date[]) {
        if (!dates) {
            this.writeValue(null);
        } else if (
            dates &&
            dates[0] instanceof Date &&
            dates[1] instanceof Date &&
            !compare(this.lastDates, dates) &&
            dates[0].toString() !== 'Invalid Date' &&
            dates[1].toString() !== 'Invalid Date'
        ) {
            // clear time info of given dates because DateRangePicker doesn't support selecting time
            dates[0] = this.clearTime(dates[0]);
            dates[1] = this.clearTime(dates[1]);
            this.lastDates = dates;
            if (abp.clock?.provider?.supportsMultipleTimezone) {
                this.lastDates = [
                    this._dateTimeService.changeTimeZone(dates[0], abp.timing.timeZoneInfo.iana.timeZoneId),
                    this._dateTimeService.changeTimeZone(dates[1], abp.timing.timeZoneInfo.iana.timeZoneId),
                ];
            }
            const startDate = this._dateTimeService.fromJSDate(this.lastDates[0]);
            const endDate = this._dateTimeService.fromJSDate(this.lastDates[1]);
            this.writeValue([startDate, endDate]);
        }
    }
    writeValue(values: DateTime[]) {
        if (!values) {
            this.cleanValue();
            return;
        }
        this.startDate = values[0];
        this.endDate = values[1];
        this.lastDates = [this._dateTimeService.toJSDate(this.startDate), this._dateTimeService.toJSDate(this.endDate)];
        super.writeValue(values);
        this.change();
    }
    change() {
        if (!this.startDate && !this.endDate) {
            return;
        }
        this.value = [this.startDate, this.endDate];
        this.setInputValue(this.lastDates);
    }
    setInputValue(date?: Date[]): void {
        let range = '';
        if (date) {
            const start: string = !date[0]
                ? ''
                : formatDate(date[0], this.getConfig().rangeInputFormat, this._localeService.currentLocale);
            const end: string = !date[1]
                ? ''
                : formatDate(date[1], this.getConfig().rangeInputFormat, this._localeService.currentLocale);
            range = start && end ? start + this.getConfig().rangeSeparator + end : '';
        }
        this._renderer.setProperty(this.datePicker.nativeElement, 'value', range);
    }
    cleanValue() {
        this.startDate = null;
        this.endDate = null;
        this.value = null;
        this.lastDates = null;
    }
    getConfig(): BsDatepickerConfig {
        if (this.bsConfig) {
            return this.bsConfig;
        }
        return NgxBootstrapDatePickerConfigService.getDatepickerConfig();
    }
    clearTime(date: Date): Date {
        date.setHours(0);
        date.setMinutes(0);
        date.setSeconds(0);
        date.setMilliseconds(0);
        return date;
    }
}
