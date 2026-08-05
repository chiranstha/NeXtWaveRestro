import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'key-value-list-manager',
    templateUrl: './key-value-list-manager.component.html',
    styleUrls: ['./key-value-list-manager.component.css'],
    imports: [FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class KeyValueListManagerComponent extends AppComponentBase implements OnInit {
    @Input() header: string;
    @Input() keyPlaceHolder: string;
    @Input() valuePlaceHolder: string;
    @Input() items: { key: string; value: string }[];
    addOrEditKey = '';
    addOrEditValue = '';
    isEdit = false;
    ngOnInit(): void {
        if (!this.items) {
            this.items = [];
        }
        if (!this.keyPlaceHolder) {
            this.l('Key');
        }
        if (!this.valuePlaceHolder) {
            this.l('Value');
        }
    }
    onKeyChange() {
        const itemIndex = this.items.findIndex((item) => item.key === this.addOrEditKey);
        this.isEdit = itemIndex !== -1;
        if (this.isEdit) {
            this.addOrEditValue = this.items[itemIndex].value;
        }
    }
    openItemEdit(keyValueItem: { key: string; value: string }) {
        this.addOrEditKey = keyValueItem.key;
        this.addOrEditValue = keyValueItem.value;
        this.isEdit = true;
    }
    removeItem(keyValueItem: { key: string; value: string }) {
        this.items = this.items.filter((item) => item.key !== keyValueItem.key);
        this.onKeyChange();
    }
    addOrEdit() {
        if (!this.addOrEditKey || !this.addOrEditValue) {
            return;
        }
        const newItem = {
            key: this.addOrEditKey,
            value: this.addOrEditValue,
        };
        const indexOfItemInArray = this.items.findIndex((item) => item.key === newItem.key);
        if (indexOfItemInArray !== -1) {
            //edit
            this.items.splice(indexOfItemInArray, 1, newItem);
        } else {
            this.items.push(newItem);
        }
        this.addOrEditKey = '';
        this.addOrEditValue = '';
    }
    getItems(): { key: string; value: string }[] {
        return this.items;
    }
}
