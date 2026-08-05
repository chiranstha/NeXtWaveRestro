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
import { AddMemberModalComponent } from '@app/admin/organization-units/add-member-modal.component';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrganizationUnitServiceProxy, OrganizationUnitUserListDto } from '@shared/service-proxies/service-proxies';
import { IBasicOrganizationUnitInfo } from './basic-organization-unit-info';
import { IUserWithOrganizationUnit } from './user-with-organization-unit';
import { IUsersWithOrganizationUnit } from './users-with-organization-unit';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent, SortChangedEvent } from 'ag-grid-enterprise';
import { DateTime } from 'luxon';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { AddMemberModalComponent as AddMemberModalComponent_1 } from './add-member-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    selector: 'organization-unit-members',
    templateUrl: './organization-unit-members.component.html',
    imports: [BusyIfDirective, AgGridAngular, AddMemberModalComponent_1, LocalizePipe, PermissionPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class OrganizationUnitMembersComponent extends AppComponentBase implements OnInit {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @Output() memberRemoved = new EventEmitter<IUserWithOrganizationUnit>();
    @Output() membersAdded = new EventEmitter<IUsersWithOrganizationUnit>();
    @ViewChild('addMemberModal', { static: true }) addMemberModal: AddMemberModalComponent;
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Delete'),
            field: 'delete',
            width: 80,
            hide: !this.permission.isGranted('Pages.Administration.OrganizationUnits.ManageMembers'),
            cellRenderer: (params) => {
                const button = document.createElement('button');
                button.className = 'btn btn-icon btn-bg-light btn-active-color-danger btn-sm';
                button.title = this.l('Delete');
                button.innerHTML = `<i class="fa fa-times" aria-label="${this.l('Delete')}"></i>`;
                button.onclick = () => this._zone.run(() => this.removeMember(params.data));
                return button;
            },
            sortable: false,
            filter: false,
        },
        {
            headerName: this.l('UserName'),
            field: 'userName',
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
    rowData: OrganizationUnitUserListDto[] = [];
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
            this._cdr.markForCheck();
            return;
        }
        if (this._organizationUnit === ou) {
            return;
        }
        this._organizationUnit = ou;
        this.addMemberModal.organizationUnitId = ou.id;
        this._cdr.markForCheck();
        if (ou) {
            this.refreshMembers();
        }
    }
    ngOnInit(): void {}
    getOrganizationUnitUsers() {
        if (!this._organizationUnit) {
            return;
        }
        this.isLoading = true;
        this._cdr.markForCheck();
        const sortModel = this.gridApi
            .getColumnState()
            .filter((col) => col.sort)
            .map((col) => ({
                field: col.colId,
                order: col.sort,
            }));
        const sorting = sortModel.length > 0 ? sortModel.map((s) => `${s.field} ${s.order}`).join(', ') : '';
        this._organizationUnitService
            .getOrganizationUnitUsers(
                this._organizationUnit.id,
                sorting,
                10, // default page size
                0, // skip count
            )
            .pipe(
                finalize(() => {
                    this.isLoading = false;
                    this._cdr.markForCheck();
                }),
            )
            .subscribe((result) => {
                this.totalRecordsCount = result.totalCount;
                this.rowData = result.items;
                this._cdr.markForCheck();
            });
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.getOrganizationUnitUsers();
    }
    onSortChanged(_event: SortChangedEvent) {
        this.getOrganizationUnitUsers();
    }
    reloadPage(): void {
        this.getOrganizationUnitUsers();
    }
    refreshMembers(): void {
        this.reloadPage();
    }
    openAddMemberModal(): void {
        this.addMemberModal.show();
    }
    removeMember(user: OrganizationUnitUserListDto): void {
        this.message.confirm(
            this.l('RemoveUserFromOuWarningMessage', user.userName, this.organizationUnit.displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._organizationUnitService
                        .removeUserFromOrganizationUnit(user.id, this.organizationUnit.id)
                        .subscribe(() => {
                            this.notify.success(this.l('SuccessfullyRemoved'));
                            this.memberRemoved.emit({
                                userId: user.id,
                                ouId: this.organizationUnit.id,
                            });
                            this.refreshMembers();
                        });
                }
            },
        );
    }
    addMembers(data: any): void {
        this.membersAdded.emit({
            userIds: data.userIds,
            ouId: data.ouId,
        });
        this.refreshMembers();
    }
}
