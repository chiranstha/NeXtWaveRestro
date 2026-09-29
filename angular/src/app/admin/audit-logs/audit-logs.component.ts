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
import { Router } from '@angular/router';
import { AuditLogDetailModalComponent } from '@app/admin/audit-logs/audit-log-detail-modal.component';
import { EntityChangeDetailModalComponent } from '@app/shared/common/entityHistory/entity-change-detail-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AuditLogListDto,
    AuditLogServiceProxy,
    EntityChangeListDto,
    NameValueDto,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from '@shared/ui-compat';
import { Paginator } from '@shared/ui-compat';
import { DataTableHelper } from 'shared/helpers/DataTableHelper';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { ColDef, ValueFormatterParams, ValueGetterParams, GetContextMenuItemsParams, ICellRendererParams } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { PaginationComponent } from '../shared/pagination.component';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AuditLogDetailModalComponent as AuditLogDetailModalComponent_1 } from './audit-log-detail-modal.component';
import { EntityChangeDetailModalComponent as EntityChangeDetailModalComponent_1 } from '../../shared/common/entityHistory/entity-change-detail-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './audit-logs.component.html',
    styleUrls: ['./audit-logs.component.less'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        FormsModule,
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        AgGridAngular,
        PaginationComponent,
        BusyIfDirective,
        Paginator,
        AuditLogDetailModalComponent_1,
        EntityChangeDetailModalComponent_1,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class AuditLogsComponent extends AppComponentBase implements OnInit {
    private _auditLogService = inject(AuditLogServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);
    private _dateTimeService = inject(DateTimeService);
    private _router = inject(Router);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @ViewChild('auditLogDetailModal', { static: true }) auditLogDetailModal: AuditLogDetailModalComponent;
    @ViewChild('entityChangeDetailModal', { static: true }) entityChangeDetailModal: EntityChangeDetailModalComponent;
    @ViewChild('paginatorEntityChanges', { static: true }) paginatorEntityChanges: Paginator;
    //Filters
    public dateRange: DateTime[] = [];
    public usernameAuditLog: string;
    public usernameEntityChange: string;
    public serviceName: string;
    public methodName: string;
    public browserInfo: string;
    public hasException: boolean = undefined;
    public minExecutionDuration: number;
    public maxExecutionDuration: number;
    public entityTypeFullName: string;
    public objectTypes: NameValueDto[] = [];
    dataTableHelperAuditLogs = new DataTableHelper();
    dataTableHelperEntityChanges = new DataTableHelper();
    entityChangesSorting = 'userName ASC';
    readonly entityChangesColumnDefs: ColDef[] = [
        {
            headerName: this.l('Actions'),
            width: 155,
            sortable: false,
            filter: false,
            cellRenderer: (params: ICellRendererParams) => this.createEntityChangeActions(params.data),
        },
        { headerName: this.l('Action'), field: 'changeTypeName', minWidth: 150 },
        { headerName: this.l('Object'), field: 'entityTypeFullName', minWidth: 230, flex: 1 },
        { headerName: this.l('UserName'), field: 'userName', minWidth: 140, sort: 'asc' },
        {
            headerName: this.l('Time'), field: 'changeTime', minWidth: 180,
            valueFormatter: (params) => params.value ? this._dateTimeService.formatDate(params.value, 'yyyy-MM-dd HH:mm:ss') : '',
        },
    ];
    readonly entityChangesDefaultColDef: ColDef = { resizable: true, sortable: true, minWidth: 100 };
    entityChangesLoading = false;
    advancedFiltersAreShown = false;
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        minWidth: 200,
    };
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
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
            sort: 'asc',
        },
        {
            field: 'serviceName',
            headerName: this.l('Service'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
        },
        {
            field: 'methodName',
            headerName: this.l('MethodName'),
            sortable: true,
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
            menuTabs: ['filterMenuTab', 'generalMenuTab', 'columnsMenuTab'],
        },
        {
            field: 'executionDuration',
            headerName: this.l('ExecutionDuration'),
            sortable: true,
            filter: true,
            valueGetter: (params: ValueGetterParams) => {
                return params.data.executionDuration;
            },
            valueFormatter: ({ value }: ValueFormatterParams) => `${value} ms`,
            cellClass: 'text-right',
            aggFunc: 'sum',
            enableValue: true,
            enableRowGroup: true,
            enablePivot: true,
            menuTabs: ['filterMenuTab', 'generalMenuTab', 'columnsMenuTab'],
            width: 150,
        },
        {
            field: 'clientIpAddress',
            headerName: this.l('ClientIpAddress'),
            sortable: true,
            filter: true,
        },
        {
            field: 'clientName',
            headerName: this.l('ClientName'),
            sortable: true,
            filter: true,
        },
        {
            field: 'browserInfo',
            headerName: this.l('BrowserInfo'),
            sortable: true,
            filter: true,
            cellRenderer: (params) => {
                return this.truncateStringWithPostfix(params.value, 20);
            },
        },
        {
            field: 'exception',
            headerName: this.l('Exception'),
            cellRenderer: (params) => {
                return this.truncateStringWithPostfix(params.value, 20);
            },
            sortable: false,
            filter: false,
        },
        {
            field: 'executionTime',
            headerName: this.l('ExecutionTime'),
            cellRenderer: (params) => {
                return this._dateTimeService.formatDate(params.value, 'yyyy-MM-dd HH:mm:ss');
            },
            sortable: false,
            filter: false,
        },
        {
            field: 'actions',
            headerName: this.l('Actions'),
            cellRenderer: this.actionCellRenderer,
            cellRendererParams: {
                onEdit: this.onEdit.bind(this),
                onDelete: this.onDelete.bind(this),
            },
            cellClass: 'actions-cell',
            width: 150,
            sortable: false,
            filter: false,
        },
    ];
    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 100, // Set a minimum width for each column
        maxWidth: 300, // Set a maximum width for each column
    };
    rowData: AuditLogListDto[] = [];
    gridApi: any;

    onPageChange(page: number): void {
        this.currentPage = page;
        this.loadPage(page);
    }
    onGridReady(params) {
        this.gridApi = params.api;
        // Add event listeners for button clicks after grid is ready
        params.api.addEventListener('cellClicked', (event) => {
            if (event.colDef.headerName === 'Actions') {
                const button = event.event.target;
                const rowIndex = button.getAttribute('data-row-index');
                if (button.classList.contains('btn-edit')) {
                    this._zone.run(() => this.onEdit(rowIndex));
                } else if (button.classList.contains('btn-delete')) {
                    this._zone.run(() => this.onDelete(rowIndex));
                } else if (button.classList.contains('btn-print')) {
                    this._zone.run(() => this.onDelete(rowIndex));
                }
            }
        });
    }
    actionCellRenderer(params) {
        const isGrouped = params.api.getRowGroupColumns().length > 0;
        if (isGrouped && params.colDef.cellRendererParams.hideDeleteOnGroup) {
            params.api.setColumnsVisible(['actions'], false);
            return '';
        }
        const editButton = `<button type="button" class="btnaction btn-edit fa-duotone fa-pen-to-square" data-row-index="${params.data.id}"></button>`;
        const printButton = `<button type="button" class="btnaction btn-print fa-duotone fa-file-pdf" data-row-index="${params.data.id}"></button>`;
        const deleteButton = `<button type="button" class="btnaction btn-delete fa-duotone fa-trash cursor-pointer " data-row-index="${params.data.id}"></button>`;
        return `${editButton} ${deleteButton} ${printButton}`;
    }
    onEdit(rowIndex) {
        this.notify.success(`Edit row: ${rowIndex}`);
    }
    onDelete(rowIndex) {
        this.notify.success(`Delete row: ${rowIndex}`);
    }
    onPrint(rowIndex) {
        this.notify.success(`Print row: ${rowIndex}`);
    }
    loadPage(page: number) {
        this._auditLogService
            .getAuditLogs(
                this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(this.dateRange[1]),
                this.usernameAuditLog,
                this.serviceName,
                this.methodName,
                this.browserInfo,
                this.hasException,
                this.minExecutionDuration,
                this.maxExecutionDuration,
                '',
                this.pageSize,
                page,
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

    onCellDoubleClicked(params) {
        this._zone.run(() => this.showAuditLogDetails(params.data));
    }
    getCustomContextMenuItems = (params: GetContextMenuItemsParams) => {
        const editItem = {
            name: 'Edit',
            action: () => {
                this.onUpdate(params.node.data.id);
            },
            icon: '<span class="ag-icon ag-icon-excel"></span>',
        };
        const deleteItem = {
            name: 'Delete',
            action: () => {
                params.api.exportDataAsExcel();
            },
            icon: '<span class="ag-icon ag-icon-excel"></span>',
        };
        const excelExportItem = {
            name: 'Export to Excel',
            action: () => {
                params.api.exportDataAsExcel();
            },
            icon: '<i class="fas fa-file-excel" style="color:#28a745;"></i>',
        };
        return [editItem, deleteItem, excelExportItem];
    };
    onUpdate(_id: number | string) {}
    //end ag-Grid
    ngOnInit(): void {
        super.ngOnInit();
        this.dateRange = [DateTime.local().startOf('day'), DateTime.local().endOf('day')];
        this.loadPage(this.currentPage);
        this.getEntityChanges({ first: 0, rows: this.dataTableHelperEntityChanges.defaultRecordsCountPerPage });
    }
    showAuditLogDetails(record: AuditLogListDto): void {
        this.auditLogDetailModal.show(record);
    }
    showEntityChangeDetails(record: EntityChangeListDto): void {
        this.entityChangeDetailModal.show(record);
    }
    getEntityChanges(event?: LazyLoadEvent) {
        this._auditLogService.getEntityHistoryObjectTypes().subscribe((result) => {
            this.objectTypes = result;
            this._cdr.markForCheck();
        });
        if (this.dataTableHelperEntityChanges.shouldResetPaging(event)) {
            this.paginatorEntityChanges.changePage(0);
            if (this.dataTableHelper.records && this.dataTableHelper.records.length > 0) {
                return;
            }
        }
        this.setEntityChangesLoading(true);
        this.dataTableHelperEntityChanges.showLoadingIndicator();
        this._auditLogService
            .getEntityChanges(
                this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(this.dateRange[1]),
                this.usernameEntityChange,
                this.entityTypeFullName,
                this.entityChangesSorting,
                this.dataTableHelperEntityChanges.getMaxResultCount(this.paginatorEntityChanges, event),
                this.dataTableHelperEntityChanges.getSkipCount(this.paginatorEntityChanges, event),
            )
            .subscribe((result) => {
                this.dataTableHelperEntityChanges.totalRecordsCount = result.totalCount;
                this.dataTableHelperEntityChanges.records = result.items;
                this.dataTableHelperEntityChanges.hideLoadingIndicator();
                this.setEntityChangesLoading(false);
                this._cdr.markForCheck();
            });
    }

    onEntityChangesSortChanged(event: any): void {
        const sort = (event.api.getColumnState() || []).find((column: any) => column.sort);
        this.entityChangesSorting = sort ? `${sort.colId} ${sort.sort.toUpperCase()}` : '';
        this.paginatorEntityChanges.changePage(0);
    }

    private createEntityChangeActions(record: EntityChangeListDto): HTMLElement {
        const container = document.createElement('div');
        container.className = 'd-flex gap-2';
        if (this.isGranted('Pages.Administration.EntityChanges.FullHistory')) {
            const allChanges = document.createElement('button');
            allChanges.type = 'button';
            allChanges.className = 'btn btn-sm btn-primary';
            allChanges.textContent = this.l('AllChanges');
            allChanges.title = this.l('EntityChangeAllDetailTooltipTitle');
            allChanges.addEventListener('click', () => this._zone.run(() => this.showEntityChangeAllDetails(record)));
            container.append(allChanges);
        }
        const viewChange = document.createElement('button');
        viewChange.type = 'button';
        viewChange.className = 'btn btn-sm btn-light-primary';
        viewChange.textContent = this.l('ViewChange');
        viewChange.title = this.l('EntityChangeDetailTooltip');
        viewChange.addEventListener('click', () => this._zone.run(() => this.showEntityChangeDetails(record)));
        container.append(viewChange);
        return container;
    }
    showEntityChangeAllDetails(record: EntityChangeListDto): void {
        this._router.navigate([
            `${abp.appPath}/app/admin/entity-changes/${record.entityId}/${record.entityTypeFullName}`,
        ]);
    }
    exportToExcelAuditLogs(): void {
        const self = this;
        self._auditLogService
            .getAuditLogsToExcel(
                this._dateTimeService.getStartOfDayForDate(self.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(self.dateRange[1]),
                self.usernameAuditLog,
                self.serviceName,
                self.methodName,
                self.browserInfo,
                self.hasException,
                self.minExecutionDuration,
                self.maxExecutionDuration,
                undefined,
                1,
                0,
            )
            .subscribe((result) => {
                self._fileDownloadService.downloadTempFile(result);
            });
    }
    exportToExcelEntityChanges(): void {
        const self = this;
        self._auditLogService
            .getEntityChangesToExcel(
                this._dateTimeService.getStartOfDayForDate(self.dateRange[0]),
                self._dateTimeService.getEndOfDayForDate(self.dateRange[1]),
                self.usernameEntityChange,
                self.entityTypeFullName,
                undefined,
                1,
                0,
            )
            .subscribe((result) => {
                self._fileDownloadService.downloadTempFile(result);
            });
    }
    truncateStringWithPostfix(text: string, length: number): string {
        return abp.utils.truncateStringWithPostfix(text, length);
    }
    private setEntityChangesLoading(value: boolean): void {
        setTimeout(() => {
            this.entityChangesLoading = value;
            this._cdr.markForCheck();
        });
    }
}
