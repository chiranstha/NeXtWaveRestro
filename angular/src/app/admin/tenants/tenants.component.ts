import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    OnInit,
    ViewChild,
    ViewEncapsulation,
    inject,
    NgZone,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ImpersonationService } from '@app/admin/users/impersonation.service';
import { CommonLookupModalComponent } from '@app/shared/common/lookup/common-lookup-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    EntityDtoOfInt64,
    FindUsersInput,
    FindUsersOutputDto,
    TenantListDto,
    TenantServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { CreateTenantModalComponent } from './create-tenant-modal.component';
import { EditTenantModalComponent } from './edit-tenant-modal.component';
import { TenantFeaturesModalComponent } from './tenant-features-modal.component';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { ColDef, GridApi, GetContextMenuItemsParams } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { EditionComboComponent } from '../shared/edition-combo.component';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { PaginationComponent } from '../shared/pagination.component';
import { CommonLookupModalComponent as CommonLookupModalComponent_1 } from '../../shared/common/lookup/common-lookup-modal.component';
import { EntityTypeHistoryModalComponent } from '../../shared/common/entityHistory/entity-type-history-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './tenants.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        SubHeaderComponent,
        FormsModule,
        EditionComboComponent,
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        AgGridAngular,
        PaginationComponent,
        CreateTenantModalComponent,
        EditTenantModalComponent,
        TenantFeaturesModalComponent,
        CommonLookupModalComponent_1,
        EntityTypeHistoryModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    schemas: [NO_ERRORS_SCHEMA],
})
export class TenantsComponent extends AppComponentBase implements OnInit {
    private _tenantService = inject(TenantServiceProxy);
    private cdr = inject(ChangeDetectorRef);
    private _activatedRoute = inject(ActivatedRoute);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    private _impersonationService = inject(ImpersonationService);
    private _dateTimeService = inject(DateTimeService);
    private _router = inject(Router);
    private zone = inject(NgZone);
    private gridApi!: GridApi;
    @ViewChild('impersonateUserLookupModal', { static: true }) impersonateUserLookupModal: CommonLookupModalComponent;
    @ViewChild('createTenantModal', { static: true }) createTenantModal: CreateTenantModalComponent;
    @ViewChild('editTenantModal', { static: true }) editTenantModal: EditTenantModalComponent;
    @ViewChild('tenantFeaturesModal', { static: true }) tenantFeaturesModal: TenantFeaturesModalComponent;
    subscriptionDateRange: DateTime[] = [];
    creationDateRange: DateTime[] = [];
    _entityTypeFullName = 'Erp.MultiTenancy.Tenant';
    entityHistoryEnabled = false;
    filters: {
        filterText: string;
        creationDateRangeActive: boolean;
        subscriptionEndDateRangeActive: boolean;
        selectedEditionId: number;
    } = <any>{};
    rowData: TenantListDto[] = [];
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        minWidth: 200,
    };
    getCustomContextMenuItems = (params: GetContextMenuItemsParams) => {
        const MenuItem = [];
        //permissions
        if (params.node.data.isActive) {
            MenuItem.push({
                name: this.l('LoginAsThisTenant'),
                action: () => {
                    this.zone.run(() => {
                        this.showUserImpersonateLookUpModal(params.node.data);
                    });
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            });
        }
        if (this.isGranted('Pages.Tenants.Edit')) {
            MenuItem.push({
                name: this.l('Edit'),
                action: () => {
                    this.zone.run(() => {
                        this.editTenantModal.show(params.node.data.id);
                    });
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            });
        }
        if (this.isGranted('Pages.Tenants.Delete')) {
            MenuItem.push({
                name: this.l('Delete'),
                action: () => {
                    this.zone.run(() => {
                        this.deleteTenant(params.node.data);
                    });
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            });
        }
        if (this.isGranted('Pages.Tenants.ChangeFeatures')) {
            MenuItem.push({
                name: this.l('Features'),
                action: () => {
                    this.zone.run(() => {
                        this.tenantFeaturesModal.show(params.node.data.id, params.node.data.name);
                    });
                },
                icon: '<span class="ag-icon ag-icon-excel"></span>',
            });
        }
        MenuItem.push({
            name: this.l('Unlock'),
            action: () => {
                this.zone.run(() => {
                    this.unlockUser(params.node.data);
                });
            },
            icon: '<span class="ag-icon ag-icon-excel"></span>',
        });
        return MenuItem;
    };
    public gridOptions = {
        rowSelection: {
            mode: 'singleRow',
            enableClickSelection: true,
        },
        getContextMenuItems: this.getCustomContextMenuItems,
    };
    public columnDefs: ColDef[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => params.node.rowIndex + 1,
            width: 80,
        },
        {
            field: 'tenancyName',
            headerName: this.l('TenancyName'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'name',
            headerName: this.l('Name'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'editionDisplayName',
            headerName: this.l('Edition'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'subscriptionEndDateUtc',
            headerName: this.l('SubscriptionEndDateUtc'),
            sortable: true,
            filter: true,
            width: 150,
            valueFormatter: (params) => this.formatTenantDate(params.value, 'yyyy-MM-dd HH:mm:ss'),
        },
        {
            field: 'isActive',
            headerName: this.l('Active'),
            sortable: true,
            filter: true,
            width: 150,
        },
        {
            field: 'creationTime',
            headerName: this.l('CreationTime'),
            sortable: true,
            filter: true,
            width: 150,
            valueFormatter: (params) => this.formatTenantDate(params.value, 'yyyy-MM-dd HH:mm:ss'),
        },
    ];
    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 100, // Set a minimum width for each column
        maxWidth: 300, // Set a maximum width for each column
    };
    constructor() {
        super();
        this.setFiltersFromRoute();
        this.subscriptionDateRange = [
            this._dateTimeService.getStartOfDay(),
            this._dateTimeService.getEndOfDayPlusDays(30),
        ];
        this.creationDateRange = [this._dateTimeService.getEndOfDayMinusDays(7), this._dateTimeService.getEndOfDay()];
    }
    onPageChange(page: number): void {
        this.currentPage = page;
        this.loadPage(page);
    }
    loadPage(page: number) {
        this._tenantService
            .getTenants(
                this.filters.filterText,
                this.filters.subscriptionEndDateRangeActive ? this.subscriptionDateRange[0] : undefined,
                this.filters.subscriptionEndDateRangeActive ? this.subscriptionDateRange[1].endOf('day') : undefined,
                this.filters.creationDateRangeActive ? this.creationDateRange[0] : undefined,
                this.filters.creationDateRangeActive ? this.creationDateRange[1].endOf('day') : undefined,
                this.filters.selectedEditionId,
                this.filters.selectedEditionId !== undefined && `${this.filters.selectedEditionId}` !== '-1',
                '',
                this.pageSize,
                page,
            )
            .subscribe((data) => {
                this.rowData = data.items;
                this.cdr.markForCheck();
                this.totalRecords = data.totalCount;
                this.calculatePageSizeOptions();
                if (this.gridApi) {
                    this.gridApi.setGridOption('rowData', this.rowData);
                }
            });
    }
    onPageSizeChange(newPageSize: number): void {
        this.pageSize = +newPageSize;
        this.currentPage = 0; // Reset to first page when page size changes
        this.loadPage(this.currentPage);
    }

    setFiltersFromRoute(): void {
        if (this._activatedRoute.snapshot.queryParams['subscriptionEndDateStart'] != null) {
            this.filters.subscriptionEndDateRangeActive = true;
            this.subscriptionDateRange[0] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['subscriptionEndDateStart'],
            );
        } else {
            this.subscriptionDateRange[0] = this._dateTimeService.getStartOfDay();
        }
        if (this._activatedRoute.snapshot.queryParams['subscriptionEndDateEnd'] != null) {
            this.filters.subscriptionEndDateRangeActive = true;
            this.subscriptionDateRange[1] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['subscriptionEndDateEnd'],
            );
        } else {
            this.subscriptionDateRange[1] = this._dateTimeService.getEndOfDayPlusDays(30);
        }
        if (this._activatedRoute.snapshot.queryParams['creationDateStart'] != null) {
            this.filters.creationDateRangeActive = true;
            this.creationDateRange[0] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['creationDateStart'],
            );
        } else {
            this.creationDateRange[0] = this._dateTimeService.getEndOfDayMinusDays(7);
        }
        if (this._activatedRoute.snapshot.queryParams['creationDateEnd'] != null) {
            this.filters.creationDateRangeActive = true;
            this.creationDateRange[1] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['creationDateEnd'],
            );
        } else {
            this.creationDateRange[1] = this._dateTimeService.getEndOfDay();
        }
        if (this._activatedRoute.snapshot.queryParams['editionId'] != null) {
            this.filters.selectedEditionId = parseInt(this._activatedRoute.snapshot.queryParams['editionId']);
        }
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.filters.filterText = this._activatedRoute.snapshot.queryParams['filterText'] || '';
        this.setIsEntityHistoryEnabled();
        this.impersonateUserLookupModal.configure({
            title: this.l('SelectAUser'),
            dataSource: (skipCount: number, maxResultCount: number, filter: string, tenantId?: number) => {
                const input = new FindUsersInput();
                input.filter = filter;
                input.maxResultCount = maxResultCount;
                input.skipCount = skipCount;
                input.tenantId = tenantId;
                return this._commonLookupService.findUsers(input);
            },
        });
        this.loadPage(this.currentPage);
    }
    showUserImpersonateLookUpModal(record: any): void {
        if (abp.multiTenancy.getTenantIdCookie()) {
            // remove this tenantId from the cookie
            abp.multiTenancy.setTenantIdCookie(null);
        }
        this.impersonateUserLookupModal.tenantId = record.id;
        this.impersonateUserLookupModal.show();
    }
    unlockUser(record: any): void {
        this._tenantService.unlockTenantAdmin(new EntityDtoOfInt64({ id: record.id })).subscribe(() => {
            this.notify.success(this.l('UnlockedTenandAdmin', record.name));
        });
    }
    createTenant(): void {
        this.createTenantModal.show();
    }
    deleteTenant(tenant: TenantListDto): void {
        this.blurActiveElement();
        this.message.confirm(
            this.l('TenantDeleteWarningMessage', tenant.tenancyName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._tenantService.deleteTenant(tenant.id).subscribe(() => {
                        this.loadPage(this.currentPage);
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            },
        );
    }
    showHistory(tenant: TenantListDto): void {
        this._router.navigate([`${abp.appPath}/app/admin/entity-changes/${tenant.id}/${this._entityTypeFullName}`]);
    }
    impersonateUser(item: FindUsersOutputDto): void {
        console.debug('TenantsComponent.impersonateUser called', {
            item,
            tenantId: this.impersonateUserLookupModal?.tenantId,
        });
        try {
            this.zone.run(() => {
                this._impersonationService.impersonateTenant(item.id, this.impersonateUserLookupModal.tenantId);
            });
        } catch (err) {
            console.error('impersonateUser failed', err);
        }
    }
    private setIsEntityHistoryEnabled(): void {
        const customSettings = (abp as any).custom;
        this.entityHistoryEnabled =
            customSettings.EntityHistory?.isEnabled &&
            customSettings.EntityHistory.enabledEntities.filter((entityType) => entityType === this._entityTypeFullName)
                .length === 1;
    }

    private formatTenantDate(value: DateTime | Date | string | undefined, format: string): string {
        if (!value) {
            return '';
        }

        if (typeof value === 'string') {
            return this._dateTimeService.formatISODateString(value, format);
        }

        return this._dateTimeService.formatDate(value, format);
    }

    private blurActiveElement(): void {
        const activeElement = document.activeElement;

        if (activeElement instanceof HTMLElement) {
            activeElement.blur();
        }
    }
}
