import { AbpMultiTenancyService } from 'abp-ng2-module';
import {
    Component,
    EventEmitter,
    Output,
    ViewChild,
    inject,
    ChangeDetectorRef,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { LinkedAccountService } from '@app/shared/layout/linked-account.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LinkedUserDto, UnlinkUserInput, UserLinkServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { LinkAccountModalComponent } from './link-account-modal.component';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent, SortChangedEvent } from 'ag-grid-enterprise';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'linkedAccountsModal',
    templateUrl: './linked-accounts-modal.component.html',
    imports: [AppBsModalDirective, BusyIfDirective, AgGridAngular, LinkAccountModalComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class LinkedAccountsModalComponent extends AppComponentBase {
    private abpMultiTenancyService = inject(AbpMultiTenancyService);
    private _userLinkService = inject(UserLinkServiceProxy);
    private _linkedAccountService = inject(LinkedAccountService);
    private cdr = inject(ChangeDetectorRef);
    @ViewChild('linkedAccountsModal', { static: true }) modal: ModalDirective;
    @ViewChild('linkAccountModal', { static: true }) linkAccountModal: LinkAccountModalComponent;
    @Output() modalClose: EventEmitter<any> = new EventEmitter<any>();
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Actions'),
            field: 'actions',
            width: 100,
            cellRenderer: (params) => {
                const button = document.createElement('button');
                button.className = 'btn btn-sm btn-primary';
                button.innerHTML = `<i class="fa fa-sign-in-alt"></i> ${this.l('LogIn')}`;
                button.onclick = () => this.switchToUser(params.data);
                return button;
            },
            sortable: false,
            filter: false,
        },
        {
            headerName: this.l('UserName'),
            field: 'userName',
            width: 300,
            valueGetter: (params) => this.getShownLinkedUserName(params.data),
            sortable: true,
        },
        {
            headerName: this.l('Delete'),
            field: 'delete',
            width: 80,
            cellRenderer: (params) => {
                const button = document.createElement('button');
                button.className = 'btn btn-sm btn-outline-danger btn-sm btn-icon';
                button.innerHTML = `<i class="fa fa-trash" aria-label="${this.l('Delete')}"></i>`;
                button.onclick = () => this.deleteLinkedUser(params.data);
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
    rowData: LinkedUserDto[] = [];
    totalRecordsCount = 0;
    isLoading = false;

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.getLinkedUsers();
    }
    onSortChanged(event: SortChangedEvent) {
        this.getLinkedUsers();
    }
    getLinkedUsers() {
        this.isLoading = true;
        this.cdr.markForCheck();
        const sortModel = this.gridApi
            .getColumnState()
            .filter((col) => col.sort)
            .map((col) => ({
                field: col.colId,
                order: col.sort,
            }));
        const sorting = sortModel.length > 0 ? sortModel.map((s) => `${s.field} ${s.order}`).join(', ') : '';
        this._userLinkService
            .getLinkedUsers(
                10, // default page size
                0, // skip count, for simplicity assuming no pagination or handle separately
                sorting,
            )
            .pipe(
                finalize(() => {
                    this.isLoading = false;
                    this.cdr.markForCheck();
                }),
            )
            .subscribe((result) => {
                this.totalRecordsCount = result.totalCount;
                this.rowData = result.items;
            });
    }
    getShownLinkedUserName(linkedUser: LinkedUserDto): string {
        if (!this.abpMultiTenancyService.isEnabled) {
            return linkedUser.username;
        }
        return `${linkedUser.tenantId ? linkedUser.tenancyName : '.'}\\${linkedUser.username}`;
    }
    deleteLinkedUser(linkedUser: LinkedUserDto): void {
        this.message.confirm(
            this.l('LinkedUserDeleteWarningMessage', linkedUser.username),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    const unlinkUserInput = new UnlinkUserInput();
                    unlinkUserInput.userId = linkedUser.id;
                    unlinkUserInput.tenantId = linkedUser.tenantId;
                    this._userLinkService.unlinkUser(unlinkUserInput).subscribe(() => {
                        this.getLinkedUsers();
                        this.notify.success(this.l('SuccessfullyUnlinked'));
                    });
                }
            },
        );
    }
    reloadPage(): void {
        this.getLinkedUsers();
    }
    manageLinkedAccounts(): void {
        this.linkAccountModal.show();
    }
    switchToUser(linkedUser: LinkedUserDto): void {
        this._linkedAccountService.switchToAccount(linkedUser.id, linkedUser.tenantId);
    }
    show(): void {
        this.modal.show();
    }
    close(): void {
        this.modal.hide();
        this.modalClose.emit(null);
    }
}
