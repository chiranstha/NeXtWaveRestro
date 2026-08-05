import { Component, EventEmitter, Output, ViewChild, inject, NgZone, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NameValueDto, PagedResultDtoOfFindUsersOutputDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { GridApi, GridOptions, ColDef, GetContextMenuItemsParams } from 'ag-grid-enterprise';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../../shared/utils/auto-focus.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
export interface ICommonLookupModalOptions {
    title?: string;
    isFilterEnabled?: boolean;
    dataSource: (
        skipCount: number,
        maxResultCount: number,
        filter: string,
        tenantId?: number,
        excludeCurrentUser?: boolean,
    ) => Observable<PagedResultDtoOfFindUsersOutputDto>;
    canSelect?: (item: NameValueDto) => boolean | Observable<boolean>;
    loadOnStartup?: boolean;
    pageSize?: number;
}
@Component({
    selector: 'commonLookupModal',
    templateUrl: './common-lookup-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, AutoFocusDirective, AgGridAngular, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CommonLookupModalComponent extends AppComponentBase {
    static defaultOptions: ICommonLookupModalOptions = {
        dataSource: undefined,
        canSelect: () => true,
        loadOnStartup: true,
        isFilterEnabled: true,
        pageSize: AppConsts.grid.defaultPageSize,
    };
    @Output() itemSelected: EventEmitter<NameValueDto> = new EventEmitter<NameValueDto>();
    @ViewChild('modal', { static: true }) modal: ModalDirective;
    private zone = inject(NgZone);
    gridApi: GridApi | null = null;
    gridOptions: GridOptions;
    options: ICommonLookupModalOptions | undefined;
    isShown = false;
    isInitialized = false;
    filterText = '';
    excludeCurrentUser = true;
    tenantId?: number;
    constructor() {
        super();
        this.initializeGridOptions();
    }
    private initializeGridOptions(): void {
        this.gridOptions = {
            rowModelType: 'serverSide',
            columnDefs: this.getColumnDefs(),
            defaultColDef: {
                resizable: false,
                sortable: false,
                filter: false,
            },
            paginationPageSize: this.agGridTableHelper.defaultRecordsCountPerPage,
            paginationPageSizeSelector: this.agGridTableHelper.predefinedRecordsCountPerPage,
            cacheBlockSize: this.agGridTableHelper.defaultRecordsCountPerPage,
            onGridReady: (params) => {
                this.gridApi = params.api;
                this.agGridTableHelper.setGridApi(params.api);
                this.getRecordsIfNeeds();
            },
            onFirstDataRendered: (params) => {
                params.api.sizeColumnsToFit();
            },
            getContextMenuItems: (params: GetContextMenuItemsParams) => {
                return [
                    {
                        name: 'Select',
                        action: () => this.zone.run(() => this.selectItem(params.node.data)),
                    },
                ];
            },
        };
    }
    private getColumnDefs(): ColDef[] {
        return [
            {
                headerName: this.l('Select'),
                field: 'select',
                width: 80,
                cellRenderer: (params) => {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.className = 'btn btn-icon btn-bg-light btn-active-color-primary btn-sm';
                    button.title = this.l('Select');
                    button.innerHTML = '<i class="fa-solid fa-arrow-right-to-bracket"></i>';
                    button.addEventListener('click', (event) => {
                        event.preventDefault();
                        event.stopPropagation();
                        this.zone.run(() => this.selectItem(params.data));
                    });
                    return button;
                },
                sortable: false,
                filter: false,
            },
            {
                headerName: this.l('Name'),
                field: 'name',
                width: 150,
            },
            {
                headerName: this.l('Surname'),
                field: 'surname',
                width: 150,
            },
            {
                headerName: this.l('Email'),
                field: 'emailAddress',
                width: 200,
            },
        ];
    }
    configure(options: ICommonLookupModalOptions): void {
        this.options = Object.assign(
            {},
            CommonLookupModalComponent.defaultOptions,
            { title: this.l('SelectAnItem') },
            options,
        );
    }
    show(): void {
        if (!this.options) {
            throw Error(
                'Should call CommonLookupModalComponent.configure once before CommonLookupModalComponent.show!',
            );
        }
        this.modal.show();
    }
    refreshTable(): void {
        this.agGridTableHelper.refreshGrid();
    }
    close(): void {
        this.modal.hide();
    }
    shown(): void {
        this.isShown = true;
        setTimeout(() => this.getRecordsIfNeeds());
    }
    getRecordsIfNeeds(): void {
        if (!this.isShown) {
            return;
        }
        if (!this.options?.loadOnStartup && !this.isInitialized) {
            return;
        }
        this.getRecords();
        this.isInitialized = true;
    }
    getRecords(): void {
        if (!this.gridApi || !this.options) {
            return;
        }
        const dataSource = this.agGridTableHelper.createServerSideDatasource(
            (skipCount: number, maxResultCount: number, filter: string, sorting: string) => {
                return new Promise((resolve, reject) => {
                    this.agGridTableHelper.showLoadingIndicator();
                    this.options
                        .dataSource(skipCount, maxResultCount, this.filterText, this.tenantId, this.excludeCurrentUser)
                        .pipe(finalize(() => this.agGridTableHelper.hideLoadingIndicator()))
                        .subscribe({
                            next: (result) => resolve(result),
                            error: (error) => reject(error),
                        });
                });
            },
        );
        this.gridApi.setGridOption('serverSideDatasource', dataSource);
    }
    selectItem(item: NameValueDto) {
        if (!this.options) {
            return;
        }
        const boolOrPromise = this.options.canSelect(item);
        if (!boolOrPromise) {
            return;
        }
        if (boolOrPromise === true) {
            this.itemSelected.emit(item);
            this.close();
            return;
        }
        //assume as observable
        boolOrPromise.subscribe((result) => {
            if (result) {
                this.itemSelected.emit(item);
                this.close();
            }
        });
    }
}
