import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    NgZone,
    OnInit,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AddRoleModalComponent } from '@app/admin/organization-units/add-role-modal.component';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrganizationUnitRoleListDto, OrganizationUnitServiceProxy } from '@shared/service-proxies/service-proxies';
import { IBasicOrganizationUnitInfo } from './basic-organization-unit-info';
import { IRoleWithOrganizationUnit } from './role-with-organization-unit';
import { IRolesWithOrganizationUnit } from './roles-with-organization-unit';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent, SortChangedEvent } from 'ag-grid-enterprise';
import { DateTime } from 'luxon';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { AddRoleModalComponent as AddRoleModalComponent_1 } from './add-role-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    selector: 'organization-unit-roles',
    templateUrl: './organization-unit-roles.component.html',
    imports: [BusyIfDirective, AgGridAngular, AddRoleModalComponent_1, LocalizePipe, PermissionPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class OrganizationUnitRolesComponent extends AppComponentBase implements OnInit {
    private _changeDetector = inject(ChangeDetectorRef);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _zone = inject(NgZone);
    @Output() roleRemoved = new EventEmitter<IRoleWithOrganizationUnit>();
    @Output() rolesAdded = new EventEmitter<IRolesWithOrganizationUnit>();
    @ViewChild('addRoleModal', { static: true }) addRoleModal: AddRoleModalComponent;
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Delete'),
            field: 'delete',
            width: 80,
            hide: !this.permission.isGranted('Pages.Administration.OrganizationUnits.ManageRoles'),
            cellRenderer: (params) => {
                const button = document.createElement('button');
                button.className = 'btn btn-icon btn-bg-light btn-active-color-danger btn-sm';
                button.title = this.l('Delete');
                button.innerHTML = `<i class="fa fa-times" aria-label="${this.l('Delete')}"></i>`;
                button.onclick = () => this._zone.run(() => this.removeRole(params.data));
                return button;
            },
            sortable: false,
            filter: false,
        },
        {
            headerName: this.l('Role'),
            field: 'displayName',
            width: 200,
            sortable: true,
        },
        {
            headerName: this.l('AddedTime'),
            field: 'addedTime',
            width: 150,
            sortable: true,
            valueFormatter: (params) => (params.value ? DateTime.fromISO(params.value).toFormat('F') : ''),
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        sortable: true,
        filter: true,
    };
    rowData: OrganizationUnitRoleListDto[] = [];
    totalRecordsCount = 0;
    isLoading = false;

    private _organizationUnit: IBasicOrganizationUnitInfo = null;
    get organizationUnit(): IBasicOrganizationUnitInfo {
        return this._organizationUnit;
    }
    set organizationUnit(ou: IBasicOrganizationUnitInfo) {
        if (!ou) {
            this._organizationUnit = null;
            this.rowData = [];
            this.totalRecordsCount = 0;
            this._changeDetector.markForCheck();
            return;
        }
        if (this._organizationUnit === ou) {
            return;
        }
        this._organizationUnit = ou;
        this.addRoleModal.organizationUnitId = ou.id;
        this._changeDetector.markForCheck();
        if (ou) {
            this.refreshRoles();
        }
    }
    ngOnInit(): void {}
    getOrganizationUnitRoles() {
        if (!this._organizationUnit) {
            return;
        }
        this.isLoading = true;
        this._changeDetector.markForCheck();
        const sortModel = this.gridApi
            .getColumnState()
            .filter((col) => col.sort)
            .map((col) => ({
                field: col.colId,
                order: col.sort,
            }));
        const sorting = sortModel.length > 0 ? sortModel.map((s) => `${s.field} ${s.order}`).join(', ') : '';
        this._organizationUnitService
            .getOrganizationUnitRoles(
                this._organizationUnit.id,
                sorting,
                10, // default page size
                0, // skip count
            )
            .pipe(
                finalize(() => {
                    this.isLoading = false;
                    this._changeDetector.markForCheck();
                }),
            )
            .subscribe((result) => {
                this.totalRecordsCount = result.totalCount;
                this.rowData = result.items;
                this._changeDetector.markForCheck();
            });
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.getOrganizationUnitRoles();
    }
    onSortChanged(_event: SortChangedEvent) {
        this.getOrganizationUnitRoles();
    }
    reloadPage(): void {
        this.getOrganizationUnitRoles();
    }
    refreshRoles(): void {
        this.reloadPage();
    }
    openAddRoleModal(): void {
        this.addRoleModal.show();
    }
    removeRole(role: OrganizationUnitRoleListDto): void {
        this.message.confirm(
            this.l('RemoveRoleFromOuWarningMessage', role.displayName, this.organizationUnit.displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._organizationUnitService
                        .removeRoleFromOrganizationUnit(role.id, this.organizationUnit.id)
                        .subscribe(() => {
                            this.notify.success(this.l('SuccessfullyRemoved'));
                            this.roleRemoved.emit({
                                roleId: role.id,
                                ouId: this.organizationUnit.id,
                            });
                            this.refreshRoles();
                        });
                }
            },
        );
    }
    addRoles(data: any): void {
        this.rolesAdded.emit({
            roleIds: data.roleIds,
            ouId: data.ouId,
        });
        this.refreshRoles();
    }
}
