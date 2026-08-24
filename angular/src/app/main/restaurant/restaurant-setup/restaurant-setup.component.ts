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

    areaForm!: FormGroup;
    tableForm!: FormGroup;
    stationForm!: FormGroup;
    deviceForm!: FormGroup;
    settingsForm!: FormGroup;
    readonly negativeStockOptions = ['Allow', 'Warn', 'Block'];
    readonly tableWorkflowOptions = ['TableSession', 'PerOrder'];

    private fb = inject(FormBuilder);
    private restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private cdr = inject(ChangeDetectorRef);

    constructor() {
        super(inject(Injector));
        this.buildForms();
    }

    ngOnInit(): void {
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
}
