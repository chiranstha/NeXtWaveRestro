import {
    HttpClient,
} from '@angular/common/http';
import {
    ChangeDetectionStrategy,
    Component,
    Injector,
    OnDestroy,
    OnInit,
    ViewChild,
    ViewEncapsulation,
} from '@angular/core';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { GetPDCPayableForViewDto, PDCPayablesServiceProxy } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { GridApi, RowSelectionOptions, ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    templateUrl: './pdcPayable.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
})
export class PdcPayableComponent extends AppComponentBase implements OnInit, OnDestroy {
    @ViewChild('viewPdcPayableModal', { static: true })
    // viewPdcPayableModal: ViewPdcPayableModalComponent;
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
        private _proxy: PDCPayablesServiceProxy,
        private _fileDownloadService: FileDownloadService,
        private _httpClient: HttpClient,
        private router: Router,
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
            field: 'voucherNo',
            headerName: this.l('Voucher No'),
            sortable: true,
            filter: false,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 5
        },

        {
            field: 'ledgerName',
            headerName: this.l('ledgerName'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 7
        },

        {
            field: 'bankName',
            headerName: this.l('Bank Name'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 7
        },

        {
            field: 'chequeNo',
            headerName: this.l('Cheque No'),
            sortable: true,
            filter: false,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },


        {
            field: 'dateMiti',
            headerName: this.l('Date Miti'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 5
        },



        {
            field: 'amount',
            headerName: this.l('Amount'),
            sortable: true,
            filter: false,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 4
        },

        {
            field: 'createUser',
            headerName: this.l('Created By'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },

        {
            field: 'updateUser',
            headerName: this.l('Updated By'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },


        {
            field: 'actions',
            headerName: this.l('Actions'),
            cellRenderer: this.actionCellRenderer,
            cellRendererParams: {
                onView: this.onView.bind(this),
                onEdit: this.onEdit.bind(this),
                onPrint: this.onPrint.bind(this),
                onDelete: this.delete.bind(this),
                hideDeleteOnGroup: true
            },
            cellClass: 'actions-cell',
            width: 200,
            sortable: false,
            filter: false,
        }
    ];

    onView(id: string) {
        // this.viewPdcPayableModal.show(id);
    }

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

      //  this.updateColumnVisibility(); // Call the function to set column visibility

        // Add event listeners for button clicks after grid is ready
        params.api.addEventListener('cellClicked', (event) => {
            if (event.colDef.headerName === 'Actions') {
                const button = event.event.target;
                const rowIndex = button.getAttribute('data-row-index');

                if (button.classList.contains('btn-edit')) {
                    this.onEdit(rowIndex);
                } else if (button.classList.contains('btn-print')) {
                    this.onPrint(rowIndex);
                } else if (button.classList.contains('btn-delete')) {
                    this.delete(rowIndex);
                } else if (button.classList.contains('btn-view')) {
                    this.onView(rowIndex);
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
        const printButton = `<button class="btnaction btn-print fa-duotone fa-print" data-row-index="${params.data.id}"></button>`;
        const editButton = `<button class="btnaction btn-edit fa-duotone fa-pen-to-square" data-row-index="${params.data.id}"></button>`;
        const deleteButton = `<button class="btnaction btn-delete fa-duotone fa-trash cursor-pointer " data-row-index="${params.data.id}"></button>`;
        return `${viewButton} ${printButton} ${editButton} ${deleteButton}`;
    }

    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 100, // Set a minimum width for each column
        maxWidth: 300, // Set a maximum width for each column
    };

    rowData: GetPDCPayableForViewDto[] = [];

    loadPage(page: number) {
        this._proxy.getAll('', '', page, this.pageSize).subscribe((data) => {
            this.rowData = this.setGridRowData(this.gridApi, data.items);
            this.totalRecords = data.totalCount;

            this.calculatePageSizeOptions();
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
        if (this.isGranted('Pages.PDCPayables.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            };
            menuItem.push(editItem);
        }

        if (this.isGranted('Pages.PDCPayables.Delete')) {
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


    onEdit(id: string) {
        if (this.isGranted('Pages.PDCPayables.Edit')) {
            this.router.navigate(['app/main/transaction/PdcPayable/edit', id]);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }

    onPrint(id: string) {
        this.router.navigate(['app/main/transaction/pdf/6', id]);
    }

    addRoute() {
        if (this.isGranted('Pages.PDCPayables.Create')) {
            this.router.navigate(['app/main/transaction/PdcPayable/add']);
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
            fileName: 'PdcPayables.xlsx', // Name of the exported file
        };

        this.gridApi.exportDataAsExcel(params); // Call the export function
    }

    onCellValueChanged(event) {
        if (this.isGranted('Pages.PDCPayables.Edit')) {
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
        // this.filterText = e;
        this.loadPage(this.currentPage);
    }
}
