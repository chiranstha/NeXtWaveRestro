import { ChangeDetectorRef, Component, NgZone, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EntityChangeDetailModalComponent } from './entity-change-detail-modal.component';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AuditLogServiceProxy, EntityChangeListDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent, SortChangedEvent } from 'ag-grid-enterprise';
import { DateTime } from 'luxon';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
export interface IEntityTypeHistoryModalOptions {
    entityTypeFullName: string;
    entityTypeDescription: string;
    entityId: string;
}
@Component({
    selector: 'entityTypeHistoryModal',
    templateUrl: './entity-type-history-modal.component.html',
    imports: [AppBsModalDirective, BusyIfDirective, AgGridAngular, EntityChangeDetailModalComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EntityTypeHistoryModalComponent extends AppComponentBase {
    private _auditLogService = inject(AuditLogServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @ViewChild('entityChangeDetailModal', { static: true }) entityChangeDetailModal: EntityChangeDetailModalComponent;
    @ViewChild('modal', { static: true }) modal: ModalDirective;
    options: IEntityTypeHistoryModalOptions;
    isShown = false;
    isInitialized = false;
    filterText = '';
    tenantId?: number;
    entityHistoryEnabled: false;
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Select'),
            field: 'select',
            width: 80,
            cellRenderer: (params) => {
                const button = document.createElement('button');
                button.className = 'btn btn-icon btn-bg-light btn-active-color-primary btn-sm';
                button.title = this.l('Select');
                button.innerHTML = `<i class="la la-chevron-circle-right" aria-label="${this.l('Select')}"></i>`;
                button.onclick = () => this._zone.run(() => this.showEntityChangeDetails(params.data));
                return button;
            },
            sortable: false,
            filter: false,
        },
        {
            headerName: this.l('Action'),
            field: 'changeTypeName',
            width: 150,
            sortable: true,
        },
        {
            headerName: this.l('UserName'),
            field: 'userName',
            width: 150,
            sortable: true,
        },
        {
            headerName: this.l('Time'),
            field: 'changeTime',
            width: 200,
            sortable: true,
            valueFormatter: (params) => (params.value ? DateTime.fromISO(params.value).toFormat('F') : ''),
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        sortable: true,
        filter: true,
    };
    rowData: EntityChangeListDto[] = [];
    totalRecordsCount = 0;
    isLoading = false;

    show(options: IEntityTypeHistoryModalOptions): void {
        this.options = options;
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
        this.getRecordsIfNeeds();
        this._cdr.markForCheck();
    }
    getRecordsIfNeeds(): void {
        if (!this.isShown) {
            return;
        }
        this.getRecords();
        this.isInitialized = true;
    }
    getRecords(): void {
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
        this._auditLogService
            .getEntityTypeChanges(
                this.options.entityTypeFullName,
                this.options.entityId,
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
    }
    onSortChanged(event: SortChangedEvent) {
        this.getRecords();
    }
    showEntityChangeDetails(record: EntityChangeListDto): void {
        this.entityChangeDetailModal.show(record);
    }
}
