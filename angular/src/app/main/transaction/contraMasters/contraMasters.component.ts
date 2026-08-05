import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { ContraMastersServiceProxy, GetContraMasterForViewDto, ReportingServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
// import { ViewContraMasterModalComponent } from './view-contraMaster-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';

import { finalize } from 'rxjs';
import { Router } from '@angular/router';
import { ColDef, GridApi } from 'ag-grid-enterprise';
import { FormBuilder, FormGroup } from '@angular/forms';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    templateUrl: './contraMasters.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
})
export class ContraMastersComponent extends AppComponentBase implements OnInit, OnDestroy {
    // @ViewChild('viewContraMasterModal', { static: true })
    // viewContraMasterModal: ViewContraMasterModalComponent;

    filterText = '';
    form: FormGroup;

    advancedFiltersAreShown = false;
    private gridApi!: GridApi;

    selectedRows = [];

    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private _proxy: ContraMastersServiceProxy,
        private _reProxy: ReportingServiceProxy,
        private router: Router,
    ) {
        super(injector);
        this.getSetting();
        this.createForm();
    }

    ngOnInit(): void {
        this.setFinancialYear();
        this.createForm();
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
    createForm() {
        this.form = this.fb.group({
            toMiti: [this.toMiti],
            fromMiti: [this.fromMiti],
        });
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
            flex: 2
        },
        {
            field: 'voucherNo',
            headerName: this.l('Voucher No'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },
        {
            field: 'ledgerName',
            headerName: this.l('Account Ledger Name'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },
        {
            field: 'type',
            headerName: this.l('Type'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
        },
        {
            field: 'totalAmount',
            headerName: this.l('Total Amount'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 2,
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
        return `${viewButton}${printButton} ${editButton} ${deleteButton}`;
    }

    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 80, // Set a minimum width for each column
        maxWidth: 800, // Set a maximum width for each column
    };

    rowData: GetContraMasterForViewDto[] = [];

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
        this.currentPage = 0;
        this.loadPage(this.currentPage);
    }

    onCellDoubleClicked(params) {
        this.onEdit(params.data.id);
    }

    getCustomContextMenuItems = (params: any) => {
        const menuItem = [];

        if (this.isGranted('Pages.ContraMasters.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            };
            menuItem.push(editItem);
        }

        if (this.isGranted('Pages.ContraMasters.Delete')) {
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
        if (this.isGranted('Pages.ContraMasters.Edit')) {
            this.router.navigate(['app/main/transaction/contraMasters/edit', id]);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }

    onView(id: string) {
        // this.viewContraMasterModal.show(id);
    }

    addRoute() {
        if (this.isGranted('Pages.ContraMasters.Create')) {
            this.router.navigate(['app/main/transaction/contraMasters/add']);
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
            onlySelected: true,
            fileName: 'firmtype.xlsx',
        };

        this.gridApi.exportDataAsExcel(params);
    }

    onCellValueChanged(event) {
        if (this.isGranted('Pages.ContraMasters.Edit')) {
            this._proxy.createOrEdit(event.data).subscribe(() => {
                this.notify.success('Updated Successfully');
            });
        }
    }

    saveChanges() {
        const updatedData = this.gridApi.getSelectedRows();
        this.notify.error('Updated rows:', updatedData.toString());
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

    print(id: string): void {
        if (this.isGranted('Pages.ContraMasters.Print')) {
            this.router.navigate(['/app/main/transaction/pdf/0/', id]);
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
