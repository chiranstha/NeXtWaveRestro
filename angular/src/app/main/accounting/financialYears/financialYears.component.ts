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
import { FinancialYearsServiceProxy, GetFinancialYearForViewDto } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ViewFinancialYearModalComponent } from './view-financialYear-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { finalize } from 'rxjs';
import { ColDef, GridApi, RowSelectionOptions } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { PaginationComponent } from '../../../admin/shared/pagination.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { AddFinancialyearComponent } from './add-financialyear/add-financialyear.component';
@Component({
    templateUrl: './financialYears.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        SubHeaderComponent,
        FormsModule,
        AgGridAngular,
        PaginationComponent,
        LocalizePipe,
        PermissionPipe,
        AddFinancialyearComponent,
    ],
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class FinancialYearsComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _proxy = inject(FinancialYearsServiceProxy);
    @ViewChild('createOrEditFinancialYearModal', { static: true })
    createOrEditFinancialYearModal: AddFinancialyearComponent;
    @ViewChild('viewFinancialYearModalComponent', { static: true })
    viewFinancialYearModal: ViewFinancialYearModalComponent;
    filterText = '';
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public rowSelection: RowSelectionOptions | 'single' | 'multiple' = {
        mode: 'multiRow',
    };
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        headerName: 'Group',
        minWidth: 100,
        cellRendererParams: {
            innerRenderer: (params) => params?.node?.key ?? '',
        },
    };
    selectedRows = [];
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
            flex: 2,
        },

        {
            field: 'fromMiti',
            headerName: this.l('FromMiti'),
            sortable: true,
            filter: true,
            editable: true,
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
            flex: 2,
        },
        {
            field: 'toMiti',
            headerName: this.l('ToMiti'),
            sortable: true,
            filter: true,
            editable: true,
            rowGroup: false,
            enableRowGroup: true,
            hide: false,
            flex: 2,
        },
        {
            field: 'status',
            headerName: this.l('Old Year'),
            width: 200,
            cellRenderer: (params) => {
                return params.value
                    ? '<i class="fas fa-check-circle" style="color: green;">'
                    : '<i class="fas fa-times-circle" style="color: red;">';
            },
        },
        {
            field: 'active',
            headerName: this.l('Active'),
            width: 200,
            cellRenderer: (params) => {
                return params.value
                    ? '<i class="fas fa-check-circle" style="color: green;">'
                    : '<i class="fas fa-times-circle" style="color: red;">';
            },
        },
        {
            field: 'actions',
            headerName: this.l('Actions'),
            cellRenderer: this.actionCellRenderer,
            cellRendererParams: {
                onEdit: this.onEdit.bind(this),
                onDelete: this.deleteFeehead.bind(this),
                onChangeYear: this.onChangeYear.bind(this),
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
        minWidth: 150,
        maxWidth: 800,
    };
    rowData: GetFinancialYearForViewDto[] = [];
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
                const button = event.event.target.closest('.btnaction');
                if (!button) {
                    return;
                }
                const rowIndex = button.getAttribute('data-row-index');
                if (button.classList.contains('btn-edit')) {
                    this.onEdit(rowIndex);
                } else if (button.classList.contains('btn-delete')) {
                    this.deleteFeehead(rowIndex);
                } else if (button.classList.contains('btn-change')) {
                    this.onChangeYear(rowIndex);
                }
            }
        });
    }
    actionCellRenderer(params) {
        if (params.node && params.node.group) {
            return '';
        }
        const iconSize = '0.9em';
        const changeButton = `<button type="button" class="btnaction btn-change cursor-pointer" data-row-index="${params.data.id}" title="Change" style="background: none; border: none; cursor: pointer;"><i class="fas fa-play" style="color:#17a2b8; font-size: ${iconSize};"></i></button>`;
        const editButton = `<button type="button" class="btnaction btn-edit" data-row-index="${params.data.id}" title="Edit" style="background: none; border: none; cursor: pointer;"><i class="fas fa-pen" style="color:#ffc107; font-size: ${iconSize};"></i></button>`;
        const deleteButton = `<button type="button" class="btnaction btn-delete cursor-pointer" data-row-index="${params.data.id}" title="Delete" style="background: none; border: none; cursor: pointer;"><i class="fas fa-trash" style="color:#dc3545; font-size: ${iconSize};"></i></button>`;
        return `${changeButton} ${editButton} ${deleteButton}`;
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
        if (this.isGranted('Pages.FinancialYears.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<i class=\"fas fa-pen\" style=\"color:#ffc107;\"></i>',
            };
            menuItem.push(editItem);
        }
        if (this.isGranted('Pages.FinancialYears.Delete')) {
            const deleteItem = {
                name: 'Delete',
                action: () => {
                    this.deleteFeehead(params.node.data.id);
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
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
        if (this.isGranted('Pages.FinancialYears.Edit')) {
            this.createOrEditFinancialYearModal.show(id);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }
    addRoute() {
        if (this.isGranted('Pages.FinancialYears.Create')) {
            this.createOrEditFinancialYearModal.show();
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
        if (this.isGranted('Pages.FinancialYears.Edit')) {
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
    deleteFeehead(id: string): void {
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
    onChangeYear(id: string): void {
        this.message.confirm('', this.l('Are you sure you want to Change ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._proxy
                    .changeFinancialYear(id)
                    .pipe(
                        finalize(() => {
                            this.loadPage(this.currentPage);
                        }),
                    )
                    .subscribe(() => {
                        this.notify.success(this.l('Updated Successfully'));
                    });
            }
        });
    }
    searchValueOnApiCall(e) {
        this.filterText = e;
        this.loadPage(this.currentPage);
    }
}
