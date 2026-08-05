import { Component, Injector, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { InputTypeComponentBase } from '../input-type-component-base';
import { FormsModule } from '@angular/forms';
@Component({
    selector: 'app-combobox-input-type',
    templateUrl: './combobox-input-type.component.html',
    styleUrls: ['./combobox-input-type.component.css'],
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ComboboxInputTypeComponent extends InputTypeComponentBase implements OnInit {
    selectedValue: string;
    constructor() {
        const injector = inject(Injector);
        super(injector);
    }
    ngOnInit() {
        this.selectedValue = this.selectedValues[0];
    }
    getSelectedValues(): string[] {
        if (!this.selectedValue) {
            return [];
        }
        return [this.selectedValue];
    }
}
