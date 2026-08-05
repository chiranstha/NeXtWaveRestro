import {
    AfterViewInit,
    Component,
    ElementRef,
    EventEmitter,
    forwardRef,
    HostListener,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges,
    ViewChild,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ConfigType, DateObj, DaysMapping, MonthMapping } from './interface/interface';
import { CalendarFormat, CalendarType, daysMapping, englishMonthMapping, monthsMapping } from './constants/mapping';
import { NepaliDatepickerPrivateService } from './services/nepali-datepicker-angular-private.service';
import { DatePipe, NgClass, NgIf, NgTemplateOutlet, NgFor } from '@angular/common';
import { englishLeapMonths, englishMonths } from './constants/data';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { ToNpPipe } from './pipes/to-np.pipe';
type DateFormatType = 'yyyy/mm/dd' | 'dd/mm/yyyy' | 'yyyy-mm-dd' | 'dd-mm-yyyy';
type Language = 'en' | 'ne';
type MonthDisplayType = 'default' | 'short';
type DateIn = 'AD' | 'BS';
@Component({
    selector: 'ne-datepicker',
    templateUrl: 'nepali-datepicker-angular.component.html',
    styleUrls: ['nepali-datepicker-angular.component.scss'],
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => NepaliDatepickerComponent),
            multi: true,
        },
    ],
    imports: [NgClass, FormsModule, NgIf, NgTemplateOutlet, NgFor, ToNpPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class NepaliDatepickerComponent implements OnInit, OnChanges, AfterViewInit, ControlValueAccessor {
    _nepaliDate = inject(NepaliDatepickerPrivateService);
    private eRef = inject(ElementRef);
    private _datePipe = inject(DatePipe);
    @ViewChild('nepaliDatePicker') nepaliDatePicker!: ElementRef<HTMLDivElement>;
    @Input()
    primaryColor!: string;
    @Input()
    placeholder = 'Enter date';
    @Input()
    language: Language = 'ne';
    @Input() dateIn: DateIn = 'BS';
    @Input() valueIn: DateIn = 'BS';
    @Input() isError = false;
    @Input() darkTheme = false;
    @Input() date!: string;
    @Input() appendTime = false;
    @Input() maxDate!: Date;
    @Input() minDate!: Date;
    @Input() dateFormat: DateFormatType = 'yyyy/mm/dd';
    @Input() monthDisplayType: MonthDisplayType = 'default';
    @Input() hasMultipleCalendarView = true;
    @Input() dateReadonly = false;
    @Input() todayBtn = true;
    @Output() dateInAD: EventEmitter<string> = new EventEmitter();
    @Output() dateInBS: EventEmitter<string> = new EventEmitter();
    public nepaliDateToday: DateObj = { day: 0, month: 0, year: 0 };
    public englishDateToday: DateObj = { day: 0, month: 0, year: 0 };
    public currentNepaliDate: DateObj = { day: 0, month: 0, year: 0 };
    public englishCurrentDate: DateObj = { day: 0, month: 0, year: 0 };
    public nepaliMaxDate: DateObj = { day: 0, month: 0, year: 0 };
    public nepaliMinDate: DateObj = { day: 0, month: 0, year: 0 };
    public englishMaxDate: DateObj = { day: 0, month: 0, year: 0 };
    public englishMinDate: DateObj = { day: 0, month: 0, year: 0 };
    public selectedDate!: DateObj;
    public englishSelectedDate!: DateObj;
    public formattedDate = '';
    public years: number[] = [];
    public currentMonthData!: any;
    public daysMapping: DaysMapping = daysMapping;
    public monthsMapping: MonthMapping = monthsMapping;
    public isOpen = false;
    public calendarType = CalendarFormat.ne; // This holds language-specific format/labels
    public dayDisplayType: 'default' | 'short' = 'short';
    public calendarView: CalendarType = CalendarType.BS; // Changed type to CalendarType and initialized
    public CalendarType = CalendarType; // Expose CalendarType enum to the template
    public selectedMonthIndex: number;
    public selectedYear: number;
    initialized: boolean = false;
    private alwaysVisible = false;
    private selectedTimeWithTimezone: any;
    private dateSeparatedCharacters = ['/', '-'];
    private currentDate: any;
    private rootPrimaryColor = '#009ef7';
    constructor() {
        const { _nepaliDate } = this;
        const config = inject<ConfigType>('config' as any, { optional: true });
        if (config && config.primaryColor) {
            this.rootPrimaryColor = config.primaryColor;
        }
        this.currentDate = new Date();
        this.setEnglishCurrentDate();
        this.setNepaliCurrentDate();
        this.nepaliDateToday = this.toInternalNepaliDate(
            _nepaliDate.engToNepDate(
                this.currentDate.getDate(),
                this.currentDate.getMonth(),
                this.currentDate.getFullYear(),
            ),
        );
        this.englishDateToday = {
            day: this.currentDate.getDate(),
            month: this.currentDate.getMonth(),
            year: this.currentDate.getFullYear(),
        };
        this.selectedMonthIndex = this.nepaliDateToday.month;
        this.selectedYear = this.nepaliDateToday.year;
    }
    private toInternalNepaliDate(date: DateObj): DateObj {
        return {
            day: date.day,
            month: date.month - 1,
            year: date.year,
        };
    }
    private getControlValue(): string {
        const modelDate = this.valueIn === 'AD' ? this.englishSelectedDate : this.selectedDate;
        const formattedValue = this.dateFormatter(modelDate);
        if (!formattedValue) {
            return '';
        }
        return this.setDateWithTime(formattedValue);
    }
    @HostListener('document:click', ['$event'])
    clickout(event: any) {
        if (!this.eRef.nativeElement.contains(event.target)) {
            this.close();
        }
    }
    propagateChange = (_: any) => {};
    propagateTouch = (_: any) => {};
    writeValue(value: any) {
        if (value) {
            try {
                let minDateArray = value.split('/');
                if (minDateArray.length !== 3) {
                    // Try other separators
                    minDateArray = value.split('-');
                    if (minDateArray.length !== 3) {
                        throw new Error('Invalid date format');
                    }
                }
                const data: DateObj = {
                    day: parseInt(minDateArray[2]),
                    month: parseInt(minDateArray[1]) - 1,
                    year: parseInt(minDateArray[0]),
                };
                const incomingDateType = this.valueIn === 'AD' ? 'AD' : this.dateIn;
                if (incomingDateType === 'AD') {
                    this.englishSelectedDate = data;
                    this.currentDate = new Date(data.year, data.month, data.day);
                    try {
                        this.selectedDate = this.toInternalNepaliDate(
                            this._nepaliDate.engToNepDate(data.day, data.month, data.year),
                        );
                        this.currentNepaliDate = { ...this.selectedDate };
                    } catch (error) {
                        console.error('Error converting English to Nepali date:', error);
                        this.setToCurrentDate(false);
                        return;
                    }
                } else {
                    this.selectedDate = data;
                    this.currentNepaliDate = data;
                    try {
                        this.currentDate = this._nepaliDate.nepToEngDate(data.day, data.month, data.year);
                        this.englishSelectedDate = {
                            day: this.currentDate.getDate(),
                            month: this.currentDate.getMonth(),
                            year: this.currentDate.getFullYear(),
                        };
                    } catch (error) {
                        console.error('Error converting Nepali to English date:', error);
                        this.setToCurrentDate(false);
                        return;
                    }
                }
                if (this.calendarView === CalendarType.BS) {
                    this.currentNepaliDate = { ...this.selectedDate };
                    this.selectedMonthIndex = this.selectedDate.month;
                    this.selectedYear = this.selectedDate.year;
                } else {
                    this.englishCurrentDate = { ...this.englishSelectedDate };
                    this.selectedMonthIndex = this.englishSelectedDate.month;
                    this.selectedYear = this.englishSelectedDate.year;
                }
                this.formatValue();
            } catch (error) {
                console.error('Error processing date:', error);
                // Set to current date if there's an error
                this.setToCurrentDate(false);
            }
        } else {
            this.setToCurrentDate(false);
        }
    }
    registerOnTouched(fn: any) {
        this.propagateTouch = fn;
    }
    registerOnChange(fn: any) {
        this.propagateChange = fn;
    }
    ngOnInit() {
        this.setCurrentDate();
        this.populateYears();
        this.setCurrentMonthData();
    }
    ngOnChanges(changes: SimpleChanges) {
        if (changes['date'] && this.date) {
            this.setInputDate();
        }
        if (changes['maxDate'] && this.maxDate) {
            this.setMaxDate();
        }
        if (changes['minDate'] && this.minDate) {
            this.setMinDate();
        }
        if (changes['language'] && this.language) {
            this.calendarType = CalendarFormat[this.language];
        }
        if (changes['primaryColor'] && this.primaryColor) {
            this.rootPrimaryColor = this.primaryColor;
            this.setDatepickerColor();
        }
    }
    ngAfterViewInit(): void {
        this.setDatepickerColor();
    }
    public setNepaliMinDate() {
        const minimumDate = new Date(this.minDate);
        this.nepaliMinDate = this.toInternalNepaliDate(
            this._nepaliDate.engToNepDate(minimumDate.getDate(), minimumDate.getMonth(), minimumDate.getFullYear()),
        );
    }
    public selectYear(e: any) {
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.currentNepaliDate.year = parseInt(e.target.value);
            this.currentDate = this._nepaliDate.nepToEngDate(
                this.currentNepaliDate.day,
                this.currentNepaliDate.month,
                this.currentNepaliDate.year,
            );
        } else {
            // CalendarType.AD
            this.englishCurrentDate.year = parseInt(e.target.value);
            this.currentDate = new Date(`${this.englishCurrentDate.year}/${this.englishCurrentDate.month + 1}/1`);
        }
        this.setEnglishCurrentDate();
        this.setCurrentMonthData();
    }
    public selectMonth(e: any) {
        const month = e.target.value;
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.currentNepaliDate.day = 1;
            const nep_month_index = this.monthsMapping[this.language][this.monthDisplayType]?.indexOf(month) ?? 0;
            this.currentNepaliDate.month = nep_month_index;
            this.currentDate = this._nepaliDate.nepToEngDate(
                this.currentNepaliDate.day,
                this.currentNepaliDate.month,
                this.currentNepaliDate.year,
            );
        } else {
            // CalendarType.AD
            this.englishCurrentDate.day = 1;
            const monthIndex = englishMonthMapping[this.language][this.monthDisplayType]?.indexOf(month) ?? 0;
            this.englishCurrentDate.month = monthIndex;
            this.currentDate = new Date(`${this.englishCurrentDate.year}/${this.englishCurrentDate.month + 1}/1`);
        }
        this.setEnglishCurrentDate();
        this.setCurrentMonthData();
    }
    public selectDate(day: number) {
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison (was ==)
            this.selectedDate = { ...this.currentNepaliDate, day };
            try {
                const en = this._nepaliDate.nepToEngDate(
                    this.selectedDate.day,
                    this.selectedDate.month,
                    this.selectedDate.year,
                );
                this.englishSelectedDate = {
                    day: en.getDate(),
                    month: en.getMonth(),
                    year: en.getFullYear(),
                };
            } catch (error) {
                console.error('Error converting to English date:', error);
                return;
            }
            this.selectedMonthIndex = this.currentNepaliDate.month;
            this.selectedYear = this.currentNepaliDate.year;
        } else {
            // CalendarType.AD
            this.englishSelectedDate = { ...this.englishCurrentDate, day };
            try {
                this.selectedDate = this.toInternalNepaliDate(
                    this._nepaliDate.engToNepDate(
                        this.englishSelectedDate.day,
                        this.englishSelectedDate.month,
                        this.englishSelectedDate.year,
                    ),
                );
            } catch (error) {
                console.error('Error converting to Nepali date:', error);
                return;
            }
            this.selectedMonthIndex = this.currentDate.getMonth();
            this.selectedYear = this.currentDate.getFullYear();
        }
        this.formatValue();
        this.emitDateInAD();
        this.emitDateInBS();
        this.propagateChange(this.getControlValue());
        this.close();
    }
    public prevMonth() {
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.currentNepaliDate.day = 1;
            if (this.currentNepaliDate.month <= 0) {
                if (this.currentNepaliDate.year > 2001) {
                    this.currentNepaliDate.month = 11;
                    this.currentNepaliDate.year--;
                }
            } else {
                this.currentNepaliDate.month--;
            }
            try {
                this.currentDate = this._nepaliDate.nepToEngDate(
                    this.currentNepaliDate.day,
                    this.currentNepaliDate.month,
                    this.currentNepaliDate.year,
                );
                this.setEnglishCurrentDate();
            } catch (error) {
                console.error('Error converting to English date:', error);
                return;
            }
        } else {
            // CalendarType.AD
            this.englishCurrentDate.day = 1;
            if (this.englishCurrentDate.month <= 0) {
                if (this.englishCurrentDate.year > 1944) {
                    this.englishCurrentDate.month = 11;
                    this.englishCurrentDate.year--;
                }
            } else {
                this.englishCurrentDate.month--;
            }
            const newDate = {
                day: this.englishCurrentDate.day,
                month: this.englishCurrentDate.month,
                year: this.englishCurrentDate.year,
            };
            this.currentDate = new Date(`${newDate.year}/${newDate.month + 1}/${newDate.day}`);
            this.setNepaliCurrentDate();
        }
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.selectedMonthIndex = this.currentNepaliDate.month;
            this.selectedYear = this.currentNepaliDate.year;
        } else {
            // CalendarType.AD
            this.selectedMonthIndex = this.currentDate.getMonth();
            this.selectedYear = this.currentDate.getFullYear();
        }
        this.setCurrentMonthData();
    }
    public nextMonth() {
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.currentNepaliDate.day = 1;
            if (this.currentNepaliDate.month >= 11) {
                if (this.currentNepaliDate.year < 2099) {
                    this.currentNepaliDate.month = 0;
                    this.currentNepaliDate.year++;
                }
            } else {
                this.currentNepaliDate.month++;
            }
            try {
                this.currentDate = this._nepaliDate.nepToEngDate(
                    this.currentNepaliDate.day,
                    this.currentNepaliDate.month,
                    this.currentNepaliDate.year,
                );
                this.setEnglishCurrentDate();
            } catch (error) {
                console.error('Error converting to English date:', error);
                return;
            }
        } else {
            // CalendarType.AD
            this.englishCurrentDate.day = 1;
            if (this.englishCurrentDate.month >= 11) {
                if (this.englishCurrentDate.year < 2044) {
                    this.englishCurrentDate.month = 0;
                    this.englishCurrentDate.year++;
                }
            } else {
                this.englishCurrentDate.month++;
            }
            const newDate = {
                day: this.englishCurrentDate.day,
                month: this.englishCurrentDate.month,
                year: this.englishCurrentDate.year,
            };
            this.currentDate = new Date(`${newDate.year}/${newDate.month + 1}/${newDate.day}`);
            this.setNepaliCurrentDate();
        }
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.selectedMonthIndex = this.currentNepaliDate.month;
            this.selectedYear = this.currentNepaliDate.year;
        } else {
            // CalendarType.AD
            this.selectedMonthIndex = this.currentDate.getMonth();
            this.selectedYear = this.currentDate.getFullYear();
        }
        this.setCurrentMonthData();
    }
    public toggleOpen() {
        if (this.dateReadonly) {
            return;
        }
        if (!this.alwaysVisible) {
            this.isOpen = !this.isOpen;
        }
    }
    public open() {
        if (this.dateReadonly) {
            return;
        }
        this.isOpen = true;
        // Always show the month of the selected date when opening the calendar
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            if (this.selectedDate) {
                // Use the selected date's month and year
                this.currentNepaliDate = {
                    day: 1, // First day of the month for display purposes
                    month: this.selectedDate.month,
                    year: this.selectedDate.year,
                };
                this.selectedMonthIndex = this.selectedDate.month;
                this.selectedYear = this.selectedDate.year;
                // Update the English equivalent
                try {
                    this.currentDate = this._nepaliDate.nepToEngDate(
                        1, // First day of the month
                        this.selectedDate.month,
                        this.selectedDate.year,
                    );
                    this.setEnglishCurrentDate();
                } catch (error) {
                    console.error('Error converting date in open():', error);
                }
            } else {
                // If no date is selected, use today's date
                this.setCurrentDate();
                this.currentNepaliDate.day = 1;
                this.selectedMonthIndex = this.currentNepaliDate.month;
                this.selectedYear = this.currentNepaliDate.year;
            }
        } else {
            // CalendarType.AD
            // English calendar view
            if (this.englishSelectedDate) {
                // Use the selected date's month and year
                this.englishCurrentDate = {
                    day: 1, // First day of the month for display purposes
                    month: this.englishSelectedDate.month,
                    year: this.englishSelectedDate.year,
                };
                this.selectedMonthIndex = this.englishSelectedDate.month;
                this.selectedYear = this.englishSelectedDate.year;
                // Update the current date and Nepali equivalent
                this.currentDate = new Date(
                    this.englishSelectedDate.year,
                    this.englishSelectedDate.month,
                    1, // First day of the month
                );
                this.setNepaliCurrentDate();
            } else {
                // If no date is selected, use today's date
                this.setCurrentDate();
                this.englishCurrentDate.day = 1;
                this.selectedMonthIndex = this.currentDate.getMonth();
                this.selectedYear = this.currentDate.getFullYear();
            }
        }
        // Update the calendar view
        this.setCurrentMonthData();
    }
    public close() {
        this.isOpen = false;
    }
    public selectCalendarView(data: { target: { value: string } }): void {
        // Changed input value type to string
        const newViewString = data.target.value;
        // Convert string to CalendarType enum
        const newView: CalendarType = newViewString === 'BS' ? CalendarType.BS : CalendarType.AD;
        const oldView: CalendarType = this.calendarView; // oldView is now CalendarType
        if (newView === oldView) {
            return;
        }
        this.calendarView = newView; // Assign enum
        this.populateYears();
        // Ensure monthsMapping is updated based on the new CalendarType view
        this.monthsMapping = this.calendarView === CalendarType.BS ? monthsMapping : englishMonthMapping; // Changed comparison
        try {
            if (newView === CalendarType.AD) {
                // Changed comparison
                if (this.selectedDate) {
                    // FIXED: Don't subtract 1 from month when converting from BS to AD
                    const convertedDate: Date = this._nepaliDate.nepToEngDate(
                        this.selectedDate.day,
                        this.selectedDate.month, // REMOVED the -1 here
                        this.selectedDate.year,
                    );
                    // Create English date object correctly
                    this.englishSelectedDate = {
                        day: convertedDate.getDate(),
                        month: convertedDate.getMonth(),
                        year: convertedDate.getFullYear(),
                    };
                    // Set current display month/year
                    this.englishCurrentDate = {
                        day: 1,
                        month: convertedDate.getMonth(),
                        year: convertedDate.getFullYear(),
                    };
                    this.selectedMonthIndex = convertedDate.getMonth();
                    this.selectedYear = convertedDate.getFullYear();
                } else {
                    // Default to today if no date selected
                    this.englishSelectedDate = { ...this.englishDateToday };
                    this.englishCurrentDate = {
                        day: 1,
                        month: this.englishDateToday.month,
                        year: this.englishDateToday.year,
                    };
                    this.selectedMonthIndex = this.englishDateToday.month;
                    this.selectedYear = this.englishDateToday.year;
                }
            } else {
                // newView === CalendarType.BS
                if (this.englishSelectedDate) {
                    // FIXED: Remove debug alert and ensure month handling is consistent
                    const convertedDate: DateObj = this.toInternalNepaliDate(
                        this._nepaliDate.engToNepDate(
                            this.englishSelectedDate.day,
                            this.englishSelectedDate.month,
                            this.englishSelectedDate.year,
                        ),
                    );
                    this.selectedDate = convertedDate;
                    this.currentNepaliDate = {
                        day: 1,
                        month: convertedDate.month,
                        year: convertedDate.year,
                    };
                    this.selectedMonthIndex = convertedDate.month;
                    this.selectedYear = convertedDate.year;
                } else {
                    // Default to today if no date selected
                    // Assuming this.nepaliDateToday.month is already 0-indexed
                    this.selectedDate = { ...this.nepaliDateToday };
                    this.currentNepaliDate = {
                        day: 1,
                        month: this.nepaliDateToday.month,
                        year: this.nepaliDateToday.year,
                    };
                    this.selectedMonthIndex = this.nepaliDateToday.month;
                    this.selectedYear = this.nepaliDateToday.year;
                }
            }
            // Format the date for display
            this.formatValue();
            // Update the calendar
            this.setCurrentMonthData();
            // Emit updated dates
            this.emitDateInAD();
            this.emitDateInBS();
        } catch (error) {
            console.error('Error during calendar view change:', error);
            // Fall back handling can be implemented here if needed
        }
    }
    // New method to validate manually entered date
    public validateManualInput() {
        if (!this.formattedDate) {
            return;
        }
        try {
            let separator = '/';
            if (this.formattedDate.includes('-')) {
                separator = '-';
            }
            const parts = this.formattedDate.split(separator);
            if (parts.length !== 3) {
                throw new Error('Invalid date format');
            }
            let year, month, day;
            if (this.dateFormat.startsWith('yyyy')) {
                year = parseInt(parts[0]);
                month = parseInt(parts[1]);
                day = parseInt(parts[2]);
            } else {
                day = parseInt(parts[0]);
                month = parseInt(parts[1]);
                year = parseInt(parts[2]);
            }
            if (isNaN(year) || isNaN(month) || isNaN(day)) {
                throw new Error('Invalid date components');
            }
            if (this.calendarView === CalendarType.BS) {
                // Changed comparison
                // Validate BS date ranges
                if (year < 2000 || year > 2099 || month < 1 || month > 12 || day < 1) {
                    throw new Error('Date out of range');
                }
                // Check if day exceeds maximum days in the month and adjust if necessary
                const maxDaysInMonth = this._nepaliDate.nepaliMonths[year - 2000][month - 1];
                if (day > maxDaysInMonth) {
                    console.log(`Adjusting day from ${day} to ${maxDaysInMonth} (max days in month)`);
                    day = maxDaysInMonth;
                }
                this.selectedDate = {
                    year,
                    month: month - 1,
                    day,
                };
                // Validate against max/min dates if set
                if (this.maxDate && this.nepaliMaxDate) {
                    const maxValue =
                        this.nepaliMaxDate.year * 365 + this.nepaliMaxDate.month * 30 + this.nepaliMaxDate.day;
                    const currentValue =
                        this.selectedDate.year * 365 + this.selectedDate.month * 30 + this.selectedDate.day;
                    if (currentValue > maxValue) {
                        throw new Error('Date exceeds maximum allowed date');
                    }
                }
                if (this.minDate && this.nepaliMinDate) {
                    const minValue =
                        this.nepaliMinDate.year * 365 + this.nepaliMinDate.month * 30 + this.nepaliMinDate.day;
                    const currentValue =
                        this.selectedDate.year * 365 + this.selectedDate.month * 30 + this.selectedDate.day;
                    if (currentValue < minValue) {
                        throw new Error('Date is before minimum allowed date');
                    }
                }
                this.currentNepaliDate = { ...this.selectedDate };
                try {
                    // Convert to English
                    const en = this._nepaliDate.nepToEngDate(
                        this.selectedDate.day,
                        this.selectedDate.month,
                        this.selectedDate.year,
                    );
                    this.englishSelectedDate = {
                        day: en.getDate(),
                        month: en.getMonth(),
                        year: en.getFullYear(),
                    };
                } catch (error) {
                    console.error('Error converting Nepali to English date:', error);
                    // If conversion fails, we still want to keep the valid Nepali date
                }
                this.selectedMonthIndex = this.selectedDate.month;
                this.selectedYear = this.selectedDate.year;
            } else {
                // CalendarType.AD
                // Validate AD date ranges
                if (year < 1944 || year > 2043 || month < 1 || month > 12 || day < 1) {
                    throw new Error('Date out of range');
                }
                // Check if day exceeds maximum days in the month and adjust if necessary
                const maxDaysInMonth = this._nepaliDate.isLeapYear(year)
                    ? englishLeapMonths[month - 1]
                    : englishMonths[month - 1];
                if (day > maxDaysInMonth) {
                    console.log(`Adjusting day from ${day} to ${maxDaysInMonth} (max days in month)`);
                    day = maxDaysInMonth;
                }
                this.englishSelectedDate = {
                    year,
                    month: month - 1,
                    day,
                };
                // Validate against max/min dates if set
                if (this.maxDate && this.englishMaxDate) {
                    const maxValue =
                        this.englishMaxDate.year * 365 + this.englishMaxDate.month * 30 + this.englishMaxDate.day;
                    const currentValue =
                        this.englishSelectedDate.year * 365 +
                        this.englishSelectedDate.month * 30 +
                        this.englishSelectedDate.day;
                    if (currentValue > maxValue) {
                        throw new Error('Date exceeds maximum allowed date');
                    }
                }
                if (this.minDate && this.englishMinDate) {
                    const minValue =
                        this.englishMinDate.year * 365 + this.englishMinDate.month * 30 + this.englishMinDate.day;
                    const currentValue =
                        this.englishSelectedDate.year * 365 +
                        this.englishSelectedDate.month * 30 +
                        this.englishSelectedDate.day;
                    if (currentValue < minValue) {
                        throw new Error('Date is before minimum allowed date');
                    }
                }
                try {
                    // Convert to Nepali
                    this.selectedDate = this.toInternalNepaliDate(
                        this._nepaliDate.engToNepDate(
                            this.englishSelectedDate.day,
                            this.englishSelectedDate.month,
                            this.englishSelectedDate.year,
                        ),
                    );
                } catch (error) {
                    console.error('Error converting English to Nepali date:', error);
                    // If conversion fails, we still want to keep the valid English date
                }
                this.selectedMonthIndex = this.englishSelectedDate.month;
                this.selectedYear = this.englishSelectedDate.year;
            }
            // Update the displayed date to reflect any adjustments that were made
            // This manual reconstruction is no longer needed as formatValue() will handle it.
            /*
            if (this.calendarView === CalendarType.BS) { // Changed comparison
              parts[0] = this.dateFormat.startsWith('yyyy') ? this.selectedDate.year.toString() : this.selectedDate.day.toString();
              parts[1] = (this.selectedDate.month + 1).toString().padStart(2, '0');
              parts[2] = this.dateFormat.startsWith('yyyy') ? this.selectedDate.day.toString() : this.selectedDate.year.toString();
            } else { // CalendarType.AD
              parts[0] = this.dateFormat.startsWith('yyyy') ? this.englishSelectedDate.year.toString() : this.englishSelectedDate.day.toString();
              parts[1] = (this.englishSelectedDate.month + 1).toString().padStart(2, '0');
              parts[2] = this.dateFormat.startsWith('yyyy') ? this.englishSelectedDate.day.toString() : this.englishSelectedDate.year.toString();
            }
            this.formattedDate = parts.join(separator);
            */
            // Format the date correctly and emit events
            this.formatValue();
            this.emitDateInAD();
            this.emitDateInBS();
            this.propagateChange(this.getControlValue());
            // Update current date and month data
            this.setCurrentDate();
            this.setCurrentMonthData();
        } catch (error) {
            console.error('Error processing manual input:', error);
            this.isError = true;
            // Reset to previous valid date
            this.formatValue();
            // After a delay, remove the error state
            setTimeout(() => {
                this.isError = false;
            }, 2000);
        }
    }
    fdDateChanges(e: any) {
        try {
            this.selectedDate = this.toInternalNepaliDate(
                this._nepaliDate.engToNepDate(e.getDate(), e.getMonth(), e.getFullYear()),
            );
            this.currentNepaliDate = { ...this.selectedDate };
            this.currentDate = this._nepaliDate.nepToEngDate(
                this.selectedDate.day,
                this.selectedDate.month,
                this.selectedDate.year,
            );
            this.formatValue();
            this.propagateChange(this.getControlValue());
            // Emit both date formats
            this.emitDateInAD();
            this.emitDateInBS();
        } catch (error) {
            console.error('Error in fdDateChanges:', error);
        }
    }
    private dateFormatter = (selectedDate: DateObj) => {
        if (!selectedDate) {
            return '';
        }
        const dd = selectedDate.day < 10 ? `0${selectedDate.day}` : selectedDate.day;
        const mm = selectedDate.month < 9 ? `0${selectedDate.month + 1}` : selectedDate.month + 1;
        // Format date based on the current calendar view and format
        let formattedDate;
        if (this.dateFormat === 'yyyy/mm/dd') {
            formattedDate = `${selectedDate.year}/${mm}/${dd}`;
        } else if (this.dateFormat === 'dd/mm/yyyy') {
            formattedDate = `${dd}/${mm}/${selectedDate.year}`;
        } else if (this.dateFormat === 'yyyy-mm-dd') {
            formattedDate = `${selectedDate.year}-${mm}-${dd}`;
        } else if (this.dateFormat === 'dd-mm-yyyy') {
            formattedDate = `${dd}-${mm}-${selectedDate.year}`;
        } else {
            formattedDate = `${selectedDate.year}/${mm}/${dd}`;
        }
        return formattedDate;
    };
    private setToCurrentDate(emitChange = true) {
        this.currentDate = new Date();
        this.englishSelectedDate = {
            day: this.currentDate.getDate(),
            month: this.currentDate.getMonth(),
            year: this.currentDate.getFullYear(),
        };
        try {
            this.selectedDate = this.toInternalNepaliDate(
                this._nepaliDate.engToNepDate(
                    this.currentDate.getDate(),
                    this.currentDate.getMonth(),
                    this.currentDate.getFullYear(),
                ),
            );
            this.currentNepaliDate = { ...this.selectedDate };
        } catch (error) {
            console.error('Error converting to Nepali date:', error);
            this.selectedDate = { ...this.nepaliDateToday };
            this.currentNepaliDate = { ...this.nepaliDateToday };
        }
        if (this.calendarView === CalendarType.AD) {
            this.englishCurrentDate = { ...this.englishSelectedDate };
            this.selectedMonthIndex = this.englishSelectedDate.month;
            this.selectedYear = this.englishSelectedDate.year;
        } else {
            this.selectedMonthIndex = this.currentNepaliDate.month;
            this.selectedYear = this.currentNepaliDate.year;
        }
        this.formatValue();
        if (emitChange) {
            this.emitDateInAD();
            this.emitDateInBS();
            this.propagateChange(this.getControlValue());
        }
    }
    private setDatepickerColor() {
        if (this.nepaliDatePicker) {
            this.nepaliDatePicker.nativeElement.style.setProperty(
                '--ne-datepicker-primary-color',
                this.rootPrimaryColor,
            );
        }
    }
    private setMaxDate() {
        this.setNepaliMaxDate();
        this.setEnglishMaxDate();
    }
    private setMinDate() {
        this.setNepaliMinDate();
        this.setEnglishMinDate();
    }
    private setInputDate() {
        if (this.dateIn === 'BS') {
            this.setCurrentNepaliDate(this.date);
        } else if (this.dateIn === 'AD') {
            const engDate = new Date(this.date);
            this.currentDate = engDate;
            this.englishSelectedDate = {
                day: engDate.getDate(),
                month: engDate.getMonth(),
                year: engDate.getFullYear(),
            };
            this.englishCurrentDate = { ...this.englishSelectedDate };
            this.selectedDate = this.toInternalNepaliDate(
                this._nepaliDate.engToNepDate(engDate.getDate(), engDate.getMonth(), engDate.getFullYear()),
            );
            this.currentNepaliDate = { ...this.selectedDate };
            this.formatValue();
            this.setCurrentMonthData();
        }
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.selectedMonthIndex = this.selectedDate.month;
            this.selectedYear = this.selectedDate.year;
        } else {
            // CalendarType.AD
            this.selectedMonthIndex = new Date(this.date).getMonth();
            this.selectedYear = new Date(this.date).getFullYear();
        }
    }
    private setCurrentNepaliDate(date: string) {
        const fd = this.changeStructure(date);
        if (!fd) {
            return;
        }
        this.selectedDate = fd;
        this.currentNepaliDate = fd;
        this.currentDate = this._nepaliDate.nepToEngDate(
            this.selectedDate.day,
            this.selectedDate.month,
            this.selectedDate.year,
        );
        this.englishSelectedDate = {
            day: this.currentDate.getDate(),
            month: this.currentDate.getMonth(),
            year: this.currentDate.getFullYear(),
        };
        this.setEnglishCurrentDate();
        this.formatValue();
        this.setCurrentMonthData();
    }
    private setNepaliMaxDate() {
        const maximumDate = new Date(this.maxDate);
        this.nepaliMaxDate = this.toInternalNepaliDate(
            this._nepaliDate.engToNepDate(maximumDate.getDate(), maximumDate.getMonth(), maximumDate.getFullYear()),
        );
    }
    private setEnglishMinDate() {
        const minimumDate = new Date(this.minDate);
        this.englishMinDate = {
            day: minimumDate.getDate(),
            month: minimumDate.getMonth(),
            year: minimumDate.getFullYear(),
        };
    }
    private setEnglishMaxDate() {
        const maximumDate = new Date(this.maxDate);
        this.englishMaxDate = {
            day: maximumDate.getDate(),
            month: maximumDate.getMonth(),
            year: maximumDate.getFullYear(),
        };
    }
    private populateYears() {
        this.years = [];
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            for (let i = 2001; i <= 2099; i++) {
                this.years.push(i);
            }
        } else {
            // CalendarType.AD
            for (let i = 1945; i < 2043; i++) {
                this.years.push(i);
            }
        }
    }
    private resetCurrentMonthData() {
        this.currentMonthData = {
            0: [],
            1: [],
            2: [],
            3: [],
            4: [],
            5: [],
            6: [],
        };
    }
    private formatValue() {
        if (this.calendarView === CalendarType.BS && this.selectedDate) {
            this.formattedDate = this.dateFormatter(this.selectedDate);
        } else if (this.calendarView === CalendarType.AD && this.englishSelectedDate) {
            this.formattedDate = this.dateFormatter(this.englishSelectedDate);
        } else {
            this.formattedDate = '';
        }
    }
    private changeStructure(value: string) {
        let separatedCharacter;
        for (let i = 0; i < this.dateSeparatedCharacters.length; i++) {
            const character = this.dateSeparatedCharacters[i];
            if (value.indexOf(character) !== -1) {
                separatedCharacter = character;
                break;
            }
        }
        if (!separatedCharacter) {
            return;
        }
        const splittedDate = value.split(separatedCharacter);
        return {
            year: Number(splittedDate[0]),
            month: Number(splittedDate[1]) - 1,
            day: Number(splittedDate[2]),
        };
    }
    private setCurrentDate() {
        if (!this.selectedDate) {
            // If no date is selected, use today's date
            this.currentNepaliDate = this.toInternalNepaliDate(
                this._nepaliDate.engToNepDate(
                    this.currentDate.getDate(),
                    this.currentDate.getMonth(),
                    this.currentDate.getFullYear(),
                ),
            );
            this.setEnglishCurrentDate();
        } else {
            // Use the selected date directly
            const { day, month, year } = this.selectedDate;
            this.currentNepaliDate = { day, month, year };
            // We need to make sure currentDate matches the selected date
            try {
                this.currentDate = this._nepaliDate.nepToEngDate(
                    this.selectedDate.day,
                    this.selectedDate.month,
                    this.selectedDate.year,
                );
            } catch (error) {
                console.error('Error in setCurrentDate:', error);
                // Use today as fallback
                this.currentDate = new Date();
            }
            this.setEnglishCurrentDate();
        }
        // Make sure selectedMonthIndex and selectedYear are always in sync
        if (this.calendarView === CalendarType.BS && this.selectedDate) {
            // Changed comparison
            this.selectedMonthIndex = this.selectedDate.month;
            this.selectedYear = this.selectedDate.year;
        } else if (this.calendarView === CalendarType.AD && this.englishSelectedDate) {
            // Changed comparison
            this.selectedMonthIndex = this.englishSelectedDate.month;
            this.selectedYear = this.englishSelectedDate.year;
        }
    }
    private setCurrentMonthData() {
        this.resetCurrentMonthData();
        if (this.calendarView === CalendarType.BS) {
            // Changed comparison
            this.fillNepaliCalendar();
        } else {
            // CalendarType.AD
            this.fillEnglishCalendar();
        }
        this.createEmptySpaces();
    }
    private fillNepaliCalendar() {
        const day = this._nepaliDate
            .nepToEngDate(this.currentNepaliDate.day, this.currentNepaliDate.month, this.currentNepaliDate.year)
            .getDay();
        this.currentMonthData[day] = [this.currentNepaliDate.day];
        this.setMonthDataBefore(day - 1, this.currentNepaliDate.day - 1);
        const currentMonthMaxValue =
            this._nepaliDate.nepaliMonths[this.currentNepaliDate.year - 2000][this.currentNepaliDate.month];
        this.setMonthDataAfter(day + 1, this.currentNepaliDate.day + 1, currentMonthMaxValue);
    }
    private fillEnglishCalendar() {
        const day = new Date(
            `${this.englishCurrentDate.year}/${this.englishCurrentDate.month + 1}/${this.englishCurrentDate.day || 1}`,
        ).getDay();
        const currentEnglishDate = this.englishCurrentDate.day;
        this.currentMonthData[day] = [currentEnglishDate];
        this.setMonthDataBefore(day - 1, currentEnglishDate - 1);
        const monthIndex = this.englishCurrentDate.month;
        const currentMonthMaxValue = this._nepaliDate.isLeapYear(this.englishCurrentDate.year)
            ? englishLeapMonths[monthIndex]
            : englishMonths[monthIndex];
        this.setMonthDataAfter(day + 1, currentEnglishDate + 1, currentMonthMaxValue);
    }
    private setMonthDataBefore(day: any, date: any) {
        if (date >= 1) {
            if (day < 0) {
                day = 6;
            }
            this.currentMonthData[day] = [date, ...this.currentMonthData[day]];
            this.setMonthDataBefore(--day, --date);
        }
    }
    private setMonthDataAfter(day: any, date: any, currentMonthMaxValue: any) {
        if (date <= currentMonthMaxValue) {
            if (day > 6) {
                day = 0;
            }
            this.currentMonthData[day] = [...this.currentMonthData[day], date];
            this.setMonthDataAfter(++day, ++date, currentMonthMaxValue);
        }
    }
    private createEmptySpaces() {
        let dayIndex = 0;
        let value: any;
        Object.values(this.currentMonthData).map((item, index) => {
            value = item;
            if (value.includes(1)) {
                dayIndex = index;
            }
            return value.includes(1);
        });
        if (dayIndex) {
            for (dayIndex; dayIndex > 0; dayIndex--) {
                const monthData = this.currentMonthData[dayIndex - 1];
                this.currentMonthData[dayIndex - 1] = [null, ...monthData];
            }
        }
    }
    private emitDateInAD() {
        try {
            // Only emit if we have a valid selected date
            if (!this.selectedDate) {
                console.log('No BS date to convert to AD for emission');
                return;
            }
            // Create a direct conversion from the selected Nepali date
            const dateInAD = this._nepaliDate.nepToEngDate(
                this.selectedDate.day,
                this.selectedDate.month,
                this.selectedDate.year,
            );
            // Format with DatePipe
            const defaultFormatDate = this._datePipe.transform(dateInAD, "yyyy/MM/dd'T'HH:mm:ss'Z'zzzz");
            // Only proceed if we have a valid formatted date
            if (defaultFormatDate) {
                // Extract the time portion
                this.selectedTimeWithTimezone = defaultFormatDate.substring(defaultFormatDate.indexOf('T'));
                // Get just the date part
                const dateAD = defaultFormatDate.split('T')[0];
                if (dateAD) {
                    // Format according to the requested format
                    const formattedDate = this._nepaliDate.formatDate(dateAD, this.dateFormat);
                    // Add time if requested
                    const dateToReturn = this.setDateWithTime(formattedDate);
                    // Finally emit the date
                    this.dateInAD.emit(dateToReturn);
                    console.log('Emitted AD date:', dateToReturn);
                }
            }
        } catch (error) {
            console.error('Error emitting AD date:', error);
        }
    }
    // Kept as a reference for formatting either BS or AD dates for display.
    /*
    private formatDateForDisplay() {
      // Format based on the current view
      if (this.calendarView === CalendarType.BS && this.selectedDate) { // Changed comparison
        const day = this.selectedDate.day.toString().padStart(2, '0');
        const month = (this.selectedDate.month + 1).toString().padStart(2, '0');
        const year = this.selectedDate.year;
        if (this.dateFormat === 'yyyy/mm/dd') {
          this.formattedDate = `${year}/${month}/${day}`;
        } else if (this.dateFormat === 'dd/mm/yyyy') {
          this.formattedDate = `${day}/${month}/${year}`;
        } else if (this.dateFormat === 'yyyy-mm-dd') {
          this.formattedDate = `${year}-${month}-${day}`;
        } else if (this.dateFormat === 'dd-mm-yyyy') {
          this.formattedDate = `${day}-${month}-${year}`;
        }
      }
      else if (this.calendarView === CalendarType.AD && this.englishSelectedDate) { // Changed comparison
        const day = this.englishSelectedDate.day.toString().padStart(2, '0');
        const month = (this.englishSelectedDate.month + 1).toString().padStart(2, '0');
        const year = this.englishSelectedDate.year;
        if (this.dateFormat === 'yyyy/mm/dd') {
          this.formattedDate = `${year}/${month}/${day}`;
        } else if (this.dateFormat === 'dd/mm/yyyy') {
          this.formattedDate = `${day}/${month}/${year}`;
        } else if (this.dateFormat === 'yyyy-mm-dd') {
          this.formattedDate = `${year}-${month}-${day}`;
        } else if (this.dateFormat === 'dd-mm-yyyy') {
          this.formattedDate = `${day}-${month}-${year}`;
        }
      }
    }
    */
    private emitDateInBS() {
        try {
            // Only emit if we have a valid selected date
            if (!this.selectedDate) {
                console.log('No BS date to emit');
                return;
            }
            // Format the date using the service's method
            const bsDay = this.selectedDate.day.toString().padStart(2, '0');
            const bsMonth = (this.selectedDate.month + 1).toString().padStart(2, '0');
            const bsYear = this.selectedDate.year;
            // Create a properly formatted date string
            const bsDateStr = `${bsYear}/${bsMonth}/${bsDay}`;
            // Use the service to apply the requested format
            const formattedDate = this._nepaliDate.formatDate(bsDateStr, this.dateFormat);
            // Add time if requested
            const dateToReturn = this.setDateWithTime(formattedDate);
            // Finally emit the date
            this.dateInBS.emit(dateToReturn);
            console.log('Emitted BS date:', dateToReturn);
        } catch (error) {
            console.error('Error emitting BS date:', error);
        }
    }
    private setDateWithTime(date: string) {
        if (!this.appendTime) {
            return date;
        }
        return `${date}${this.selectedTimeWithTimezone}`;
    }
    private setEnglishCurrentDate() {
        this.englishCurrentDate = {
            day: this.currentDate.getDate(),
            month: this.currentDate.getMonth(),
            year: this.currentDate.getFullYear(),
        };
    }
    private setNepaliCurrentDate() {
        try {
            this.currentNepaliDate = this.toInternalNepaliDate(
                this._nepaliDate.engToNepDate(
                    this.currentDate.getDate(),
                    this.currentDate.getMonth(),
                    this.currentDate.getFullYear(),
                ),
            );
        } catch (error) {
            console.error('Error setting Nepali current date:', error);
            // Use today's Nepali date as fallback
            this.currentNepaliDate = this.nepaliDateToday;
        }
    }
}
