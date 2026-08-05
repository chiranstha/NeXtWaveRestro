import { AfterViewInit, Directive, ElementRef, NgZone, inject } from '@angular/core';
@Directive({ selector: '[autoFocus]' })
export class AutoFocusDirective implements AfterViewInit {
    private _element = inject(ElementRef);
    private _ngZone = inject(NgZone);
    constructor() {}
    ngAfterViewInit(): void {
        this._ngZone.runOutsideAngular(() => {
            setTimeout(() => {
                this._element.nativeElement.focus();
            });
        });
    }
}
