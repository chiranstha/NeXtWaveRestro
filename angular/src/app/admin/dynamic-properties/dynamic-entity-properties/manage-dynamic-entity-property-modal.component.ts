import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DynamicEntityPropertyServiceProxy } from '@shared/service-proxies/service-proxies';
import { CreateDynamicEntityPropertyModalComponent } from './create-dynamic-entity-property-modal.component';
import { ActivatedRoute } from '@angular/router';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    selector: 'manage-dynamic-entity-property-modal',
    templateUrl: './manage-dynamic-entity-property-modal.component.html',
    imports: [
        AppBsModalDirective,
        AgGridAngular,
        CreateDynamicEntityPropertyModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ManageDynamicEntityPropertyModalComponent extends AppComponentBase {
    private _activatedRoute = inject(ActivatedRoute);
    private _dynamicEntityPropertyService = inject(DynamicEntityPropertyServiceProxy);
    @Output() onPropertyChange: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('createDynamicEntityPropertyModal')
    createDynamicEntityPropertyModal: CreateDynamicEntityPropertyModalComponent;
    @ViewChild('createModal') modal: ModalDirective;
    entityFullName: string;
    private gridApi!: GridApi;
    public rowData: any[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: this.l('DynamicProperty'),
            field: 'dynamicPropertyName',
            sortable: true,
            filter: true,
        },
        {
            headerName: '',
            field: 'actions',
            width: 100,
            cellRenderer: this.actionsCellRenderer.bind(this),
            sortable: false,
            filter: false,
            resizable: false,
        },
    ];

    show(entityFullName: string): void {
        this.entityFullName = entityFullName;
        this.modal.show();
    }
    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        params.api.setGridOption('context', { componentParent: this });
        this.getDynamicEntityProperties();
    }
    handlePropertyChanges(): void {
        this.onPropertyChange.emit(null);
        this.getDynamicEntityProperties();
    }
    getDynamicEntityProperties(): void {
        this.showMainSpinner();
        this._dynamicEntityPropertyService.getAllPropertiesOfAnEntity(this.entityFullName).subscribe(
            (result) => {
                this.rowData = result.items;
                this.hideMainSpinner();
            },
            () => {
                this.hideMainSpinner();
            },
        );
    }
    addNewDynamicEntityProperty(): void {
        this.createDynamicEntityPropertyModal.show(this.entityFullName);
    }
    deleteDynamicEntityProperty(id: number): void {
        this.message.confirm(this.l('DeleteDynamicPropertyMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._dynamicEntityPropertyService.delete(id).subscribe(() => {
                    abp.notify.success(this.l('SuccessfullyDeleted'));
                    this.handlePropertyChanges();
                });
            }
        });
    }
    onCellClicked(event: any): void {
        if (event.colDef.field === 'actions' && event.event.target.tagName === 'BUTTON') {
            const { id } = event.data;
            this.deleteDynamicEntityProperty(id);
        }
    }
    private actionsCellRenderer(params: any): string {
        const context = params.context.componentParent;
        const hasPermission = context.permission.isGranted('Pages.Administration.DynamicEntityProperties.Delete');
        if (!hasPermission) {
            return '';
        }
        return `<button type="button" class="btn btn-danger btn-sm" title="${context.l('Delete')}">
            ${context.l('Delete')}
        </button>`;
    }
    close(): void {
        this.modal.hide();
    }
}
