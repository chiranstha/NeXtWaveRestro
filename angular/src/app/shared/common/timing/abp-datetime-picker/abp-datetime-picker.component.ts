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
    BsDatepickerInputDirective,
    BsDatepickerDirective,
} from 'ngx-bootstrap/datepicker';
import { AbstractNgModelComponent } from '../../ng-model.component';
import { DateTimeService } from '../date-time.service';
import compare from 'just-compare';
import { NgxBootstrapDatePickerConfigService } from 'assets/ngx-bootstrap/ngx-bootstrap-datepicker-config.service';
@Component({
    selector: 'abp-datetime-picker',
    templateUrl: './abp-datetime-picker.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => AbpDateTimePickerComponent),
            multi: true,
        },
        { provide: DateTimeService, useClass: DateTimeService },
    ],
    imports: [BsDatepickerInputDirective, BsDatepickerDirective],
    schemas: [NO_ERRORS_SCHEMA],
})
export class AbpDateTimePickerComponent extends AbstractNgModelComponent {
    @ViewChild('datePicker', { static: true }) datePicker: ElementRef;
    @Input() bsConfig: BsDatepickerConfig = null;
    @Input() isUtc = false;
    date: DateTime | null | undefined;
    lastDate: Date = null;
    private _dateTimeService = inject(DateTimeService);
    private _renderer = inject(Renderer2);
    private _localeService = inject(BsLocaleService);
    setDate(date: Date) {
        if (!date) {
            this.writeValue(null);
        } else if (date instanceof Date && !compare(this.lastDate, date) && date.toString() !== 'Invalid Date') {
            // clear hour, minute, seconds and milliseconds since this is a date
            date = this.clearTime(date);
            if (this.isUtc) {
                // using UTC date regardless of timing configuration
                const utcDateString = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString();
                date = this._dateTimeService.fromISODateString(utcDateString).toJSDate();
            } else if (abp.clock?.provider?.supportsMultipleTimezone) {
                date = this._dateTimeService.changeTimeZone(date, abp.timing.timeZoneInfo.iana.timeZoneId);
            }
            this.writeValue(this._dateTimeService.fromJSDate(date));
        }
    }
    writeValue(value: DateTime) {
        this.date = value;
        if (!value) {
            this.cleanValue();
            return;
        }
        this.lastDate = this._dateTimeService.toJSDate(value);
        super.writeValue(value);
        this.change();
    }
    change() {
        if (!this.date) {
            return;
        }
        this.value = this.date;
        this.setInputValue(this.lastDate);
    }
    setInputValue(value?: Date): void {
        const initialDate = !value
            ? ''
            : formatDate(value, this.getConfig().dateInputFormat, this._localeService.currentLocale);
        this._renderer.setProperty(this.datePicker.nativeElement, 'value', initialDate);
    }
    cleanValue() {
        this.date = null;
        this.value = null;
        this.lastDate = null;
    }
    clearTime(date: Date): Date {
        if (!this.getConfig().withTimepicker) {
            date.setHours(0);
            date.setMinutes(0);
        }
        date.setSeconds(0);
        date.setMilliseconds(0);
        return date;
    }
    getConfig(): BsDatepickerConfig {
        if (this.bsConfig) {
            return this.bsConfig;
        }
        return NgxBootstrapDatePickerConfigService.getDatepickerConfig();
    }
}
