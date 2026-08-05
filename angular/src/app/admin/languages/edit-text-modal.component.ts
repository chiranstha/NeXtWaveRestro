import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LanguageServiceProxy, UpdateLanguageTextInput } from '@shared/service-proxies/service-proxies';
import { find as _find } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'editTextModal',
    templateUrl: './edit-text-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, NgClass, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EditTextModalComponent extends AppComponentBase {
    private _languageService = inject(LanguageServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('modal', { static: true }) modal: ModalDirective;
    @ViewChild('editForm') editTextForm: any;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    model: UpdateLanguageTextInput = new UpdateLanguageTextInput();
    baseText: string;
    baseLanguage: abp.localization.ILanguageInfo;
    targetLanguage: abp.localization.ILanguageInfo;
    active = false;
    saving = false;
    isFormValid = false;

    show(
        baseLanguageName: string,
        targetLanguageName: string,
        sourceName: string,
        key: string,
        baseText: string,
        targetText: string,
    ): void {
        this.model.sourceName = sourceName;
        this.model.key = key;
        this.model.languageName = targetLanguageName;
        this.model.value = targetText;
        this.baseText = baseText;
        this.baseLanguage = _find(abp.localization.languages, (l) => l.name === baseLanguageName);
        this.targetLanguage = _find(abp.localization.languages, (l) => l.name === targetLanguageName);
        this.active = true;
        this.modal.show();
        this._cdr.markForCheck();
    }
    onShown(): void {
        document.getElementById('TargetLanguageDisplayName').focus();
        if (this.editTextForm) {
            this.editTextForm.form.valueChanges.subscribe(() => {
                this.isFormValid = this.editTextForm.form.valid;
                this._cdr.markForCheck();
            });
        }
    }
    save(): void {
        this.saving = true;
        this._languageService
            .updateLanguageText(this.model)
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
    private findLanguage(name: string): abp.localization.ILanguageInfo {
        return _find(abp.localization.languages, (l) => l.name === name);
    }
}
