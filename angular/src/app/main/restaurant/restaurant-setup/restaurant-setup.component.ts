import {
    AfterViewInit,
    ChangeDetectorRef,
    Component,
    Injector,
    OnInit,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRestaurantAreaDto,
    CreateOrEditRestaurantStationDto,
    CreateOrEditRestaurantTableDto,
    RegisterRestaurantDeviceDto,
    RestaurantAreaDto,
    RestaurantDeviceDto,
    RestaurantOperationalSettingsDto,
    RestaurantSetupServiceProxy,
    RestaurantStationDto,
    RestaurantTableDto,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { CellClickedEvent, ColDef } from 'ag-grid-community';

type RestaurantSetupSection = 'areas' | 'tables' | 'stations' | 'devices' | 'settings';

@Component({
    selector: 'restaurant-setup',
    templateUrl: './restaurant-setup.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantSetupComponent extends AppComponentBase implements OnInit, AfterViewInit {
    areas: RestaurantAreaDto[] = [];
    tables: RestaurantTableDto[] = [];
    stations: RestaurantStationDto[] = [];
    devices: RestaurantDeviceDto[] = [];
    saving = false;
    loading = false;
    activeSection: RestaurantSetupSection = 'areas';
    returnToMenu = false;

    areaForm!: FormGroup;
    tableForm!: FormGroup;
    stationForm!: FormGroup;
    deviceForm!: FormGroup;
    settingsForm!: FormGroup;
    readonly negativeStockOptions = ['Allow', 'Warn', 'Block'];
    readonly tableWorkflowOptions = ['TableSession', 'PerOrder'];
    readonly defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 100,
    };
    areaColumnDefs: ColDef<RestaurantAreaDto>[] = [];
    tableColumnDefs: ColDef<RestaurantTableDto>[] = [];
    stationColumnDefs: ColDef<RestaurantStationDto>[] = [];
    deviceColumnDefs: ColDef<RestaurantDeviceDto>[] = [];
    noRowsOverlayTemplate = '';

    private fb = inject(FormBuilder);
    private restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private cdr = inject(ChangeDetectorRef);
    private route = inject(ActivatedRoute);

    constructor() {
        super(inject(Injector));
        this.buildForms();
        this.noRowsOverlayTemplate = `<span class="restaurant-grid-empty fw-bolder">${this.l('NoData')}</span>`;
        this.areaColumnDefs = this.createAreaColumnDefs();
        this.tableColumnDefs = this.createTableColumnDefs();
        this.stationColumnDefs = this.createStationColumnDefs();
        this.deviceColumnDefs = this.createDeviceColumnDefs();
    }

    ngOnInit(): void {
        this.returnToMenu = this.route.snapshot.queryParamMap.get('returnToMenu') === 'true';
        const requestedSection = this.route.snapshot.queryParamMap.get('section') as RestaurantSetupSection | null;
        if (requestedSection && ['areas', 'tables', 'stations', 'devices', 'settings'].includes(requestedSection)) {
            this.activeSection = requestedSection;
        }
        this.refresh();
    }

    ngAfterViewInit(): void {
        this.cdr.detectChanges();
    }

    refresh(): void {
        this.loading = true;
        this.restaurantSetupService.getAreas().subscribe((result) => {
            this.areas = result || [];
            if (!this.tableForm.get('areaId')?.value && this.areas.length) {
                this.tableForm.patchValue({ areaId: this.areas[0].id });
            }
            this.cdr.markForCheck();
        });
        this.restaurantSetupService.getTables(null).subscribe((result) => {
            this.tables = result || [];
            this.cdr.markForCheck();
        });
        this.restaurantSetupService.getStations().subscribe((result) => {
            this.stations = result || [];
            this.cdr.markForCheck();
        });
        this.restaurantSetupService.getOperationalSettings().subscribe((settings) => {
            if (settings) {
                this.settingsForm.patchValue(settings);
            }
            this.cdr.markForCheck();
        });
        this.restaurantSetupService
            .getDevices()
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((result) => {
                this.devices = result || [];
                this.cdr.markForCheck();
            });
    }

    saveArea(): void {
        if (this.areaForm.invalid) {
            this.areaForm.markAllAsTouched();
            return;
        }

        this.saving = true;
        this.restaurantSetupService
            .createOrEditArea(new CreateOrEditRestaurantAreaDto(this.areaForm.getRawValue()))
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetArea();
                this.refresh();
            });
    }

    saveOperationalSettings(): void {
        if (this.settingsForm.invalid) {
            this.settingsForm.markAllAsTouched();
            return;
        }

        this.saving = true;
        this.restaurantSetupService
            .updateOperationalSettings(new RestaurantOperationalSettingsDto(this.settingsForm.getRawValue()))
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh();
            });
    }

    saveTable(): void {
        if (this.tableForm.invalid) {
            this.tableForm.markAllAsTouched();
            return;
        }

        this.saving = true;
        this.restaurantSetupService
            .createOrEditTable(new CreateOrEditRestaurantTableDto(this.tableForm.getRawValue()))
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetTable();
                this.refresh();
            });
    }

    saveStation(): void {
        if (this.stationForm.invalid) {
            this.stationForm.markAllAsTouched();
            return;
        }

        this.saving = true;
        this.restaurantSetupService
            .createOrEditStation(new CreateOrEditRestaurantStationDto(this.stationForm.getRawValue()))
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetStation();
                this.refresh();
            });
    }

    registerDevice(): void {
        if (this.deviceForm.invalid) {
            this.deviceForm.markAllAsTouched();
            return;
        }

        this.saving = true;
        this.restaurantSetupService
            .registerDevice(new RegisterRestaurantDeviceDto(this.deviceForm.getRawValue()))
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetDevice();
                this.refresh();
            });
    }

    editArea(area: RestaurantAreaDto): void {
        this.activeSection = 'areas';
        this.areaForm.patchValue({ ...area });
    }

    editTable(table: RestaurantTableDto): void {
        this.activeSection = 'tables';
        this.tableForm.patchValue({ ...table });
    }

    onSetupGridCellClicked(event: CellClickedEvent): void {
        if (event.column.getColId() !== 'actions' || !event.data) {
            return;
        }

        switch (this.activeSection) {
            case 'areas':
                this.editArea(event.data as RestaurantAreaDto);
                break;
            case 'tables':
                this.editTable(event.data as RestaurantTableDto);
                break;
            case 'stations':
                this.editStation(event.data as RestaurantStationDto);
                break;
            case 'devices':
                this.editDevice(event.data as RestaurantDeviceDto);
                break;
        }
    }

    editStation(station: RestaurantStationDto): void {
        this.activeSection = 'stations';
        this.stationForm.patchValue({ ...station });
    }

    editDevice(device: RestaurantDeviceDto): void {
        this.activeSection = 'devices';
        this.deviceForm.patchValue({ ...device });
    }

    resetArea(): void {
        this.areaForm.reset({ id: undefined, name: '', description: '', sortOrder: 0, isActive: true });
    }

    resetTable(): void {
        this.tableForm.reset({
            id: undefined,
            name: '',
            code: '',
            capacity: 4,
            sortOrder: 0,
            status: 0,
            isActive: true,
            areaId: this.areas[0]?.id || '',
        });
    }

    resetStation(): void {
        this.stationForm.reset({ id: undefined, name: '', stationType: 0, isActive: true });
    }

    resetDevice(): void {
        this.deviceForm.reset({ id: undefined, deviceCode: '', name: '', userId: null });
    }

    isInvalid(form: FormGroup, controlName: string): boolean {
        const control = form.get(controlName);
        return !!control && control.invalid && (control.dirty || control.touched);
    }

    stationTypeText(type: number): string {
        return ['Kitchen', 'Bar', 'Counter', 'Other'][type] || 'Other';
    }

    tableStatusText(status: number): string {
        return ['Available', 'Occupied', 'Reserved', 'Maintenance'][status] || 'Available';
    }

    deviceStatusText(status: number): string {
        return status === 1 ? 'Blocked' : 'Active';
    }

    syncStatusText(device: RestaurantDeviceDto): string {
        if (device.hasConflict) {
            return 'Needs review';
        }
        if (device.lastSyncAt) {
            return 'Healthy';
        }
        return 'Waiting';
    }

    private finishLoading(): void {
        this.loading = false;
        this.cdr.markForCheck();
    }

    private finishSaving(): void {
        this.saving = false;
        this.cdr.markForCheck();
    }

    private buildForms(): void {
        this.areaForm = this.fb.group({
            id: [undefined],
            name: ['', Validators.required],
            description: [''],
            sortOrder: [0, [Validators.required, Validators.min(0)]],
            isActive: [true],
        });

        this.tableForm = this.fb.group({
            id: [undefined],
            name: ['', Validators.required],
            code: [''],
            capacity: [4, [Validators.required, Validators.min(1)]],
            sortOrder: [0, [Validators.required, Validators.min(0)]],
            status: [0, Validators.required],
            isActive: [true],
            areaId: ['', Validators.required],
        });

        this.stationForm = this.fb.group({
            id: [undefined],
            name: ['', Validators.required],
            stationType: [0, Validators.required],
            isActive: [true],
        });

        this.deviceForm = this.fb.group({
            id: [undefined],
            deviceCode: ['', Validators.required],
            name: ['', Validators.required],
            userId: [null],
        });

        this.settingsForm = this.fb.group({
            vatPercent: [13, [Validators.required, Validators.min(0), Validators.max(100)]],
            serviceChargePercent: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
            requireManagerPinForSensitiveActions: [false],
            managerPin: [''],
            negativeStockStatus: ['Warn', Validators.required],
            ticketPrintingEnabled: [true],
            channelAvailabilityEnabled: [true],
            tableWorkflow: ['TableSession', Validators.required],
        });
    }

    private createAreaColumnDefs(): ColDef<RestaurantAreaDto>[] {
        return [
            { headerName: this.l('Name'), field: 'name', flex: 1, minWidth: 140 },
            { headerName: this.l('Description'), field: 'description', flex: 1, minWidth: 160 },
            { headerName: this.l('Sort Order'), field: 'sortOrder', width: 130, filter: 'agNumberColumnFilter' },
            {
                headerName: this.l('Status'),
                field: 'isActive',
                width: 130,
                cellRenderer: (params) => this.statusBadge(params.value ? this.l('Active') : this.l('Inactive'), params.value ? 'success' : 'muted'),
            },
            this.actionColumn(),
        ];
    }

    private createTableColumnDefs(): ColDef<RestaurantTableDto>[] {
        return [
            { headerName: this.l('Table'), field: 'name', flex: 1, minWidth: 140 },
            { headerName: this.l('Area'), field: 'areaName', flex: 1, minWidth: 130 },
            { headerName: this.l('Capacity'), field: 'capacity', width: 120, filter: 'agNumberColumnFilter' },
            {
                headerName: this.l('Status'),
                field: 'status',
                width: 150,
                cellRenderer: (params) => this.statusBadge(this.tableStatusText(Number(params.value)), 'primary'),
            },
            this.actionColumn(),
        ];
    }

    private createStationColumnDefs(): ColDef<RestaurantStationDto>[] {
        return [
            { headerName: this.l('Station'), field: 'name', flex: 1, minWidth: 140 },
            {
                headerName: this.l('Type'),
                field: 'stationType',
                width: 150,
                valueFormatter: (params) => this.stationTypeText(Number(params.value)),
            },
            {
                headerName: this.l('Status'),
                field: 'isActive',
                width: 130,
                cellRenderer: (params) => this.statusBadge(params.value ? this.l('Active') : this.l('Inactive'), params.value ? 'success' : 'muted'),
            },
            this.actionColumn(),
        ];
    }

    private createDeviceColumnDefs(): ColDef<RestaurantDeviceDto>[] {
        return [
            { headerName: this.l('Device'), field: 'name', flex: 1, minWidth: 140 },
            { headerName: this.l('Code'), field: 'deviceCode', minWidth: 130 },
            { headerName: this.l('User'), field: 'userId', width: 100, valueFormatter: (params) => params.value || '-' },
            {
                headerName: this.l('Status'),
                field: 'status',
                width: 130,
                cellRenderer: (params) => this.statusBadge(this.deviceStatusText(Number(params.value)), Number(params.value) === 1 ? 'danger' : 'success'),
            },
            {
                headerName: this.l('Sync'),
                colId: 'syncStatus',
                width: 140,
                valueGetter: (params) => params.data ? this.syncStatusText(params.data) : '',
                cellRenderer: (params) => {
                    const tone = params.value === 'Needs review' ? 'danger' : params.value === 'Healthy' ? 'success' : 'warning';
                    return this.statusBadge(String(params.value || ''), tone);
                },
            },
            {
                headerName: this.l('Cursor'),
                colId: 'cursor',
                minWidth: 170,
                valueGetter: (params) =>
                    params.data
                        ? `${this.l('Pulled')}: ${params.data.lastPulledSeq || 0} · ${this.l('Ack')}: ${params.data.lastAcknowledgedSeq || 0}`
                        : '',
            },
            { headerName: this.l('Last Sync Error'), field: 'lastSyncError', minWidth: 180 },
            this.actionColumn(),
        ];
    }

    private actionColumn(): ColDef {
        return {
            colId: 'actions',
            headerName: '',
            width: 76,
            minWidth: 76,
            maxWidth: 76,
            sortable: false,
            filter: false,
            resizable: false,
            cellClass: 'text-end',
            cellRenderer: () =>
                `<button type="button" class="btn btn-xs btn-light-primary align-items-center d-inline-flex fs-9 justify-content-center" aria-label="${this.l('Edit')}" title="${this.l('Edit')}"><i class="fa fa-pencil"></i></button>`,
        };
    }

    private statusBadge(label: string, tone: 'success' | 'warning' | 'danger' | 'primary' | 'muted'): string {
        const classes: Record<typeof tone, string> = {
            success: 'bg-light-success text-success',
            warning: 'bg-light-warning text-warning',
            danger: 'bg-light-danger text-danger',
            primary: 'bg-light-primary text-primary',
            muted: 'bg-light text-gray-600',
        };
        return `<span class="restaurant-status fs-9 min-h-20px fw-bold gap-1 px-2 py-1 rounded-2 align-items-center d-inline-flex lh-1 mw-100 text-nowrap ${classes[tone]}">${label}</span>`;
    }
}
