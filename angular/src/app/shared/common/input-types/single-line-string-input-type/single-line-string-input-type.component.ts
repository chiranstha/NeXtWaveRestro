import { Component, Injector, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { InputTypeComponentBase } from '../input-type-component-base';
import { FormsModule } from '@angular/forms';
@Component({
    selector: 'app-single-line-string-input-type',
    templateUrl: './single-line-string-input-type.component.html',
    styleUrls: ['./single-line-string-input-type.component.css'],
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class SingleLineStringInputTypeComponent extends InputTypeComponentBase implements OnInit {
    selectedValue: string;
    constructor() {
        const injector = inject(Injector);
        super(injector);
    }
    getSelectedValues(): string[] {
        if (!this.selectedValue) {
            return [];
        }
        return [this.selectedValue];
    }
    ngOnInit(): void {
        this.selectedValue = this.selectedValues[0];
    }
}
