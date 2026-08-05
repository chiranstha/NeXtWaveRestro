import { Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ComboboxItemDto,
    CommonLookupServiceProxy,
    EditionServiceProxy,
    MoveTenantsToAnotherEditionDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppConsts } from '@shared/AppConsts';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { RouterLink } from '@angular/router';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'moveTenantsToAnotherEditionModal',
    templateUrl: './move-tenants-to-another-edition-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, RouterLink, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class MoveTenantsToAnotherEditionModalComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    @ViewChild('editModal', { static: true }) modal: ModalDirective;
    @ViewChild('MoveTenantsForm') moveTenantsForm: NgForm;
    active = false;
    saving = false;
    isFormValid = false;
    appBaseUrl = '';
    tenantCount = 0;
    moveTenantsInput: MoveTenantsToAnotherEditionDto = new MoveTenantsToAnotherEditionDto();
    targetEditions: ComboboxItemDto[] = [];

    show(editionId: number): void {
        this.active = true;
        this.moveTenantsInput.sourceEditionId = editionId;
        this._commonLookupService.getEditionsForCombobox(undefined).subscribe((editionsResult) => {
            this.targetEditions = editionsResult.items.filter((item) => item.value != editionId.toString());
            this.modal.show();
            if (this.moveTenantsForm) {
                this.moveTenantsForm.form.valueChanges.subscribe(() => {
                    this.isFormValid = this.moveTenantsForm.form.valid;
                });
            }
        });
        this._editionService.getTenantCount(editionId).subscribe((editionCountResult) => {
            this.tenantCount = editionCountResult;
            this.appBaseUrl = AppConsts.appBaseUrl;
        });
    }
    save(): void {
        if (!this.canChangeEdition()) {
            abp.message.warn(this.l('SameEditionChangeErrorMessage'));
            return;
        }
        this.saving = true;
        this._editionService
            .moveTenantsToAnotherEdition(this.moveTenantsInput)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.info(this.l('MoveTenantsToAnotherEditionStartedNotification'));
                this.close();
            });
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
    private canChangeEdition(): boolean {
        return this.moveTenantsInput.targetEditionId.toString() !== this.moveTenantsInput.sourceEditionId.toString();
    }
}
