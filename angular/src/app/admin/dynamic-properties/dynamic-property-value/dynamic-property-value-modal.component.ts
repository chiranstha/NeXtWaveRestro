import { Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DynamicPropertyValueDto, DynamicPropertyValueServiceProxy } from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    selector: 'dynamic-property-value-modal',
    templateUrl: './dynamic-property-value-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, BusyIfDirective, AgGridAngular, LocalizePipe, PermissionPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DynamicPropertyValueModalComponent extends AppComponentBase {
    private _dynamicPropertyValueAppService = inject(DynamicPropertyValueServiceProxy);
    @ViewChild('createOrEditModal') modal: ModalDirective;
    dynamicPropertyValue: DynamicPropertyValueDto;
    createOrEditValueEnabled = false;
    saving = false;
    dynamicPropertyId: number;
    private gridApi!: GridApi;
    public rowData: DynamicPropertyValueDto[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: this.l('Values'),
            field: 'value',
            sortable: true,
            filter: true,
        },
        {
            headerName: '',
            field: 'edit',
            width: 100,
            cellRenderer: this.editCellRenderer.bind(this),
            sortable: false,
            filter: false,
            resizable: false,
        },
        {
            headerName: '',
            field: 'delete',
            width: 100,
            cellRenderer: this.deleteCellRenderer.bind(this),
            sortable: false,
            filter: false,
            resizable: false,
        },
    ];

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.getDynamicProperties();
    }
    getDynamicProperties(): void {
        this.showMainSpinner();
        this._dynamicPropertyValueAppService.getAllValuesOfDynamicProperty(this.dynamicPropertyId).subscribe(
            (result) => {
                this.rowData = result.items;
                this.hideMainSpinner();
            },
            () => {
                this.hideMainSpinner();
            },
        );
    }
    editDynamicPropertyValue(dynamicPropertyValueId: number): void {
        this._dynamicPropertyValueAppService.get(dynamicPropertyValueId).subscribe(
            (data) => {
                this.dynamicPropertyValue = data;
                this.createOrEditValueEnabled = true;
                this.hideMainSpinner();
            },
            () => {
                this.hideMainSpinner();
            },
        );
    }
    deleteDynamicPropertyValue(dynamicPropertyValueId: number): void {
        this.message.confirm(this.l('DeleteDynamicPropertyValueMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._dynamicPropertyValueAppService.delete(dynamicPropertyValueId).subscribe(() => {
                    abp.notify.success(this.l('SuccessfullyDeleted'));
                    this.getDynamicProperties();
                });
            }
        });
    }
    createDynamicPropertyValue(): void {
        this.dynamicPropertyValue = new DynamicPropertyValueDto();
        this.dynamicPropertyValue.dynamicPropertyId = this.dynamicPropertyId;
        this.createOrEditValueEnabled = true;
    }
    show(dynamicPropertyId?: number) {
        this.dynamicPropertyValue = new DynamicPropertyValueDto();
        this.dynamicPropertyValue.dynamicPropertyId = dynamicPropertyId;
        this.dynamicPropertyId = dynamicPropertyId;
        this.getDynamicProperties();
        this.modal.show();
        return;
    }
    save(): void {
        this.saving = true;
        this.showMainSpinner();
        let observable: Observable<void>;
        if (!this.dynamicPropertyValue.id) {
            observable = this._dynamicPropertyValueAppService.add(this.dynamicPropertyValue);
        } else {
            observable = this._dynamicPropertyValueAppService.update(this.dynamicPropertyValue);
        }
        observable.subscribe(
            () => {
                this.getDynamicProperties();
                this.notify.info(this.l('SavedSuccessfully'));
                this.hideMainSpinner();
                this.saving = false;
                this.createOrEditValueEnabled = false;
            },
            () => {
                this.hideMainSpinner();
                this.saving = false;
                this.createOrEditValueEnabled = false;
            },
        );
    }
    close(): void {
        this.modal.hide();
    }
    private editCellRenderer(params: any): HTMLElement | null {
        if (!this.permission.isGranted('Pages.Administration.DynamicPropertyValue.Edit')) {
            return null;
        }
        const record = params.data;
        const editBtn = document.createElement('button');
        editBtn.className = 'btn btn-sm btn-primary';
        editBtn.innerText = this.l('Edit');
        editBtn.onclick = () => this.editDynamicPropertyValue(record.id);
        return editBtn;
    }
    private deleteCellRenderer(params: any): HTMLElement | null {
        if (!this.permission.isGranted('Pages.Administration.DynamicPropertyValue.Delete')) {
            return null;
        }
        const record = params.data;
        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'btn btn-sm btn-danger';
        deleteBtn.innerText = this.l('Delete');
        deleteBtn.onclick = () => this.deleteDynamicPropertyValue(record.id);
        return deleteBtn;
    }
}
