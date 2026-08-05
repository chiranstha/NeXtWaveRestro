import { Component, ElementRef, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy, SendAndGetDateWithTextInput } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { FormsModule } from '@angular/forms';
import { AbpDateTimePickerComponent } from '../../shared/common/timing/abp-datetime-picker/abp-datetime-picker.component';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { AbpDateRangePickerComponent } from '../../shared/common/timing/abp-date-range-picker/abp-date-range-picker.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'demo-ui-date-time',
    templateUrl: './demo-ui-date-time.component.html',
    animations: [appModuleAnimation],
    imports: [
        FormsModule,
        AbpDateTimePickerComponent,
        ValidationMessagesComponent,
        AbpDateRangePickerComponent,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DemoUiDateTimeComponent extends AppComponentBase implements OnInit {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    @ViewChild('SampleDatePicker', { static: true }) sampleDatePicker: ElementRef;
    @ViewChild('SampleDateTimePicker', { static: true }) sampleDateTimePicker: ElementRef;
    sampleDate: DateTime;
    sampleDateTime: DateTime;
    sampleDateRange: DateTime[];
    sampleDateWithTextInput: SendAndGetDateWithTextInput = new SendAndGetDateWithTextInput();
    dateFormat = 'yyyy-LL-dd HH:mm:ss';

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.sampleDate = this._dateTimeService.getStartOfDay();
        this.sampleDateTime = this._dateTimeService.getStartOfDay();
        this.sampleDateWithTextInput.text = 'This is a sample text';
        this.sampleDateWithTextInput.date = this._dateTimeService.getStartOfDay();
        this.sampleDateRange = [this._dateTimeService.getStartOfDayMinusDays(7), this._dateTimeService.getStartOfDay()];
    }
    // default date picker - post
    submitDate(): void {
        this.demoUiComponentsService.sendAndGetDate(this.sampleDate).subscribe((data) => {
            const dateString = this.getDateString(data.date);
            this.message.info(dateString, this.l('PostedValue'));
        });
    }
    // default date time picker - post
    submitDateTime(): void {
        this.demoUiComponentsService.sendAndGetDate(this.sampleDateTime).subscribe((data) => {
            const dateString = this.getDateString(data.date);
            this.message.info(dateString, this.l('PostedValue'));
        });
    }
    // default date range picker - post
    submitDateRange(): void {
        this.demoUiComponentsService
            .sendAndGetDateRange(this.sampleDateRange[0], this.sampleDateRange[1])
            .subscribe((data) => {
                const startDateString = this.getDateString(data.startDate);
                const endDateString = this.getDateString(data.endDate);
                this.message.info(`${startDateString}-${endDateString}`, this.l('PostedValue'));
            });
    }
    // default date picker with text - post
    submitDateWithText(): void {
        this.demoUiComponentsService.sendAndGetDateWithText(this.sampleDateWithTextInput).subscribe((data) => {
            const dateString = this.getDateString(data.date);
            this.message.info(`${dateString}-${data.text}`, this.l('PostedValue'));
        });
    }
    getDateString(date: DateTime): string {
        let dateString = this._dateTimeService.formatDate(date, this.dateFormat);
        if (abp.clock?.provider?.supportsMultipleTimezone) {
            dateString += `(${abp.timing.timeZoneInfo.iana.timeZoneId})`;
        }
        return dateString;
    }
}
