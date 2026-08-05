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
    CreateOrganizationUnitInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    UpdateOrganizationUnitInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
export interface IOrganizationUnitOnEdit {
    id?: number;
    parentId?: number;
    displayName?: string;
}
@Component({
    selector: 'createOrEditOrganizationUnitModal',
    templateUrl: './create-or-edit-unit-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateOrEditUnitModalComponent extends AppComponentBase {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _changeDetector = inject(ChangeDetectorRef);
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @ViewChild('organizationUnitDisplayName', { static: true }) organizationUnitDisplayNameInput: ElementRef;
    @ViewChild('editForm') organizationUnitForm: any;
    @Output() unitCreated: EventEmitter<OrganizationUnitDto> = new EventEmitter<OrganizationUnitDto>();
    @Output() unitUpdated: EventEmitter<OrganizationUnitDto> = new EventEmitter<OrganizationUnitDto>();
    active = false;
    saving = false;
    isFormValid = false;
    organizationUnit: IOrganizationUnitOnEdit = {};

    onShown(): void {
        document.getElementById('OrganizationUnitDisplayName').focus();
        if (this.organizationUnitForm) {
            this.organizationUnitForm.form.valueChanges.subscribe(() => {
                this.isFormValid = this.organizationUnitForm.form.valid;
                this._changeDetector.markForCheck();
            });
        }
    }
    show(organizationUnit: IOrganizationUnitOnEdit): void {
        this.organizationUnit = organizationUnit;
        this.active = true;
        this.modal.show();
        this._changeDetector.markForCheck();
    }
    save(): void {
        if (!this.organizationUnit.id) {
            this.createUnit();
        } else {
            this.updateUnit();
        }
    }
    createUnit() {
        const createInput = new CreateOrganizationUnitInput();
        createInput.parentId = this.organizationUnit.parentId;
        createInput.displayName = this.organizationUnit.displayName;
        this.saving = true;
        this._organizationUnitService
            .createOrganizationUnit(createInput)
            .pipe(
                finalize(() => {
                    this.saving = false;
                    this._changeDetector.markForCheck();
                }),
            )
            .subscribe((result: OrganizationUnitDto) => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.unitCreated.emit(result);
            });
    }
    updateUnit() {
        const updateInput = new UpdateOrganizationUnitInput();
        updateInput.id = this.organizationUnit.id;
        updateInput.displayName = this.organizationUnit.displayName;
        this.saving = true;
        this._organizationUnitService
            .updateOrganizationUnit(updateInput)
            .pipe(
                finalize(() => {
                    this.saving = false;
                    this._changeDetector.markForCheck();
                }),
            )
            .subscribe((result: OrganizationUnitDto) => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.unitUpdated.emit(result);
            });
    }
    close(): void {
        this.modal.hide();
        this.active = false;
        this._changeDetector.markForCheck();
    }
}
