import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    FindOrganizationUnitRolesInput,
    NameValueDto,
    OrganizationUnitServiceProxy,
    RolesToOrganizationUnitInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { IRolesWithOrganizationUnit } from './roles-with-organization-unit';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent, RowSelectedEvent } from 'ag-grid-enterprise';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../shared/utils/auto-focus.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'addRoleModal',
    templateUrl: './add-role-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        AutoFocusDirective,
        BusyIfDirective,
        AgGridAngular,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class AddRoleModalComponent extends AppComponentBase {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    @Output() rolesAdded: EventEmitter<IRolesWithOrganizationUnit> = new EventEmitter<IRolesWithOrganizationUnit>();
    @ViewChild('modal', { static: true }) modal: ModalDirective;
    organizationUnitId: number;
    isShown = false;
    filterText = '';
    tenantId?: number;
    saving = false;
    selectedRoles: NameValueDto[] = [];
    private gridApi!: GridApi;
    public rowData: NameValueDto[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: this.l('Name'),
            field: 'name',
            sortable: true,
            filter: true,
            checkboxSelection: true,
            headerCheckboxSelection: true,
        },
    ];

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.getRecords();
    }
    onSelectionChanged(_event: RowSelectedEvent): void {
        this.selectedRoles = this.gridApi.getSelectedRows();
        this._cdr.markForCheck();
    }
    show(): void {
        this.modal.show();
        this._cdr.markForCheck();
    }
    refreshTable(): void {
        this.getRecords();
    }
    close(): void {
        this.modal.hide();
        this._cdr.markForCheck();
    }
    shown(): void {
        this.isShown = true;
        this.getRecords();
        this._cdr.markForCheck();
    }
    getRecords(): void {
        if (!this.isShown) {
            return;
        }
        this.showMainSpinner();
        const input = new FindOrganizationUnitRolesInput();
        input.organizationUnitId = this.organizationUnitId;
        input.filter = this.filterText;
        input.skipCount = 0;
        input.maxResultCount = 1000; // Load all for AG Grid client-side filtering
        this._organizationUnitService
            .findRoles(input)
            .pipe(
                finalize(() => {
                    this.hideMainSpinner();
                    this._cdr.markForCheck();
                }),
            )
            .subscribe((result) => {
                this.rowData = result.items;
                this.hideMainSpinner();
                this._cdr.markForCheck();
            });
    }
    addRolesToOrganizationUnit(): void {
        const input = new RolesToOrganizationUnitInput();
        input.organizationUnitId = this.organizationUnitId;
        input.roleIds = this.selectedRoles.map((selectedRole) => Number(selectedRole.value));
        this.saving = true;
        this._organizationUnitService.addRolesToOrganizationUnit(input).subscribe(() => {
            this.notify.success(this.l('SuccessfullyAdded'));
            this.rolesAdded.emit({
                roleIds: input.roleIds,
                ouId: input.organizationUnitId,
            });
            this.saving = false;
            this.close();
            this.selectedRoles = [];
            this._cdr.markForCheck();
        });
    }
}
