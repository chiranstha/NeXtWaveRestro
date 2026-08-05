import {
    Component,
    EventEmitter,
    Output,
    ViewChild,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/common/app-component-base';
import { MassNotificationUserLookupTableDto, NotificationServiceProxy } from '@shared/service-proxies/service-proxies';
import { ColDef, GridApi, GridReadyEvent, RowSelectedEvent } from 'ag-grid-enterprise';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../shared/utils/auto-focus.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'user-lookup-table-modal',
    templateUrl: './user-lookup-table-modal.component.html',
    encapsulation: ViewEncapsulation.None,
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
export class UserLookupTableModalComponent extends AppComponentBase {
    private _notificationServiceProxy = inject(NotificationServiceProxy);
    @ViewChild('userLookupTableModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<MassNotificationUserLookupTableDto[]> = new EventEmitter<
        MassNotificationUserLookupTableDto[]
    >();
    filterText = '';
    active = false;
    saving = false;
    selectedUsers: MassNotificationUserLookupTableDto[] = [];
    private gridApi!: GridApi;
    public rowData: MassNotificationUserLookupTableDto[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: this.l('Name'),
            field: 'displayName',
            sortable: true,
            filter: true,
            checkboxSelection: true,
            headerCheckboxSelection: true,
        },
    ];

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.getAll();
    }
    onSelectionChanged(_event: RowSelectedEvent): void {
        this.selectedUsers = this.gridApi.getSelectedRows();
    }
    show(): void {
        this.active = true;
        this.getAll();
        this.modal.show();
    }
    getAll() {
        if (!this.active) {
            return;
        }
        this.showMainSpinner();
        this._notificationServiceProxy
            .getAllUserForLookupTable(
                this.filterText,
                '', // No sorting for AG Grid
                0,
                1000, // Load all for client-side filtering
            )
            .subscribe((result) => {
                this.rowData = result.items;
                this.hideMainSpinner();
            });
    }
    save() {
        this.active = false;
        this.modal.hide();
        this.modalSave.emit(this.selectedUsers);
        this.selectedUsers = [];
    }
    close(): void {
        this.active = false;
        this.modal.hide();
        this.selectedUsers = [];
    }
}
