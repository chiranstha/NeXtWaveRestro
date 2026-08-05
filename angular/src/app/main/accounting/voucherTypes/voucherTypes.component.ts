import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ColDef, GridApi } from 'ag-grid-enterprise';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { GetVoucherTypeForViewDto, VoucherTypesServiceProxy } from '@shared/service-proxies/service-proxies';

import { finalize } from 'rxjs';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'voucherTypes',
    templateUrl: './voucherTypes.component.html',
    animations: [appModuleAnimation]

})
export class VoucherTypesComponent extends AppComponentBase implements OnInit, OnDestroy {
    filterText = '';
    private gridApi!: GridApi; // Define gridApi

    selectedRows = [];

    constructor(
        injector: Injector,
        private _proxy: VoucherTypesServiceProxy,
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
            field: 'voucherName',
            headerName: this.l('Voucher Name'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },
        {
            field: 'startIndex',
            headerName: this.l('Start Index'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },
        {
            field: 'voucherGenerateType',
            headerName: this.l('Voucher Generate Type'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },

        {
            field: 'postfix',
            headerName: this.l('Postfix'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },
        {
            field: 'prefix',
            headerName: this.l('Prefix'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },

        {
            field: 'actions',
            headerName: this.l('Actions'),
            cellRenderer: this.actionCellRenderer,
            cellRendererParams: {
                onEdit: this.onEdit.bind(this),
                onDelete: this.deleteFeehead.bind(this),
            },
           cellClass: 'actions-cell',
            width: 320,
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

                if (button.classList.contains('btn-edit')) {
                    this.onEdit(rowIndex);
                } else if (button.classList.contains('btn-delete')) {
                    this.deleteFeehead(rowIndex);
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
        const editButton = `<button class="btnaction btn-edit fa-duotone fa-pen-to-square" data-row-index="${params.data.id}"></button>`;
        const deleteButton = `<button class="btnaction btn-delete fa-duotone fa-trash cursor-pointer " data-row-index="${params.data.id}"></button>`;
        return `${editButton} ${deleteButton}`;
    }

    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 120, // Set a minimum width for each column
        maxWidth: 800, // Set a maximum width for each column
    };

    rowData: GetVoucherTypeForViewDto[] = [];

    loadPage(page: number) {
        this._proxy.getAll(this.filterText, '', page, this.pageSize).subscribe((data) => {
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
        if (this.isGranted('Pages.VoucherTypes.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            };
            menuItem.push(editItem);
        }

        if (this.isGranted('Pages.VoucherTypes.Delete')) {
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
            icon: '<span class="ag-icon ag-icon-excel"></span>',
        };
        menuItem.push(excelExportItem);

        return menuItem;
    };

    onEdit(id: string) {
        if (this.isGranted('Pages.VoucherTypes.Edit')) {
            this.router.navigate(['app/main/accounting/voucherTypes/edit', id]);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }

    addRoute() {
        if (this.isGranted('Pages.VoucherTypes.Create')) {
            this.router.navigate(['app/main/accounting/voucherTypes/add']);
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
            fileName: 'voucherType.xlsx', // Name of the exported file
        };

        this.gridApi.exportDataAsExcel(params); // Call the export function
    }

    onCellValueChanged(event) {
        if (this.isGranted('Pages.VoucherTypes.Edit')) {
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

    searchValueOnApiCall(e) {
        this.filterText = e;
        this.loadPage(this.currentPage);
    }
}
