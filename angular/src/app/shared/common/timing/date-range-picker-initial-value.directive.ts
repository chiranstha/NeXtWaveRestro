import { AfterViewInit, Directive, ElementRef, Input, inject } from '@angular/core';
import { DateTimeService } from './date-time.service';
@Directive({ selector: '[dateRangePickerInitialValue]' })
export class DateRangePickerInitialValueSetterDirective implements AfterViewInit {
    private _element = inject(ElementRef);
    private _dateTimeService = inject(DateTimeService);
    @Input() ngModel;
    hostElement: ElementRef;
    constructor() {
        const { _element } = this;
        this.hostElement = _element;
    }
    ngAfterViewInit(): void {
        if (this.ngModel?.[0] && this.ngModel[1]) {
            setTimeout(() => {
                const value = `${this._dateTimeService.formatDate(
                    this.ngModel[0],
                    'F',
                )} - ${this._dateTimeService.formatDate(this.ngModel[1], 'F')}`;
                this.hostElement.nativeElement.value = value;
            });
        }
    }
}
