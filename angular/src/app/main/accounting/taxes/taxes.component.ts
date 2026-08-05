import {
    HttpClient,
} from '@angular/common/http';
import {
    ChangeDetectorRef,
    ChangeDetectionStrategy,
    Component,
    Injector,
    OnDestroy,
    OnInit,
    ViewChild,
    ViewEncapsulation,
} from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { GetTaxForViewDto, TaxesServiceProxy } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { finalize } from 'rxjs';
// import { ViewTaxModalComponent } from './view-tax-modal.component';
import { GridApi, RowSelectionOptions, ColDef } from 'ag-grid-community';
import { AddTaxesComponent } from './addTaxes/addTaxes.component';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    templateUrl: './taxes.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
})
export class TaxComponent extends AppComponentBase implements OnInit, OnDestroy {

   
    @ViewChild('createOrEditTaxModal', { static: true })
    createOrEditTaxModal: AddTaxesComponent;

    filterText: string = '';
    private gridApi!: GridApi; // Define gridApi
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public rowSelection: RowSelectionOptions | 'single' | 'multiple' = {
        mode: 'multiRow',
    };
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        minWidth: 200,
    };
    selectedRows = [];
    uploadUrl: string;

    constructor(
        injector: Injector,
        private _proxy: TaxesServiceProxy,
        private _fileDownloadService: FileDownloadService,
        private _httpClient: HttpClient,
        private cdr: ChangeDetectorRef,
    ) {
        super(injector);
    }

    onPageChange(page: number): void {
        this.loadPage(page);
    }

    public columnDefs: ColDef[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => params.node.rowIndex + 1,
            width: 80,
        },

        {
            field: 'name',
            headerName: this.l('Name'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },

        {
            field: 'rate',
            headerName: this.l('Rate'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3,
        },

        {
            field: 'description',
            headerName: this.l('Description'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },

        {
            field: 'isActive',
            headerName: this.l('IsActive'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
            valueFormatter: (params) => params.value ? this.l('Active') : this.l('Inactive'),
            cellRenderer: (params) =>
                params.value
                    ? '<span class="badge badge-light-success fw-semibold">Active</span>'
                    : '<span class="badge badge-light-danger fw-semibold">Inactive</span>',
        },


        {
            field: 'actions',
            headerName: this.l('Actions'),
            cellRenderer: this.actionCellRenderer,
            cellRendererParams: {
                //       onView: this.onView.bind(this),
                onEdit: this.onEdit.bind(this),
                onDelete: this.delete.bind(this),
                hideDeleteOnGroup: true
            },
            cellClass: 'actions-cell',
            width: 160,
            sortable: false,
            filter: false,
        },
    ];

    onGridReady(params) {
        this.gridApi = params.api; // Store the grid API reference


        params.api.addEventListener('columnRowGroupChanged', () => {
            const groupedColumns = this.gridApi.getRowGroupColumns();

            if (groupedColumns.length === 0) {

                this.gridApi.setColumnsVisible(['actions'], true);
                this.gridApi.refreshCells();
            } else {
                this.gridApi.setColumnsVisible(['actions'], false);
            }
        });


        // Add event listeners for button clicks after grid is ready
        params.api.addEventListener('cellClicked', (event) => {
            if (event.colDef.headerName === 'Actions') {
                const button = event.event.target;
                const rowIndex = button.getAttribute('data-row-index');

                if (button.classList.contains('btn-view')) {
                    //       this.onView(rowIndex);
                } else if (button.classList.contains('btn-edit')) {
                    this.onEdit(rowIndex);
                } else if (button.classList.contains('btn-delete')) {
                    this.delete(rowIndex);
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
        const viewButton = `<button class="btnaction btn-view fa-duotone fa-eye" data-row-index="${params.data.id}"></button>`;
        const editButton = `<button class="btnaction btn-edit fa-duotone fa-pen-to-square" data-row-index="${params.data.id}"></button>`;
        const deleteButton = `<button class="btnaction btn-delete fa-duotone fa-trash cursor-pointer " data-row-index="${params.data.id}"></button>`;
        return `${viewButton} ${editButton} ${deleteButton}`;
    }

    defaultColDef: ColDef = {
        resizable: true,
        minWidth: 100,
    };

    rowData: GetTaxForViewDto[] = [];

    loadPage(page: number) {
        this._proxy.getAll(this.filterText, '', page, this.pageSize).subscribe((data) => {
            this.rowData = this.setGridRowData(this.gridApi, data.items);
            this.totalRecords = data.totalCount;

            this.calculatePageSizeOptions();
            this.cdr.markForCheck();
        });
    }

    onPageSizeChange(newPageSize: number): void {
        this.pageSize = +newPageSize;
        this.currentPage = 0; // Reset to first page when page size changes
        this.loadPage(this.currentPage);
    }

    onCellDoubleClicked(params) {
        //  this.onEdit(params.data.id);
    }

    getCustomContextMenuItems = (params: any) => {
        const menuItem = [];


        const viewItem = {
            name: 'View',
            action: () => {
                //   this.onView(params.node.data.id);
            },
            icon: '<span class="ag-icon ag-icon-excel"></span>',
        };
        menuItem.push(viewItem);


        if (this.isGranted('Pages.Taxes.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            };
            menuItem.push(editItem);
        }

        if (this.isGranted('Pages.Taxes.Delete')) {
            const deleteItem = {
                name: 'Delete',
                action: () => {
                    this.delete(params.node.data.id);
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
            icon: '<span class="ag-icon ag-icon-excel"></span>',
        };
        menuItem.push(excelExportItem);

        return menuItem;
    };

    // onView(id: string) {
    //     this.viewTaxModalComponent.show(id);
    // }


    onEdit(id: string) {
        if (this.isGranted('Pages.Taxes.Edit')) {
            this.createOrEditTaxModal.show(id);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }


    addRoute() {
        if (this.isGranted('Pages.Taxes.Create')) {
            this.createOrEditTaxModal.show();
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }

    deleteSelectedRows() {
        this.selectedRows = this.gridApi.getSelectedRows();
        const selectedIds = this.selectedRows.map((row) => row.id); // Extract IDs

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

            this.notify.success(`${selectedIds.length  } row Deleted Successfully`);
            this.selectedRows = [];
            this.loadPage(this.currentPage);
        });
    }

    onSelectionChanged() {
        this.selectedRows = this.gridApi.getSelectedRows();
    }

    exportSelectedRows() {
        const params = {
            onlySelected: true, // Export only selected rows
            fileName: 'tax.xlsx', // Name of the exported file
        };

        this.gridApi.exportDataAsExcel(params); // Call the export function
    }

    onCellValueChanged(event) {
        if (this.isGranted('Pages.Taxes.Edit')) {
            this._proxy.createOrEdit(event.data).subscribe(() => {
                this.notify.success('Updated Successfully');
            });
        }
    }

    saveChanges() {
        const updatedData = this.gridApi.getSelectedRows();
        this.notify.error('Updated rows:', updatedData.toString());

        // Here you can implement logic to save these changes to your backend or data source
    }

    //end ag-Grid

    ngOnInit(): void {
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

    searchValueOnApiCall(e: string): void {
        this.filterText = e;
        this.currentPage = 0;
        this.loadPage(this.currentPage);
    }

    exportToExcelfromAPI(event) {
        if (event) {
            this._proxy
                .getTaxesToExcel(this.filterText, '', 0, 5)

                .subscribe((result) => {
                    this._fileDownloadService.downloadTempFile(result);
                });
        }
    }

    // importexcelfromAPI(data: { files: File }) {
    //     const formData: FormData = new FormData();
    //     const file = data.files[0];
    //     formData.append('file', file, file.name);
    //     this._httpClient
    //         .post<any>(this.uploadUrl, formData)
    //         .pipe(finalize(() => this.excelFileUpload.clear()))
    //         .subscribe((response) => {
    //             if (response.success) {
    //                 this.notify.success(this.l('ImportAccountGroupProcessStart'));
    //                 this.loadPage(this.currentPage);
    //             } else if (response.error != null) {
    //                 this.notify.error(this.l('ImportAccountGroupUploadFailed'));
    //             }
    //         });
    // }
}
