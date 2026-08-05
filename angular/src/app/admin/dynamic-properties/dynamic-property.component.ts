import { Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DynamicPropertyDto, DynamicPropertyServiceProxy } from '@shared/service-proxies/service-proxies';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { CreateOrEditDynamicPropertyModalComponent } from './create-or-edit-dynamic-property-modal.component';
import { InputTypeConfigurationService } from '@app/shared/common/input-types/input-type-configuration.service';
import { DynamicPropertyValueModalComponent } from '@app/admin/dynamic-properties/dynamic-property-value/dynamic-property-value-modal.component';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { DynamicEntityPropertyListComponent } from './dynamic-entity-properties/dynamic-entity-property-list.component';
import { DynamicPropertyValueModalComponent as DynamicPropertyValueModalComponent_1 } from './dynamic-property-value/dynamic-property-value-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './dynamic-property.component.html',
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        BusyIfDirective,
        AgGridAngular,
        DynamicEntityPropertyListComponent,
        DynamicPropertyValueModalComponent_1,
        CreateOrEditDynamicPropertyModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DynamicPropertyComponent extends AppComponentBase {
    private _dynamicPropertyService = inject(DynamicPropertyServiceProxy);
    private _router = inject(Router);
    private _inputTypeConfigurationService = inject(InputTypeConfigurationService);
    @ViewChild('createOrEditDynamicProperty', { static: true })
    createOrEditDynamicPropertyModal: CreateOrEditDynamicPropertyModalComponent;
    @ViewChild('dynamicPropertyValueModal', { static: true })
    dynamicPropertyValueModal: DynamicPropertyValueModalComponent;
    private gridApi!: GridApi;
    public rowData: DynamicPropertyDto[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: '',
            field: 'actions',
            width: 130,
            cellRenderer: this.actionsCellRenderer.bind(this),
            sortable: false,
            filter: false,
            resizable: false,
        },
        {
            headerName: this.l('PropertyName'),
            field: 'propertyName',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('DisplayName'),
            field: 'displayName',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('InputType'),
            field: 'inputType',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('Permission'),
            field: 'permission',
            sortable: true,
            filter: true,
        },
    ];

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.getDynamicProperties();
    }
    getDynamicProperties(): void {
        this.showMainSpinner();
        this._dynamicPropertyService.getAll().subscribe(
            (result) => {
                this.rowData = result.items;
                this.hideMainSpinner();
            },
            () => {
                this.hideMainSpinner();
            },
        );
    }
    addNewDynamicProperty(): void {
        this.createOrEditDynamicPropertyModal.show();
    }
    editDynamicProperty(dynamicPropertyId: number): void {
        this.createOrEditDynamicPropertyModal.show(dynamicPropertyId);
    }
    deleteDynamicProperty(dynamicPropertyId: number): void {
        this.message.confirm(this.l('DeleteDynamicPropertyMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._dynamicPropertyService.delete(dynamicPropertyId).subscribe(() => {
                    abp.notify.success(this.l('SuccessfullyDeleted'));
                    this.getDynamicProperties();
                });
            }
        });
    }
    editValues(dynamicProperty: DynamicPropertyDto): void {
        this.dynamicPropertyValueModal.show(dynamicProperty.id);
    }
    hasValues(inputType: string): boolean {
        return this._inputTypeConfigurationService.getByName(inputType).hasValues;
    }
    private actionsCellRenderer(params: any): HTMLElement {
        const record = params.data;
        const container = document.createElement('div');
        container.className = 'btn-group';
        if (this.permission.isGranted('Pages.Administration.DynamicProperties.Edit')) {
            const editBtn = document.createElement('button');
            editBtn.className = 'btn btn-sm btn-outline-primary';
            editBtn.innerText = this.l('Edit');
            editBtn.onclick = () => this.editDynamicProperty(record.id);
            container.appendChild(editBtn);
        }
        if (this.permission.isGranted('Pages.Administration.DynamicProperties.Delete')) {
            const deleteBtn = document.createElement('button');
            deleteBtn.className = 'btn btn-sm btn-outline-danger';
            deleteBtn.innerText = this.l('Delete');
            deleteBtn.onclick = () => this.deleteDynamicProperty(record.id);
            container.appendChild(deleteBtn);
        }
        if (
            this.permission.isGranted('Pages.Administration.DynamicPropertyValue.Edit') &&
            this.hasValues(record.inputType)
        ) {
            const editValuesBtn = document.createElement('button');
            editValuesBtn.className = 'btn btn-sm btn-outline-secondary';
            editValuesBtn.innerText = this.l('EditValues');
            editValuesBtn.onclick = () => this.editValues(record);
            container.appendChild(editValuesBtn);
        }
        return container;
    }
}
