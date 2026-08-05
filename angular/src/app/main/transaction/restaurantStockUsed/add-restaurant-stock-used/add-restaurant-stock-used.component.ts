import { Location } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    Injector,
    OnDestroy,
    OnInit,
    ViewEncapsulation,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateRestaurantStockAdjustmentDto,
    CreateRestaurantStockAdjustmentLineDto,
    RestaurantInventoryServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { finalize, forkJoin } from 'rxjs';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    templateUrl: './add-restaurant-stock-used.component.html',
    styleUrls: ['./add-restaurant-stock-used.component.css'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
})
export class AddRestaurantStockUsedComponent extends AppComponentBase implements OnInit, OnDestroy {
    title = 'Add Restaurant Stock Used';
    form: FormGroup;
    saving = false;
    loadingLookups = false;
    rawMaterials: UniversalDropdownDto[] = [];
    units: UniversalDropdownDto[] = [];
    shortcuts: ShortcutInput[] = [];

    private readonly stockUsedAdjustmentType = 1;

    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private inventoryService: RestaurantInventoryServiceProxy,
        private location: Location,
    ) {
        super(injector);
        this.getSetting();
        this.createForm();
    }

    get linesArray(): FormArray {
        return this.form.get('lines') as FormArray;
    }

    get lineControls(): FormGroup[] {
        return this.linesArray.controls as FormGroup[];
    }

    get totalAmount(): number {
        return this.lineControls.reduce((sum, line) => sum + this.getLineAmount(line), 0);
    }

    get totalQty(): number {
        return this.lineControls.reduce((sum, line) => sum + Number(line.get('qty')?.value || 0), 0);
    }

    ngOnInit(): void {
        this.initShortcuts();
        this.loadLookups();
    }

    override ngOnDestroy(): void {
        super.ngOnDestroy();
    }

    createForm(): void {
        this.form = this.fb.group({
            adjustmentType: [this.stockUsedAdjustmentType],
            voucherNo: [{ value: 'Auto', disabled: true }],
            dateMiti: [this.today, Validators.required],
            description: [''],
            lines: this.fb.array([this.createLine()]),
        });
    }

    initShortcuts(): void {
        this.shortcuts = [
            {
                key: ['alt + s'],
                label: 'Save',
                description: 'Save stock used',
                command: () => this.save(),
                preventDefault: true,
            },
            {
                key: ['ctrl + n'],
                label: 'New Detail',
                description: 'Add new detail row',
                command: () => this.addLine(),
                preventDefault: true,
            },
            {
                key: ['alt + c'],
                label: 'Close',
                description: 'Close form',
                command: () => this.close(),
                preventDefault: true,
            },
        ];
    }

    loadLookups(): void {
        this.loadingLookups = true;
        forkJoin({
            rawMaterials: this.inventoryService.getRawMaterials(),
            units: this.inventoryService.getUnits(),
        })
            .pipe(finalize(() => {
                this.loadingLookups = false;
                this.markViewForCheck();
            }))
            .subscribe((result) => {
                this.rawMaterials = result.rawMaterials || [];
                this.units = result.units || [];
                this.markViewForCheck();
            });
    }

    addLine(): void {
        if (this.linesArray.invalid) {
            this.linesArray.markAllAsTouched();
            this.notify.warn('Complete current stock used line before adding another');
            return;
        }

        this.linesArray.push(this.createLine());
        this.markViewForCheck();
    }

    removeLine(index: number): void {
        if (this.linesArray.length <= 1) {
            this.notify.warn('At least one stock used line is required');
            return;
        }

        this.linesArray.removeAt(index);
        this.markViewForCheck();
    }

    getLineAmount(line: FormGroup): number {
        const qty = Number(line.get('qty')?.value || 0);
        const rate = Number(line.get('rate')?.value || 0);
        return qty * rate;
    }

    updateLineAmount(line: FormGroup): void {
        line.get('amount')?.setValue(this.getLineAmount(line), { emitEvent: false });
    }

    save(): void {
        this.lineControls.forEach((line) => this.updateLineAmount(line));

        if (!this.linesArray.length) {
            this.notify.warn('Add at least one line');
            return;
        }

        if (this.form.invalid) {
            this.form.markAllAsTouched();
            this.notify.error('Form is invalid !!');
            return;
        }

        this.saving = true;
        this.inventoryService
            .createStockAdjustment(this.toPayload())
            .pipe(finalize(() => {
                this.saving = false;
                this.markViewForCheck();
            }))
            .subscribe(() => {
                this.notify.success(this.l('Saved Successfully'));
                this.location.back();
            });
    }

    close(): void {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.location.back();
                }
            });
        } else {
            this.location.back();
        }
    }

    isInvalid(line: FormGroup, controlName: string): boolean {
        const control = line.get(controlName);
        return !!control && control.invalid && (control.dirty || control.touched);
    }

    private createLine(item: any = {}): FormGroup {
        const line = this.fb.group({
            productId: [item.productId || '', Validators.required],
            unitId: [item.unitId || '', Validators.required],
            qty: [item.qty || 1, [Validators.required, Validators.min(0.000001)]],
            rate: [item.rate || 0, [Validators.required, Validators.min(0)]],
            amount: [{ value: item.amount || 0, disabled: true }],
            reason: [item.reason || ''],
        });

        line.get('qty')?.valueChanges.subscribe(() => this.updateLineAmount(line));
        line.get('rate')?.valueChanges.subscribe(() => this.updateLineAmount(line));
        this.updateLineAmount(line);

        return line;
    }

    private toPayload(): CreateRestaurantStockAdjustmentDto {
        return new CreateRestaurantStockAdjustmentDto({
            adjustmentType: this.stockUsedAdjustmentType,
            dateMiti: this.form.get('dateMiti')?.value,
            description: this.form.get('description')?.value || '',
            lines: this.lineControls.map(
                (line) =>
                    new CreateRestaurantStockAdjustmentLineDto({
                        productId: line.get('productId')?.value,
                        unitId: line.get('unitId')?.value,
                        qty: Number(line.get('qty')?.value || 0),
                        countedQty: undefined,
                        rate: Number(line.get('rate')?.value || 0),
                        reason: line.get('reason')?.value || '',
                    }),
            ),
        });
    }
}
