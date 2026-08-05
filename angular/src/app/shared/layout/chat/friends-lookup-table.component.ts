import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    OnInit,
    Output,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    FindUsersInput,
    FindUsersOutputDto,
    PagedResultDtoOfFindUsersOutputDto,
} from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent, SortChangedEvent } from 'ag-grid-enterprise';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../../shared/utils/auto-focus.directive';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
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
    canSelect?: (item: FindUsersOutputDto) => boolean | Observable<boolean>;
    loadOnStartup?: boolean;
    pageSize?: number;
}
//For more modal options http://valor-software.com/ngx-bootstrap/#/modals#modal-directive
@Component({
    selector: 'friendsLookupTable',
    templateUrl: './friends-lookup-table.component.html',
    imports: [FormsModule, AutoFocusDirective, BusyIfDirective, AgGridAngular, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class FriendsLookupTableComponent extends AppComponentBase implements OnInit {
    private _commonLookupService = inject(CommonLookupServiceProxy);
    private cdr = inject(ChangeDetectorRef);
    @Output() itemSelected: EventEmitter<FindUsersOutputDto> = new EventEmitter<FindUsersOutputDto>();
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Select'),
            field: 'select',
            width: 80,
            cellRenderer: (params) => {
                const button = document.createElement('button');
                button.className = 'btn btn-icon btn-bg-light btn-active-color-primary btn-sm';
                button.title = this.l('Select');
                button.innerHTML = `<i class="la la-chevron-circle-right" aria-label="${this.l('Select')}"></i>`;
                button.onclick = () => this.selectItem(params.data);
                return button;
            },
            sortable: false,
            filter: false,
        },
        {
            headerName: this.l('Name'),
            field: 'name',
            width: 150,
            sortable: true,
        },
        {
            headerName: this.l('Surname'),
            field: 'surname',
            width: 150,
            sortable: true,
        },
        {
            headerName: this.l('Email'),
            field: 'emailAddress',
            width: 200,
            sortable: true,
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        sortable: true,
        filter: true,
    };
    rowData: FindUsersOutputDto[] = [];
    totalRecordsCount = 0;
    isLoading = false;
    public saving = false;
    options: ICommonLookupModalOptions = {
        dataSource: (
            skipCount: number,
            maxResultCount: number,
            filter: string,
            tenantId?: number,
            excludeCurrentUser?: boolean,
        ) => {
            const input = new FindUsersInput();
            input.filter = filter;
            input.maxResultCount = maxResultCount;
            input.skipCount = skipCount;
            input.tenantId = tenantId;
            input.excludeCurrentUser = excludeCurrentUser;
            return this._commonLookupService.findUsers(input);
        },
        title: this.l('AddFriend'),
        canSelect: () => true,
        loadOnStartup: true,
        isFilterEnabled: true,
        pageSize: AppConsts.grid.defaultPageSize,
    };
    isInitialized = false;
    filterText = '';
    excludeCurrentUser = true;
    tenantId?: number;

    ngOnInit(): void {}
    refreshTable(): void {
        this.getRecords();
    }
    getRecordsIfNeeds(): void {
        if (!this.options.loadOnStartup && !this.isInitialized) {
            return;
        }
        this.getRecords();
        this.isInitialized = true;
    }
    getRecords(): void {
        this.isLoading = true;
        this.options
            .dataSource(
                0,
                this.options.pageSize || AppConsts.grid.defaultPageSize,
                this.filterText,
                this.tenantId,
                this.excludeCurrentUser,
            )
            .pipe(finalize(() => (this.isLoading = false)))
            .subscribe((result) => {
                this.totalRecordsCount = result.totalCount;
                this.rowData = result.items;
                this.cdr.markForCheck();
            });
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        if (this.options.loadOnStartup) {
            this.getRecordsIfNeeds();
        }
    }
    onSortChanged(event: SortChangedEvent) {
        this.getRecords();
    }
    selectItem(item: FindUsersOutputDto) {
        const boolOrPromise = this.options.canSelect(item);
        if (!boolOrPromise) {
            return;
        }
        if (boolOrPromise === true) {
            this.itemSelected.emit(item);
            return;
        }
        //assume as observable
        boolOrPromise.subscribe((result) => {
            if (result) {
                this.itemSelected.emit(item);
            }
        });
    }
}
