import {
    ChangeDetectorRef,
    Component,
    NgZone,
    OnInit,
    ViewChild,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDtoOfInt64,
    GetUsersInput,
    UserListDto,
    UserServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { CreateOrEditUserModalComponent } from './create-or-edit-user-modal.component';
import { EditUserPermissionsModalComponent } from './edit-user-permissions-modal.component';
import { ImpersonationService } from './impersonation.service';
import { HttpClient } from '@angular/common/http';
import { FileUpload } from '@shared/ui-compat';
import { finalize } from 'rxjs/operators';
import { PermissionTreeModalComponent } from '../shared/permission-tree-modal.component';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import { DynamicEntityPropertyManagerComponent } from '@app/shared/common/dynamic-entity-property-manager/dynamic-entity-property-manager.component';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { ExcelColumnSelectionModalComponent } from '@app/shared/common/excel-column-selection/excel-column-selection-modal';
import { ColDef, GridApi } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { BsDropdownDirective, BsDropdownToggleDirective, BsDropdownMenuDirective } from 'ngx-bootstrap/dropdown';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../shared/utils/auto-focus.directive';
import { NgClass } from '@angular/common';
import { RoleComboComponent } from '../shared/role-combo.component';
import { AgGridAngular } from 'ag-grid-angular';
import { PaginationComponent } from '../shared/pagination.component';
import { DynamicEntityPropertyManagerComponent as DynamicEntityPropertyManagerComponent_1 } from '../../shared/common/dynamic-entity-property-manager/dynamic-entity-property-manager.component';
import { ExcelColumnSelectionModalComponent as ExcelColumnSelectionModalComponent_1 } from '../../shared/common/excel-column-selection/excel-column-selection-modal';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './users.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./users.component.less'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        BsDropdownDirective,
        BsDropdownToggleDirective,
        BsDropdownMenuDirective,
        FileUpload,
        FormsModule,
        AutoFocusDirective,
        NgClass,
        PermissionTreeModalComponent,
        RoleComboComponent,
        AgGridAngular,
        PaginationComponent,
        CreateOrEditUserModalComponent,
        EditUserPermissionsModalComponent,
        DynamicEntityPropertyManagerComponent_1,
        ExcelColumnSelectionModalComponent_1,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class UsersComponent extends AppComponentBase implements OnInit {
    _impersonationService = inject(ImpersonationService);
    private _userServiceProxy = inject(UserServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);
    private _activatedRoute = inject(ActivatedRoute);
    private _httpClient = inject(HttpClient);
    private _localStorageService = inject(LocalStorageService);
    private _dateTimeService = inject(DateTimeService);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @ViewChild('createOrEditUserModal', { static: true }) createOrEditUserModal: CreateOrEditUserModalComponent;
    @ViewChild('editUserPermissionsModal', { static: true })
    editUserPermissionsModal: EditUserPermissionsModalComponent;
    @ViewChild('ExcelFileUpload', { static: false }) excelFileUpload: FileUpload;
    @ViewChild('permissionFilterTreeModal', { static: true }) permissionFilterTreeModal: PermissionTreeModalComponent;
    @ViewChild('dynamicEntityPropertyManager', { static: true })
    dynamicEntityPropertyManager: DynamicEntityPropertyManagerComponent;
    @ViewChild('excelColumnSelectionModal', { static: true })
    excelColumnSelectionModal: ExcelColumnSelectionModalComponent;
    uploadUrl: string;
    //Filters
    advancedFiltersAreShown = false;
    filterText = '';
    role = '';
    onlyLockedUsers = false;
    private gridApi!: GridApi;
    constructor() {
        super();
        this.filterText = this._activatedRoute.snapshot.queryParams['filterText'] || '';
        this.uploadUrl = `${AppConsts.remoteServiceBaseUrl}/Users/ImportFromExcel`;
    }
    //ag grid
    public rowData: UserListDto[] = [];
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        minWidth: 200,
    };
    onPageChange(page: number): void {
        this.currentPage = page;
        this.loadPage(page);
    }
    public columnDefs: ColDef[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => params.node.rowIndex + 1,
            width: 80,
        },
        {
            field: 'userName',
            headerName: this.l('UserName'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'name',
            headerName: this.l('FirstName'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'surname',
            headerName: this.l('Surname'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'roles',
            headerName: this.l('Roles'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            width: 150,
            cellRenderer: (params) => {
                return this.getRolesAsString(params.value);
            },
        },
        {
            field: 'emailAddress',
            headerName: this.l('EmailAddress'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'isEmailConfirmed',
            headerName: this.l('EmailConfirm'),
            sortable: true,
            filter: true,
            width: 150,
            cellRenderer: (params) => {
                return params.data.isEmailConfirmed
                    ? '<i class="fas fa-check-circle" style="color: green;">'
                    : '<i class="fas fa-times-circle" style="color: red;">';
            },
        },
        {
            field: 'isActive',
            headerName: this.l('Active'),
            sortable: true,
            filter: true,
            width: 150,
            cellRenderer: (params) => {
                return params.data.isActive
                    ? '<i class="fas fa-check-circle" style="color: green;">'
                    : '<i class="fas fa-times-circle" style="color: red;">';
            },
        },
        {
            field: 'creationTime',
            headerName: this.l('CreationTime'),
            sortable: true,
            filter: true,
            width: 150,
            cellRenderer: (params) => {
                return this._dateTimeService.formatDate(params.value, 'F');
            },
        },
    ];
    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 100, // Set a minimum width for each column
        maxWidth: 300, // Set a maximum width for each column
    };
    loadPage(_page: number) {
        this._userServiceProxy
            .getUsers(
                new GetUsersInput({
                    filter: this.filterText,
                    permissions: this.permissionFilterTreeModal.getSelectedPermissions(),
                    role: this.role !== '' ? parseInt(this.role) : undefined,
                    onlyLockedUsers: this.onlyLockedUsers,
                    sorting: '',
                    maxResultCount: this.pageSize,
                    skipCount: this.currentPage,
                }),
            )
            .subscribe((data) => {
                this.rowData = data.items;
                this.totalRecords = data.totalCount;
                this.calculatePageSizeOptions();
                if (this.gridApi) {
                    this.gridApi.setGridOption('rowData', this.rowData);
                }
                this._cdr.markForCheck();
            });
    }
    onPageSizeChange(newPageSize: number): void {
        this.pageSize = +newPageSize;
        this.currentPage = 0; // Reset to first page when page size changes
        this.loadPage(this.currentPage);
    }

    getCustomContextMenuItems = (params) => {
        const MenuItem = [];
        //permissions
        if (
            this.isGranted('Pages.Administration.Users.Impersonation') &&
            params.node.data.id !== this.appSession.userId
        ) {
            MenuItem.push({
                name: this.l('LoginAsThisUser'),
                disabled: false,
                action: () => {
                    this._zone.run(() =>
                        this._impersonationService.impersonateUser(params.node.data.id, this.appSession.tenantId),
                    );
                },
            });
        }
        if (this.isGranted('Pages.Administration.Users.Edit')) {
            MenuItem.push({
                name: this.l('Edit'),
                disabled: false,
                action: () => {
                    this._zone.run(() => this.createOrEditUserModal.show(params.node.data.id));
                },
            });
        }
        if (this.isGranted('Pages.Administration.Users.ChangePermissions')) {
            MenuItem.push({
                name: this.l('Permissions'),
                disabled: false,
                action: () => {
                    this._zone.run(() =>
                        this.editUserPermissionsModal.show(params.node.data.id, params.node.data.userName),
                    );
                },
            });
        }
        if (params.node.data.lockoutEndDateUtc && this.isGranted('Pages.Administration.Users.Unlock')) {
            MenuItem.push({
                name: this.l('Unlock'),
                disabled: false,
                action: () => {
                    this._zone.run(() => this.unlockUser(params.node.data));
                },
            });
        }
        if (this.dynamicEntityPropertyManager.canShow('Erp.Authorization.Users.User')) {
            MenuItem.push({
                name: this.l('DynamicProperties'),
                disabled: false,
                action: () => {
                    this._zone.run(() => this.showDynamicProperties(params.node.data));
                },
            });
        }
        if (this.isGranted('Pages.Administration.Users.Delete')) {
            MenuItem.push({
                name: this.l('Delete'),
                disabled: false,
                action: () => {
                    this._zone.run(() => this.deleteUser(params.node.data));
                },
            });
        }
        return MenuItem;
    };
    //end ag grid
    ngOnInit(): void {
        super.ngOnInit();
        this.loadPage(this.currentPage);
    }
    unlockUser(record): void {
        this._userServiceProxy.unlockUser(new EntityDtoOfInt64({ id: record.id })).subscribe(() => {
            this.notify.success(this.l('UnlockedTheUser', record.userName));
            this.loadPage(this.currentPage);
        });
    }
    getRolesAsString(roles): string {
        let roleNames = '';
        for (let j = 0; j < roles.length; j++) {
            if (roleNames.length) {
                roleNames = `${roleNames}, `;
            }
            roleNames = roleNames + roles[j].roleName;
        }
        return roleNames;
    }
    exportToExcel($event): void {
        this._userServiceProxy
            .getUsersToExcel(
                this.filterText,
                this.permissionFilterTreeModal.getSelectedPermissions(),
                $event,
                this.role !== '' ? parseInt(this.role) : undefined,
                this.onlyLockedUsers,
                '',
            )
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }
    createUser(): void {
        this.createOrEditUserModal.show();
    }
    uploadExcel(data: { files: File }): void {
        const formData: FormData = new FormData();
        const file = data.files[0];
        formData.append('file', file, file.name);
        this._httpClient
            .post<any>(this.uploadUrl, formData)
            .pipe(
                finalize(() => {
                    this.excelFileUpload.clear();
                    this._cdr.markForCheck();
                }),
            )
            .subscribe((response) => {
                if (response.success) {
                    this.notify.success(this.l('ImportUsersProcessStart'));
                } else if (response.error != null) {
                    this.notify.error(this.l('ImportUsersUploadFailed'));
                }
            });
    }
    onUploadExcelError(): void {
        this.notify.error(this.l('ImportUsersUploadFailed'));
    }
    deleteUser(user: UserListDto): void {
        if (user.userName === AppConsts.userManagement.defaultAdminUserName) {
            this.message.warn(this.l('{0}UserCannotBeDeleted', AppConsts.userManagement.defaultAdminUserName));
            return;
        }
        this.message.confirm(this.l('UserDeleteWarningMessage', user.userName), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._userServiceProxy.deleteUser(user.id).subscribe(() => {
                    this.loadPage(this.currentPage);
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
    showDynamicProperties(user: UserListDto): void {
        this.dynamicEntityPropertyManager.getModal().show('Erp.Authorization.Users.User', user.id.toString());
    }
    setUsersProfilePictureUrl(users: UserListDto[]): void {
        for (let i = 0; i < users.length; i++) {
            const user = users[i];
            this._localStorageService.getItem(AppConsts.authorization.encrptedAuthTokenName, (err, value) => {
                const profilePictureUrl = `${AppConsts.remoteServiceBaseUrl}/Profile/GetProfilePictureByUser?userId=${
                    user.id
                }&${AppConsts.authorization.encrptedAuthTokenName}=${encodeURIComponent(value.token)}`;
                (user as any).profilePictureUrl = profilePictureUrl;
                this._cdr.markForCheck();
            });
        }
    }
    isUserLocked(user: UserListDto): boolean {
        if (!user.lockoutEndDateUtc) {
            return false;
        }
        const lockoutEndDateUtc = this._dateTimeService.changeDateTimeZone(user.lockoutEndDateUtc, 'UTC');
        return lockoutEndDateUtc > this._dateTimeService.getUTCDate();
    }
    showExcelColumnsSelectionModal(): void {
        this._userServiceProxy.getUserExcelColumnsToExcel().subscribe((result) => {
            this.excelColumnSelectionModal.show(result);
            this._cdr.markForCheck();
        });
    }
}
