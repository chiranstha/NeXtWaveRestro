import {
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationLanguageEditDto,
    CreateOrUpdateLanguageInput,
    LanguageServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { SelectItem, AppTemplate } from '@shared/ui-compat';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { Select } from '@shared/ui-compat';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { NgClass } from '@angular/common';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'createOrEditLanguageModal',
    templateUrl: './create-or-edit-language-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        Select,
        ValidationMessagesComponent,
        AppTemplate,
        NgClass,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateOrEditLanguageModalComponent extends AppComponentBase {
    private _languageService = inject(LanguageServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @ViewChild('languageCombobox', { static: true }) languageCombobox: ElementRef;
    @ViewChild('iconCombobox', { static: true }) iconCombobox: ElementRef;
    @ViewChild('editForm') languageForm: any;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    active = false;
    saving = false;
    isFormValid = false;
    language: ApplicationLanguageEditDto = new ApplicationLanguageEditDto();
    languageNamesSelectItems: SelectItem[] = [];
    flagsSelectItems: SelectItem[] = [];

    show(languageId?: number): void {
        this.active = true;
        this._cdr.markForCheck();
        this._languageService.getLanguageForEdit(languageId).subscribe((result) => {
            this.language = result.language;
            this.languageNamesSelectItems = result.languageNames.map((language) => {
                return {
                    label: language.displayText,
                    value: language.value,
                };
            });
            this.flagsSelectItems = result.flags.map((flag) => {
                return {
                    label: flag.displayText,
                    value: flag.value,
                };
            });
            if (!languageId) {
                this.language.isEnabled = true;
            }
            this.modal.show();
            if (this.languageForm) {
                this.languageForm.form.valueChanges.subscribe(() => {
                    this.isFormValid = this.languageForm.form.valid;
                    this._cdr.markForCheck();
                });
            }
            this._cdr.markForCheck();
        });
    }
    save(): void {
        const input = new CreateOrUpdateLanguageInput();
        input.language = this.language;
        this.saving = true;
        this._languageService
            .createOrUpdateLanguage(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                    this._cdr.markForCheck();
                }),
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }
    close(): void {
        this.active = false;
        this.modal.hide();
        this._cdr.markForCheck();
    }
}
