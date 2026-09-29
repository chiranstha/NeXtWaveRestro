import { HttpClient } from '@angular/common/http';
import {
    ChangeDetectionStrategy,
    Component,
    OnDestroy,
    OnInit,
    ViewChild,
    ViewEncapsulation,
    inject,
} from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountGroupsServiceProxy, GetAccountGroupForViewDto } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { FileUpload } from '@shared/ui-compat';
import { finalize } from 'rxjs';
import { ViewAccountGroupModalComponent } from './view-accountGroup-modal.component';
import { ColDef, GridApi, RowSelectionOptions } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { PaginationComponent } from '../../../admin/shared/pagination.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { AddOrEditAccountGroupComponent } from './accountGroups/add-or-edit-account-group/add-or-edit-account-group.component';
@Component({
    templateUrl: './accountGroups.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        SubHeaderComponent,
        FormsModule,
        AgGridAngular,
        PaginationComponent,
        ViewAccountGroupModalComponent,
        AddOrEditAccountGroupComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class AccountGroupsComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _proxy = inject(AccountGroupsServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);
    private _httpClient = inject(HttpClient);

    @ViewChild('ExcelFileUpload', { static: false }) excelFileUpload: FileUpload;
    @ViewChild('createOrEditAccountGroupModal', { static: true })
    createOrEditAccountGroupModal: AddOrEditAccountGroupComponent;
    @ViewChild('viewAccountGroupModalComponent', { static: true })
    viewAccountGroupModal: ViewAccountGroupModalComponent;
    filterText = '';
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public rowSelection: RowSelectionOptions | 'single' | 'multiple' = {
        mode: 'multiRow',
    };
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        headerName: 'Group',
        minWidth: 200,
        cellRendererParams: {
            innerRenderer: (params) => params?.node?.key ?? '',
        },
    };
    selectedRows = [];
    uploadUrl: string;
    public columnDefs: ColDef[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => {
                if (params.node?.group) {
                    return '';
                }
                let serial = 0;
                let result = '';
                params.api.forEachNodeAfterFilterAndSort((node) => {
                    if (node.group) {
                        return;
                    }
                    serial++;
                    if (node === params.node) {
                        result = String(serial);
                    }
                });
                return result;
            },
            cellRenderer: (params) => (params.node && params.node.group ? '' : params.value),
            width: 80,
        },
        {
            field: 'name',
            headerName: this.l('Name'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
            editable: true,
            flex: 3,
        },
        {
            field: 'accountGroupName',
            headerName: this.l('GroupName'),
            sortable: true,
            filter: true,
            editable: true,
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
            flex: 3,
        },
        {
            field: 'natureName',
            headerName: this.l('Nature'),
            sortable: true,
            filter: true,
            editable: true,
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
            flex: 3,
        },
        {
            field: 'actions',
            headerName: this.l('Actions'),
            cellRenderer: this.actionCellRenderer,
            cellRendererParams: {
                onEdit: this.onEdit.bind(this),
                onDelete: this.delete.bind(this),
                hideDeleteOnGroup: true,
            },
            cellClass: 'actions-cell',
            flex: 4,
            sortable: false,
            filter: false,
            enableRowGroup: false,
            lockPosition: 'right',
        },
    ];
    defaultColDef = {
        resizable: true,
        minWidth: 100,
        maxWidth: 300,
    };
    rowData: GetAccountGroupForViewDto[] = [];
    private gridApi!: GridApi;

    onPageChange(page: number): void {
        this.loadPage(page);
    }
    onGridReady(params) {
        this.gridApi = params.api;
        params.api.addEventListener('columnRowGroupChanged', () => {
            this.gridApi.refreshCells({ force: true });
        });
        this.gridApi.sizeColumnsToFit();

        params.api.addEventListener('cellClicked', (event) => {
            if (event.colDef.headerName === 'Actions') {
                const btn = event.event.target.closest('.btnaction');
                if (!btn) {
                    return;
                }
                const rowIndex = btn.getAttribute('data-row-index');
                if (btn.classList.contains('btn-edit')) {
                    this.onEdit(rowIndex);
                } else if (btn.classList.contains('btn-delete')) {
                    this.delete(rowIndex);
                }
            }
        });
    }
    actionCellRenderer(params) {
        if (params.node && params.node.group) {
            return '';
        }
        const iconSize = '0.9em';
        const editButton = `<button type="button" class=\"btnaction btn-edit\" data-row-index=\"${params.data.id}\" title=\"Edit\" style=\"background: none; border: none; cursor: pointer;\"><i class=\"fas fa-pen\" style=\"color:#ffc107; font-size: ${iconSize};\"></i></button>`;
        const deleteButton = `<button type="button" class=\"btnaction btn-delete cursor-pointer\" data-row-index=\"${params.data.id}\" title=\"Delete\" style=\"background: none; border: none; cursor: pointer;\"><i class=\"fas fa-trash\" style=\"color:#dc3545; font-size: ${iconSize};\"></i></button>`;
        return `${editButton} ${deleteButton}`;
    }
    loadPage(page: number) {
        this._proxy.getAll(this.filterText, '', page, this.pageSize).subscribe((data) => {
            this.rowData = this.setGridRowData(this.gridApi, data.items);
            this.totalRecords = data.totalCount;
            this.calculatePageSizeOptions();
        });
    }
    onPageSizeChange(newPageSize: number): void {
        this.pageSize = +newPageSize;
        this.currentPage = 0;
        this.loadPage(this.currentPage);
    }

    getCustomContextMenuItems = (params: any) => {
        const menuItem = [];
        if (this.isGranted('Pages.AccountGroups.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<i class=\"fas fa-pen\" style=\"color:#ffc107;\"></i>',
            };
            menuItem.push(editItem);
        }
        if (this.isGranted('Pages.AccountGroups.Delete')) {
            const deleteItem = {
                name: 'Delete',
                action: () => {
                    this.delete(params.node.data.id);
                },
                icon: '<i class=\"fas fa-trash\" style=\"color:#dc3545;\"></i>',
            };
            menuItem.push(deleteItem);
        }
        const excelExportItem = {
            name: 'Export to Excel',
            action: () => {
                params.api.exportDataAsExcel();
            },
            icon: '<i class="fas fa-file-excel" style="color:#28a745;"></i>',
        };
        menuItem.push(excelExportItem);
        return menuItem;
    };
    onEdit(id: string) {
        if (this.isGranted('Pages.AccountGroups.Edit')) {
            this.createOrEditAccountGroupModal.show(id);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }
    addRoute() {
        if (this.isGranted('Pages.AccountGroups.Create')) {
            this.createOrEditAccountGroupModal.show();
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }
    deleteSelectedRows() {
        this.selectedRows = this.gridApi.getSelectedRows();
        const selectedIds = this.selectedRows.map((row) => row.id);
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            let num = 1;
            if (isConfirmed) {
                this.gridApi.applyTransaction({ remove: this.selectedRows });
                selectedIds.forEach((id) => {
                    this._proxy.delete(id).subscribe(() => {
                        num = num + 1;
                    });
                });
            }
            this.notify.success(`${selectedIds.length} row Deleted Successfully`);
            this.selectedRows = [];
            this.loadPage(this.currentPage);
        });
    }
    onSelectionChanged() {
        this.selectedRows = this.gridApi.getSelectedRows();
    }
    exportSelectedRows() {
        const params = {
            onlySelected: true,
            fileName: 'firmtype.xlsx',
        };
        this.gridApi.exportDataAsExcel(params);
    }
    onCellValueChanged(event) {
        if (this.isGranted('Pages.AccountGroups.Edit')) {
            this._proxy.createOrEdit(event.data).subscribe(() => {
                this.notify.success('Updated Successfully');
            });
        }
    }
    saveChanges() {
        const updatedData = this.gridApi.getSelectedRows();
        this.notify.error('Updated rows:', updatedData.toString());
    }

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.loadPage(this.currentPage);
    }
    delete(id: string): void {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._proxy
                    .delete(id)
                    .pipe(
                        finalize(() => {
                            this.loadPage(this.currentPage);
                        }),
                    )
                    .subscribe(() => {
                        this.notify.success(this.l('Deleted Successfully'));
                    });
            }
        });
    }
    searchValueOnApiCall(e) {
        this.filterText = e;
        this.loadPage(this.currentPage);
    }
    exportToExcelfromAPI(event) {
        if (event) {
            this._proxy
                .getAccountGroupsToExcel(this.filterText, '', this.currentPage, this.pageSize)
                .subscribe((result) => {
                    this._fileDownloadService.downloadTempFile(result);
                });
        }
    }
    importexcelfromAPI(data: { files: File }) {
        const formData: FormData = new FormData();
        const file = data.files[0];
        formData.append('file', file, file.name);
        this._httpClient
            .post<any>(this.uploadUrl, formData)
            .pipe(finalize(() => this.excelFileUpload.clear()))
            .subscribe((response) => {
                if (response.success) {
                    this.notify.success(this.l('ImportAccountGroupProcessStart'));
                    this.loadPage(this.currentPage);
                } else if (response.error != null) {
                    this.notify.error(this.l('ImportAccountGroupUploadFailed'));
                }
            });
    }
}
