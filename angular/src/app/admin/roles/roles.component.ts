import {
    ChangeDetectorRef,
    Component,
    NgZone,
    ViewChild,
    OnInit,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { RoleListDto, RoleServiceProxy, GetRolesInput } from '@shared/service-proxies/service-proxies';
import { CreateOrEditRoleModalComponent } from './create-or-edit-role-modal.component';
import { finalize } from 'rxjs/operators';
import { PermissionTreeModalComponent } from '../shared/permission-tree-modal.component';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent, ICellRendererParams } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { EntityTypeHistoryModalComponent } from '../../shared/common/entityHistory/entity-type-history-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './roles.component.html',
    styleUrls: ['./roles.component.less'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        PermissionTreeModalComponent,
        BusyIfDirective,
        AgGridAngular,
        CreateOrEditRoleModalComponent,
        EntityTypeHistoryModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class RolesComponent extends AppComponentBase implements OnInit {
    private _roleService = inject(RoleServiceProxy);
    private _router = inject(Router);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @ViewChild('createOrEditRoleModal', { static: true }) createOrEditRoleModal: CreateOrEditRoleModalComponent;
    @ViewChild('dataGrid', { static: true }) dataGrid: AgGridAngular;
    @ViewChild('permissionFilterTreeModal', { static: true }) permissionFilterTreeModal: PermissionTreeModalComponent;
    _entityTypeFullName = 'Erp.Authorization.Roles.Role';
    entityHistoryEnabled = false;
    isLoading = false;
    // AG Grid properties
    rowSelection = { mode: 'multiRow' as const, enableClickSelection: false };
    rowData: RoleListDto[] = [];
    columnDefs: ColDef[] = [];
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
    };
    private gridApi: GridApi;
    constructor() {
        super();
        // Expose component instance to global scope for dropdown actions
        (window as any).rolesComponent = this;
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.setIsEntityHistoryEnabled();
        this.initializeColumnDefs();
        this.getRoles();
    }
    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
    }
    private initializeColumnDefs(): void {
        this.columnDefs = [
            {
                headerName: this.l('Actions'),
                field: 'actions',
                cellRenderer: (params: ICellRendererParams) => {
                    const role = params.data;
                    let actions = '';
                    if (this.isGranted('Pages.Administration.Roles.Edit')) {
                        actions += `<i class="fa fa-edit text-primary me-2" style="cursor: pointer;" onclick="window.rolesComponent.editRole(${role.id})" title="${this.l('Edit')}"></i>`;
                    }
                    if (!role.isStatic && this.isGranted('Pages.Administration.Roles.Delete')) {
                        actions += `<i class="fa fa-trash text-danger me-2" style="cursor: pointer;" onclick="window.rolesComponent.deleteRoleFromGrid('${role.id}', '${role.displayName}')" title="${this.l('Delete')}"></i>`;
                    }
                    if (this.entityHistoryEnabled) {
                        actions += `<i class="fa fa-history text-info" style="cursor: pointer;" onclick="window.rolesComponent.showHistoryFromGrid('${role.id}')" title="${this.l('History')}"></i>`;
                    }
                    return actions;
                },
                width: 100,
                sortable: false,
                filter: false,
                hide: !(
                    this.isGrantedAny('Pages.Administration.Roles.Edit', 'Pages.Administration.Roles.Delete') ||
                    this.entityHistoryEnabled
                ),
            },
            {
                headerName: this.l('RoleName'),
                field: 'displayName',
                cellRenderer: (params: ICellRendererParams) => this.createRoleNameTemplate(params.data),
                minWidth: 200,
            },
            {
                headerName: this.l('CreationTime'),
                field: 'creationTime',
                cellRenderer: (params: ICellRendererParams) => {
                    if (params.value) {
                        const date = new Date(params.value);
                        return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
                    }
                    return '';
                },
                width: 180,
            },
        ];
        this._cdr.markForCheck();
    }
    private createRoleNameTemplate(role: RoleListDto): string {
        let template = role.displayName;
        if (role.isStatic) {
            template += ` <span class="badge badge-primary ms-1" title="${this.l('StaticRole_Tooltip')}">${this.l('Static')}</span>`;
        }
        if (role.isDefault) {
            template += ` <span class="badge badge-dark ms-1" title="${this.l('DefaultRole_Description')}">${this.l('Default')}</span>`;
        }
        return template;
    }
    getRoles(): void {
        this.isLoading = true;
        this._cdr.markForCheck();
        const selectedPermissions = this.permissionFilterTreeModal.getSelectedPermissions();
        this._roleService
            .getRoles(new GetRolesInput({ permissions: selectedPermissions }))
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((result) => {
                this.rowData = result.items;
                this._cdr.markForCheck();
            });
    }
    createRole(): void {
        this.createOrEditRoleModal.show();
    }
    editRole(roleId: number): void {
        this._zone.run(() => this.createOrEditRoleModal.show(roleId));
    }
    deleteRoleFromGrid(roleId: string, _displayName: string): void {
        const role = this.rowData.find((r) => r.id.toString() === roleId);
        if (role) {
            this._zone.run(() => this.deleteRole(role));
        }
    }
    showHistoryFromGrid(roleId: string): void {
        const role = this.rowData.find((r) => r.id.toString() === roleId);
        if (role) {
            this._zone.run(() => this.showHistory(role));
        }
    }
    showHistory(role: RoleListDto): void {
        this._router.navigate([`${abp.appPath}app/admin/entity-changes/${role.id}/${this._entityTypeFullName}`]);
    }
    deleteRole(role: RoleListDto): void {
        const self = this;
        self.message.confirm(
            self.l('RoleDeleteWarningMessage', role.displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._roleService.deleteRole(role.id).subscribe(() => {
                        this.getRoles();
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            },
        );
    }
    private setIsEntityHistoryEnabled(): void {
        const customSettings = (abp as any).custom;
        this.entityHistoryEnabled =
            customSettings.EntityHistory?.isEnabled &&
            customSettings.EntityHistory.enabledEntities.filter((entityType) => entityType === this._entityTypeFullName)
                .length === 1;
        this._cdr.markForCheck();
    }
    private finishLoading(): void {
        setTimeout(() => {
            this.isLoading = false;
            this._cdr.markForCheck();
        });
    }
}
