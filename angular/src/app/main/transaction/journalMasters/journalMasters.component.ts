import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { GetJournalMasterForViewDto, JournalMastersServiceProxy, ReportingServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormBuilder, FormGroup } from '@angular/forms';
import { finalize } from 'rxjs';
import { Router } from '@angular/router';
import { GridApi, RowSelectionOptions, ColDef } from 'ag-grid-community';
@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    templateUrl: './journalMasters.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
})
export class JournalMastersComponent extends AppComponentBase implements OnInit, OnDestroy {
    // @ViewChild('viewJournalMasterModal', { static: true })
    // viewJournalMasterModal: ViewJournalMasterModalComponent;
    filterText = '';
    form: FormGroup;
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public rowSelection: RowSelectionOptions | 'single' | 'multiple' = {
        mode: 'multiRow',
    };

    advancedFiltersAreShown = false;
    private gridApi!: GridApi;
    selectedRows = [];

    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private _reProxy: ReportingServiceProxy,
        private _proxy: JournalMastersServiceProxy,
        private router: Router,
    ) {
        super(injector);
        this.getSetting();
        this.createForm();
    }

    createForm() {
        this.form = this.fb.group({
            toMiti: [this.toMiti],
            fromMiti: [this.fromMiti],
        });
    }


    ngOnInit(): void {
        this.createForm();
        this.setFinancialYear();

        this.loadPage(this.currentPage);
    }

    setFinancialYear() {
        this._reProxy.getFinancialYears().subscribe(result => {
            this.form.patchValue({
                fromMiti: result.fromMiti,
                toMiti: result.toMiti
            });
        })
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
            field: 'dateMiti',
            headerName: this.l('Date Miti'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            editable: false,
            flex: 1,
        },
        {
            field: 'voucherNo',
            headerName: this.l('Voucher No'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            editable: false,
            flex: 1,
        },
        {
            field: 'debitTotal',
            headerName: this.l(' Amount'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            editable: false,
            flex: 3,
        },

        {
            field: 'description',
            headerName: this.l('Narration'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            editable: false,
            flex: 3,
        },
        {
            field: 'createUser',
            headerName: this.l('Create User'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,
            editable: false,
            flex: 2,
        },
        {
            field: 'updateUser',
            headerName: this.l('Update User'),
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
                onView: this.onView.bind(this),
                onPrint: this.print.bind(this),
                onEdit: this.onEdit.bind(this),
                onDelete: this.delete.bind(this),
                hideDeleteOnGroup: true
            },
            cellClass: 'actions-cell',
            width: 320,
            sortable: false,
            filter: false,
        },
    ];

    onView(id: string) {
        //  this.viewJournalMasterModal.show(id);
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

        // this.updateColumnVisibility(); // Call the function to set column visibility

        // Add event listeners for button clicks after grid is ready
        params.api.addEventListener('cellClicked', (event) => {
            if (event.colDef.headerName === 'Actions') {
                const button = event.event.target;
                const rowIndex = button.getAttribute('data-row-index');

                if (button.classList.contains('btn-edit')) {
                    this.onEdit(rowIndex);
                } else if (button.classList.contains('btn-delete')) {
                    this.delete(rowIndex);
                } else if (button.classList.contains('btn-print')) {
                    this.print(rowIndex);
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
        const printButton = `<button class="btnaction btn-print fa-duotone  fa-print" data-row-index="${params.data.id}"></button>`;
        const editButton = `<button class="btnaction btn-edit fa-duotone fa-pen-to-square" data-row-index="${params.data.id}"></button>`;
        const deleteButton = `<button class="btnaction btn-delete fa-duotone fa-trash cursor-pointer " data-row-index="${params.data.id}"></button>`;
        return `${viewButton} ${printButton} ${editButton} ${deleteButton}`;
    }

    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 120, // Set a minimum width for each column
        maxWidth: 800, // Set a maximum width for each column
    };

    rowData: GetJournalMasterForViewDto[] = [];

    loadPage(page: number) {
        this._proxy
            .getAll(
                this.filterText,
                this.form.get('fromMiti').value,
                this.form.get('toMiti').value,
                '',
                page,
                this.pageSize,
            )
            .subscribe((data) => {
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
        this.onEdit(params.data.id);
    }

    getCustomContextMenuItems = (params: any) => {
        const menuItem = [];
        if (this.isGranted('Pages.JournalMasters.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            };
            menuItem.push(editItem);
        }

        if (this.isGranted('Pages.JournalMasters.Delete')) {
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
        if (this.isGranted('Pages.JournalMasters.Edit')) {
            this.router.navigate(['app/main/transaction/journalMasters/edit', id]);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }

    addRoute() {
        if (this.isGranted('Pages.JournalMasters.Create')) {
            this.router.navigate(['app/main/transaction/journalMasters/add']);
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
            fileName: 'firmtype.xlsx', // Name of the exported file
        };

        this.gridApi.exportDataAsExcel(params); // Call the export function
    }

    onCellValueChanged(event) {
        if (this.isGranted('Pages.JournalMasters.Edit')) {
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

    print(id: string): void {
        if (this.isGranted('Pages.JournalMasters.Print')) {
            this.router.navigate(['/app/main/transaction/pdf/3/', id]);
        }
    }

    searchValueOnApiCall(e) {
        this.filterText = e;
        this.loadPage(this.currentPage);
    }

    numberWithCommas(x) {
        return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }
}
