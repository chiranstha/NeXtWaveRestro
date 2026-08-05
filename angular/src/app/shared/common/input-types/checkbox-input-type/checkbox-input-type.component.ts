import { Component, Injector, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { InputTypeComponentBase } from '../input-type-component-base';
import { FormsModule } from '@angular/forms';
@Component({
    selector: 'app-checkbox-input-type',
    templateUrl: './checkbox-input-type.component.html',
    styleUrls: ['./checkbox-input-type.component.css'],
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CheckboxInputTypeComponent extends InputTypeComponentBase implements OnInit {
    checked: boolean;
    constructor() {
        const injector = inject(Injector);
        super(injector);
    }
    ngOnInit() {
        this.checked = this.selectedValues && this.selectedValues[0] && this.selectedValues[0] === 'true';
    }
    getSelectedValues(): string[] {
        return [this.checked.toString()];
    }
}
