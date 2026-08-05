import { Component, EventEmitter, OnInit, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'excel-column-selection-modal',
    templateUrl: './excel-column-selection-modal.html',
    imports: [AppBsModalDirective, FormsModule, ReactiveFormsModule, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ExcelColumnSelectionModalComponent extends AppComponentBase implements OnInit {
    private fb = inject(FormBuilder);
    @ViewChild('exportExcelModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    active = false;
    saving = false;
    form: FormGroup;
    toggleAllBtnText: string;
    selectedColumnsList: string[];
    constructor() {
        super();
    }
    ngOnInit(): void {
        super.ngOnInit();
        this.form = this.fb.group({
            selectedColumns: this.fb.array([]),
        });
    }
    show(excelColumns: string[]): void {
        this.active = true;
        this.toggleAllBtnText = this.l('SelectAll');
        this.selectedColumnsList = excelColumns;
        this.form = this.fb.group({
            selectedColumns: this.fb.array(Array(excelColumns.length).fill(false)),
        });
        this.modal.show();
    }
    toggleClick(): void {
        const formArray = this.form.controls.selectedColumns as FormArray;
        const allSelected = formArray.controls.every((control) => control.value);
        if (allSelected) {
            formArray.controls.forEach((control) => {
                control.setValue(false);
                this.toggleAllBtnText = this.l('SelectAll');
            });
        } else {
            formArray.controls.forEach((control) => {
                control.setValue(true);
                this.toggleAllBtnText = this.l('UnselectAll');
            });
        }
    }
    selectedColumnChange(): void {
        const formArray = this.form.controls.selectedColumns as FormArray;
        const allSelected = formArray.controls.every((control) => control.value);
        if (allSelected) {
            formArray.controls.forEach(() => {
                this.toggleAllBtnText = this.l('UnselectAll');
            });
        } else {
            formArray.controls.forEach(() => {
                this.toggleAllBtnText = this.l('SelectAll');
            });
        }
    }
    selectColumns(): void {
        this.saving = true;
        const formArray = this.form.controls.selectedColumns as FormArray;
        const selectedColumns = formArray.controls
            .map((control, index) => ({ index, value: control.value }))
            .filter((control) => control.value)
            .map((control) => this.selectedColumnsList[control.index]);
        this.modalSave.emit(selectedColumns);
        this.saving = false;
        this.close();
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
