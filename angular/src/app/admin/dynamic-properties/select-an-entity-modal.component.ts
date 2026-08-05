import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DynamicEntityPropertyDefinitionServiceProxy,
    DynamicEntityPropertyServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { forkJoin } from 'rxjs';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'select-and-entity-modal',
    templateUrl: './select-an-entity-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class SelectAnEntityModalComponent extends AppComponentBase {
    private _dynamicEntityPropertyService = inject(DynamicEntityPropertyServiceProxy);
    private _dynamicEntityPropertyDefinitionService = inject(DynamicEntityPropertyDefinitionServiceProxy);
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('createModal') modal: ModalDirective;
    allEntities: string[];
    initialized = false;
    saving = false;
    entityFullName: string;

    show(): void {
        this.initialize();
        this.modal.show();
    }
    save(): void {
        this.saving = true;
        this.showMainSpinner();
        this.modalSave.emit(this.entityFullName);
        this.modal.hide();
    }
    close(): void {
        this.modal.hide();
    }
    private initialize() {
        if (this.initialized) {
            return;
        }
        this.showMainSpinner();
        const allEntitiesObservable = this._dynamicEntityPropertyDefinitionService.getAllEntities();
        const allEntitiesHasPropertyObservable = this._dynamicEntityPropertyService.getAllEntitiesHasDynamicProperty();
        forkJoin([allEntitiesObservable, allEntitiesHasPropertyObservable]).subscribe(
            ([allEntities, allEntitiesHasProperty]) => {
                this.allEntities = allEntities.filter((element) => {
                    return allEntitiesHasProperty.items.map((item) => item.entityFullName).indexOf(element) === -1;
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
