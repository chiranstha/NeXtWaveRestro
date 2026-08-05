import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { AppEditionExpireAction } from '@shared/AppEnums';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ComboboxItemDto,
    CommonLookupServiceProxy,
    EditionServiceProxy,
    UpdateEditionDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { FeatureTreeComponent } from '../shared/feature-tree.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { NgClass } from '@angular/common';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
@Component({
    selector: 'editEditionModal',
    templateUrl: './edit-edition-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        NgClass,
        ValidationMessagesComponent,
        FeatureTreeComponent,
        ButtonBusyDirective,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EditEditionModalComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    @ViewChild('editModal', { static: true }) modal: ModalDirective;
    @ViewChild('featureTree') featureTree: FeatureTreeComponent;
    @ViewChild('editionForm') editionForm: NgForm;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    active = false;
    saving = false;
    isFormValid = false;
    edition: UpdateEditionDto = new UpdateEditionDto();
    expiringEditions: ComboboxItemDto[] = [];
    expireAction: AppEditionExpireAction = AppEditionExpireAction.DeactiveTenant;
    expireActionEnum: typeof AppEditionExpireAction = AppEditionExpireAction;
    isFree = false;
    isTrialActive = false;
    isWaitingDayActive = false;

    show(editionId?: number): void {
        this.active = true;
        this._commonLookupService.getEditionsForCombobox(true).subscribe(() => {
            this._editionService.getEditionForEdit(editionId).subscribe((editionResult) => {
                this.featureTree.editData = editionResult;
                this.edition.edition = editionResult.edition;
                this.modal.show();
            });
        });
    }
    onShown(): void {
        document.getElementById('EditionDisplayName').focus();
        if (this.editionForm) {
            this.editionForm.form.valueChanges.subscribe(() => {
                this.isFormValid = this.editionForm.form.valid;
            });
        }
    }
    save(): void {
        if (!this.featureTree.areAllValuesValid()) {
            this.message.warn(this.l('InvalidFeaturesWarning'));
            return;
        }
        const input = new UpdateEditionDto();
        input.edition = this.edition.edition;
        input.featureValues = this.featureTree.getGrantedFeatures();
        this.saving = true;
        this._editionService
            .updateEdition(input)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
