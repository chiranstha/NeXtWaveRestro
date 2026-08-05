import { Component, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DynamicEntityPropertyServiceProxy } from '@shared/service-proxies/service-proxies';
import { SelectAnEntityModalComponent } from '@app/admin/dynamic-properties/select-an-entity-modal.component';
import { Router } from '@angular/router';
import { ManageDynamicEntityPropertyModalComponent } from './manage-dynamic-entity-property-modal.component';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { AgGridAngular } from 'ag-grid-angular';
import { SelectAnEntityModalComponent as SelectAnEntityModalComponent_1 } from '../select-an-entity-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    selector: 'dynamic-entity-property-list',
    templateUrl: './dynamic-entity-property-list.component.html',
    imports: [
        AgGridAngular,
        SelectAnEntityModalComponent_1,
        ManageDynamicEntityPropertyModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DynamicEntityPropertyListComponent extends AppComponentBase implements OnInit {
    private _dynamicEntityPropertyService = inject(DynamicEntityPropertyServiceProxy);
    private _router = inject(Router);
    @ViewChild('selectAnEntityModal') selectAnEntityModal: SelectAnEntityModalComponent;
    @ViewChild('manageDynamicEntityPropertyModalComponent')
    manageDynamicEntityPropertyModalComponent: ManageDynamicEntityPropertyModalComponent;
    private gridApi!: GridApi;
    public rowData: any[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: '',
            field: 'actions',
            width: 100,
            cellRenderer: this.actionsCellRenderer.bind(this),
            sortable: false,
            filter: false,
            resizable: false,
        },
        {
            headerName: this.l('EntityFullName'),
            field: 'entityFullName',
            sortable: true,
            filter: true,
        },
    ];

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        params.api.setGridOption('context', { componentParent: this });
        this.getDynamicEntityProperties();
    }
    onCellClicked(event: any): void {
        if (event.colDef.field === 'actions' && event.event.target.tagName === 'BUTTON') {
            const { entityFullName } = event.data;
            this.gotoEdit(entityFullName);
        }
    }
    ngOnInit() {
        // Moved to onGridReady
    }
    getDynamicEntityProperties(): void {
        this.showMainSpinner();
        this._dynamicEntityPropertyService.getAllEntitiesHasDynamicProperty().subscribe(
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
        this.selectAnEntityModal.show();
    }
    gotoEdit(entityFullName: string): void {
        this.manageDynamicEntityPropertyModalComponent.show(entityFullName);
    }
    private actionsCellRenderer(params: any): string {
        const { entityFullName } = params.data;
        const context = params.context.componentParent;
        const hasPermission = context.permission.isGranted('Pages.Administration.DynamicEntityProperties.Edit');
        if (!hasPermission) {
            return '';
        }
        return `<button type="button" class="btn btn-sm btn-primary" title="${context.l('Detail')}" data-entity="${entityFullName}">
            ${context.l('Detail')}
        </button>`;
    }
}
