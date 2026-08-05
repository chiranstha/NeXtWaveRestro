import {
    HttpClient,
} from '@angular/common/http';
import {
    ChangeDetectionStrategy,
    Component,
    Injector,
    OnDestroy,
    OnInit,
    ViewEncapsulation,
} from '@angular/core';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { GetPurchaseReturnForViewDto, PurchaseReturnsServiceProxy, ReportingServiceProxy } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { finalize, Subject, takeUntil } from 'rxjs';
import { ColDef, GridApi, GridOptions, RowSelectionOptions } from 'ag-grid-community';
import { FormBuilder, FormGroup } from '@angular/forms';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    templateUrl: './purchaseReturns.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
})
export class PurchaseReturnsComponent extends AppComponentBase implements OnInit, OnDestroy {
    // @ViewChild('viewPurchaseReturnModal', { static: true })
    filterText = '';
    advancedFiltersAreShown = false;
    form: FormGroup;
    private destroy$ = new Subject<void>();
    private gridApi!: GridApi; // Define gridApi
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public rowSelection: RowSelectionOptions | 'single' | 'multiple' = {
        mode: 'multiRow',
    };

    gridOptions: GridOptions = {
        rowHeight: 20,
        headerHeight: 25,
        rowSelection: { mode: 'singleRow' },
        animateRows: true,
        pagination: false,
    };
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        minWidth: 200,
        maxWidth: 300
    };

    selectedRows = [];
    uploadUrl: string;

    constructor(
        injector: Injector,
        private _proxy: PurchaseReturnsServiceProxy,
        private _fileDownloadService: FileDownloadService,
        private _reProxy: ReportingServiceProxy,
        private _httpClient: HttpClient,
        private router: Router,
        private fb: FormBuilder,
    ) {
        super(injector); this.getSetting();
        this.createForm();
    }



    //end ag-Grid

    ngOnInit(): void {
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
    createForm() {
        this.form = this.fb.group({
            toMiti: [this.toMiti],
            fromMiti: [this.fromMiti],
        });

        // Subscribe to form changes
        this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
            this.filterChange();
        });
    }

    onPageChange(page: number): void {
        this.loadPage(page);
    }

    filterChange() {
        this.loadPage(this.currentPage);
        this._proxy.fixedPurchaseReturn().subscribe();
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
            flex: 3
        },

        {
            field: 'voucherNo',
            headerName: this.l('Voucher No'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },

        {
            field: 'ledgerName',
            headerName: this.l('Cash/Party'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },

        {
            field: 'totalDiscount',
            headerName: this.l('Discount Amt'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },

        {
            field: 'totalAmount',
            headerName: this.l('Total Amount'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },



        {
            field: 'totalTax',
            headerName: this.l('Total Tax'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
        },

        {
            field: 'grandTotal',
            headerName: this.l('Grand Total'),
            sortable: true,
            filter: true,
            rowGroup: false,
            enableRowGroup: true,

            editable: false,
            flex: 3
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
                    this.onView(rowIndex);
                } else if (button.classList.contains('btn-edit')) {
                    this.onEdit(rowIndex);
                } else if (button.classList.contains('btn-print')) {
                    this.onPrint(rowIndex);
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

    rowData: GetPurchaseReturnForViewDto[] = [];

    loadPage(page: number) {
        const fromDate = this.form.get('fromMiti').value;
        const toDate = this.form.get('toMiti').value;
        this._proxy.getAll(this.filterText, fromDate, toDate, '', page, this.pageSize).subscribe((data) => {
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

        if (this.isGranted('Pages.PurchaseReturns.Edit')) {
            const editItem = {
                name: 'Edit',
                action: () => {
                    this.onEdit(params.node.data.id);
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            };
            menuItem.push(editItem);
        }

        if (this.isGranted('Pages.PurchaseReturns.Delete')) {
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

    onView(id: string) {
    }

    onEdit(id: string) {
        if (this.isGranted('Pages.PurchaseReturns.Edit')) {
            this.router.navigate(['app/main/purchase/purchaseReturns/edit', id]);
        } else {
            this.notify.error('You are not authorized to perform this action');
        }
    }

    onPrint(id: string) {
        this.router.navigate(['app/main/purchase/pdf/4', id]);
    }

    addRoute() {
        if (this.isGranted('Pages.PurchaseReturns.Create')) {
            this.router.navigate(['app/main/purchase/purchaseReturns/add']);
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
            fileName: 'PurchaseReturns.xlsx', // Name of the exported file
        };

        this.gridApi.exportDataAsExcel(params); // Call the export function
    }

    onCellValueChanged(event) {
        if (this.isGranted('Pages.PurchaseReturns.Edit')) {
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

    exportToExcelfromAPI() {
        const fromdate = this.form.get('fromMiti').value;
        const todate = this.form.get('toMiti').value;
        this._proxy
            .getPurchaseReturnsToExcel(this.filterText, fromdate, todate, '', 0, this.totalRecords || this.pageSize)
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
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

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
