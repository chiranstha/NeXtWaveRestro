import { AfterViewInit, Directive, ElementRef, Input, inject } from '@angular/core';
@Directive({ selector: '[datePickerInitialValue]' })
export class DatePickerInitialValueSetterDirective implements AfterViewInit {
    private _element = inject(ElementRef);
    @Input() ngModel;
    hostElement: ElementRef;
    constructor() {
        const { _element } = this;
        this.hostElement = _element;
    }
    ngAfterViewInit(): void {
        if (this.ngModel) {
            setTimeout(() => {
                this.hostElement.nativeElement.value = this.ngModel.toFormat('D');
            });
        }
    }
}
