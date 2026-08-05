import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { AppEditionExpireAction } from '@shared/AppEnums';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ComboboxItemDto,
    CommonLookupServiceProxy,
    CreateEditionDto,
    EditionServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { FeatureTreeComponent } from '../shared/feature-tree.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { IMaskDirective } from 'angular-imask';
import { NgClass } from '@angular/common';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'createEditionModal',
    templateUrl: './create-edition-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        IMaskDirective,
        NgClass,
        FeatureTreeComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateEditionModalComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    @ViewChild('createModal', { static: true }) modal: ModalDirective;
    @ViewChild('featureTree') featureTree: FeatureTreeComponent;
    @ViewChild('editionForm') editionForm: NgForm;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    active = false;
    saving = false;
    isFormValid = false;
    currencyMask = {
        mask: Number,
        scale: 2,
        signed: true,
        radix: '.',
    };
    edition: CreateEditionDto = new CreateEditionDto();
    expiringEditions: ComboboxItemDto[] = [];
    expireAction: AppEditionExpireAction = AppEditionExpireAction.DeactiveTenant;
    expireActionEnum: typeof AppEditionExpireAction = AppEditionExpireAction;
    isFree = true;
    isTrialActive = false;
    isWaitingDayActive = false;

    show(editionId?: number): void {
        this.active = true;
        this._commonLookupService.getEditionsForCombobox(true).subscribe((editionsResult) => {
            this.expiringEditions = editionsResult.items;
            this.expiringEditions.unshift(
                new ComboboxItemDto({ value: null, displayText: this.l('NotAssigned'), isSelected: true }),
            );
            this._editionService.getEditionForEdit(editionId).subscribe((editionResult) => {
                this.featureTree.editData = editionResult;
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
    resetPrices(_isFree) {
        this.edition.edition.annualPrice = undefined;
        this.edition.edition.monthlyPrice = undefined;
    }
    removeExpiringEdition(_isDeactivateTenant) {
        this.edition.edition.expiringEditionId = null;
    }
    save(): void {
        if (!this.featureTree.areAllValuesValid()) {
            this.message.warn(this.l('InvalidFeaturesWarning'));
            return;
        }
        const input = new CreateEditionDto();
        input.edition = this.edition.edition;
        input.featureValues = this.featureTree.getGrantedFeatures();
        if (!this.isTrialActive) {
            this.edition.edition.trialDayCount = null;
        }
        this.saving = true;
        this._editionService
            .createEdition(input)
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
