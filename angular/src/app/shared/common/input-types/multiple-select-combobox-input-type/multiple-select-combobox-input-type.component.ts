import { Component, Injector, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { InputTypeComponentBase } from '../input-type-component-base';
import { AutoComplete } from '@shared/ui-compat';
import { FormsModule } from '@angular/forms';
@Component({
    selector: 'app-multiple-select-input-type',
    templateUrl: './multiple-select-combobox-input-type.component.html',
    imports: [AutoComplete, FormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class MultipleSelectComboboxInputTypeComponent extends InputTypeComponentBase implements OnInit {
    filteredValues: string[];
    constructor() {
        const injector = inject(Injector);
        super(injector);
    }
    ngOnInit() {
        this.filteredValues = this.allValues;
    }
    getSelectedValues(): string[] {
        if (!this.selectedValues) {
            return [];
        }
        return this.selectedValues;
    }
    filter(event) {
        this.filteredValues = this.allValues.filter((item) => item.toLowerCase().includes(event.query.toLowerCase()));
    }
}
