import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UserDelegationServiceProxy, UserDelegationDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { CreateNewUserDelegationModalComponent } from './create-new-user-delegation-modal.component';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent, SortChangedEvent } from 'ag-grid-enterprise';
import { DateTime } from 'luxon';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'userDelegationsModal',
    templateUrl: './user-delegations-modal.component.html',
    imports: [AppBsModalDirective, BusyIfDirective, AgGridAngular, CreateNewUserDelegationModalComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class UserDelegationsModalComponent extends AppComponentBase {
    private _userDelegationService = inject(UserDelegationServiceProxy);
    @ViewChild('userDelegationsModal', { static: true }) modal: ModalDirective;
    @ViewChild('createNewUserDelegation', { static: true })
    createNewUserDelegation: CreateNewUserDelegationModalComponent;
    @Output() modalClose: EventEmitter<any> = new EventEmitter<any>();
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('UserName'),
            field: 'username',
            width: 150,
            sortable: true,
        },
        {
            headerName: this.l('StartTime'),
            field: 'startTime',
            width: 150,
            sortable: true,
            valueFormatter: (params) => (params.value ? DateTime.fromISO(params.value).toFormat('F') : ''),
        },
        {
            headerName: this.l('EndTime'),
            field: 'endTime',
            width: 150,
            sortable: true,
            valueFormatter: (params) => (params.value ? DateTime.fromISO(params.value).toFormat('F') : ''),
        },
        {
            headerName: '',
            field: 'delete',
            width: 80,
            cellRenderer: (params) => {
                const button = document.createElement('button');
                button.className = 'btn btn-sm btn-outline-danger btn-sm btn-icon';
                button.innerHTML = `<i class="fa fa-trash" aria-label="${this.l('Delete')}"></i>`;
                button.onclick = () => this.deleteUserDelegation(params.data);
                return button;
            },
            sortable: false,
            filter: false,
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        sortable: true,
        filter: true,
    };
    rowData: UserDelegationDto[] = [];
    totalRecordsCount = 0;
    isLoading = false;

    getUserDelegations() {
        this.isLoading = true;
        const sortModel = this.gridApi
            .getColumnState()
            .filter((col) => col.sort)
            .map((col) => ({
                field: col.colId,
                order: col.sort,
            }));
        const sorting = sortModel.length > 0 ? sortModel.map((s) => `${s.field} ${s.order}`).join(', ') : '';
        this._userDelegationService
            .getDelegatedUsers(
                10, // default page size
                0, // skip count
                sorting,
            )
            .pipe(finalize(() => (this.isLoading = false)))
            .subscribe((result) => {
                this.totalRecordsCount = result.totalCount;
                this.rowData = result.items;
            });
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
    }
    onSortChanged(event: SortChangedEvent) {
        this.getUserDelegations();
    }
    deleteUserDelegation(userDelegation: UserDelegationDto): void {
        this.message.confirm(
            this.l('UserDelegationDeleteWarningMessage', userDelegation.username),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._userDelegationService.removeDelegation(userDelegation.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            },
        );
    }
    reloadPage(): void {
        this.getUserDelegations();
    }
    manageUserDelegations(): void {
        this.createNewUserDelegation.show();
    }
    show(): void {
        this.modal.show();
    }
    onShown(): void {
        this.getUserDelegations();
    }
    close(): void {
        this.modal.hide();
        this.modalClose.emit(null);
    }
}
