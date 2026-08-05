import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DynamicEntityPropertyDefinitionServiceProxy,
    DynamicEntityPropertyDto,
    DynamicEntityPropertyServiceProxy,
    DynamicPropertyDto,
    DynamicPropertyServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { forkJoin } from 'rxjs';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'create-dynamic-entity-property-modal',
    templateUrl: './create-dynamic-entity-property-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateDynamicEntityPropertyModalComponent extends AppComponentBase {
    private _dynamicEntityPropertyService = inject(DynamicEntityPropertyServiceProxy);
    private _dynamicPropertyService = inject(DynamicPropertyServiceProxy);
    private _dynamicEntityParameterDefinitionService = inject(DynamicEntityPropertyDefinitionServiceProxy);
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('createModal') modal: ModalDirective;
    dynamicEntityProperty = new DynamicEntityPropertyDto();
    allDynamicProperties: DynamicPropertyDto[];
    initialized = false;
    saving = false;
    private entityFullName: string;

    show(entityFullName: string): void {
        this.entityFullName = entityFullName;
        this.initialize();
        this.dynamicEntityProperty = new DynamicEntityPropertyDto();
        this.dynamicEntityProperty.entityFullName = this.entityFullName;
        this.modal.show();
    }
    save(): void {
        this.saving = true;
        this.showMainSpinner();
        this.dynamicEntityProperty.tenantId = abp.session.tenantId; // remove that
        this._dynamicEntityPropertyService.add(this.dynamicEntityProperty).subscribe(
            () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.hideMainSpinner();
                this.modalSave.emit(null);
                this.modal.hide();
                this.saving = false;
            },
            () => {
                this.hideMainSpinner();
                this.saving = false;
            },
        );
    }
    close(): void {
        this.modal.hide();
    }
    private initialize() {
        this.initialized = false;
        const definedParametersObservable = this._dynamicEntityPropertyService.getAllPropertiesOfAnEntity(
            this.entityFullName,
        );
        const allParametersObservable = this._dynamicPropertyService.getAll();
        this.showMainSpinner();
        forkJoin([allParametersObservable, definedParametersObservable]).subscribe(
            ([dynamicProperties, definedParameters]) => {
                this.allDynamicProperties = dynamicProperties.items.filter((element) => {
                    return definedParameters.items.map((item) => item.dynamicPropertyId).indexOf(element.id) === -1;
                });
                this.hideMainSpinner();
                this.initialized = true;
            },
            () => {
                this.hideMainSpinner();
            },
        );
    }
}
