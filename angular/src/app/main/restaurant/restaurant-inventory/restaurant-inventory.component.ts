import { Component, Injector, OnInit, ViewEncapsulation, inject, ChangeDetectionStrategy } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRestaurantSupplierItemMappingDto,
    CreateRestaurantStockAdjustmentDto,
    CreateRestaurantStockAdjustmentLineDto,
    GenerateDraftPurchaseOrdersDto,
    RestaurantConsumptionLedgerDto,
    RestaurantInventoryServiceProxy,
    RestaurantLowStockSuggestionDto,
    RestaurantRecipeCoverageDto,
    RestaurantStockAdjustmentDto,
    RestaurantSupplierItemMappingDto,
    ReportingServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize, forkJoin } from 'rxjs';

type RestaurantInventoryTab = 'reorder' | 'consumption' | 'coverage' | 'mapping' | 'adjustment' | 'wastage';

interface DateRangeFilter {
    fromDate: string;
    toDate: string;
}

interface RestaurantConsumptionLedgerFilter {
    FromDate?: string;
    ToDate?: string;
    RawMaterialId?: string;
    MenuProductId?: string;
    OrderId?: string;
    SalesMasterId?: string;
    TableId?: string;
    WaiterUserId?: number;
    CategoryId?: string;
}

interface RestaurantSupplierItemMappingForm {
    id: string | undefined;
    productId: string;
    supplierLedgerId: string;
    supplierSku: string;
    unitId: string;
    rate: number;
    leadTimeDays: number;
    minimumOrderQty: number;
    isPreferred: boolean;
    isActive: boolean;
}

interface RestaurantStockAdjustmentLineForm {
    productId: string;
    unitId: string;
    qty: number;
    countedQty: number;
    rate: number;
    reason: string;
}

interface RestaurantStockAdjustmentForm {
    adjustmentType: number;
    dateMiti: string;
    description: string;
    lines: RestaurantStockAdjustmentLineForm[];
}

@Component({
    selector: 'restaurant-inventory',
    templateUrl: './restaurant-inventory.component.html',
    styleUrls: ['../restaurant-shared.css'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantInventoryComponent extends AppComponentBase implements OnInit {
    activeTab: RestaurantInventoryTab = 'reorder';
    loading = false;
    saving = false;
    rawMaterials: UniversalDropdownDto[] = [];
    suppliers: UniversalDropdownDto[] = [];
    units: UniversalDropdownDto[] = [];
    mappings: RestaurantSupplierItemMappingDto[] = [];
    lowStock: RestaurantLowStockSuggestionDto[] = [];
    adjustments: RestaurantStockAdjustmentDto[] = [];
    consumptionLedger: RestaurantConsumptionLedgerDto[] = [];
    recipeCoverage: RestaurantRecipeCoverageDto[] = [];

    mappingForm: RestaurantSupplierItemMappingForm = this.createEmptyMappingForm();
    adjustmentForm: RestaurantStockAdjustmentForm = this.createEmptyAdjustmentForm(1);
    wastageForm: RestaurantStockAdjustmentForm = this.createEmptyAdjustmentForm(3);
    adjustmentFilter: DateRangeFilter = { fromDate: '', toDate: '' };
    consumptionFilter: RestaurantConsumptionLedgerFilter = { FromDate: '', ToDate: '' };

    private inventoryService = inject(RestaurantInventoryServiceProxy);
    private restaurantReportingService = inject(ReportingServiceProxy);

    constructor() {
        super(inject(Injector));
    }

    ngOnInit(): void {
        this.loadLookups();
        this.refresh();
    }

    get missingMappingCount(): number {
        return this.lowStock.filter((item) => item.missingSupplierMapping).length;
    }

    get missingRecipeCount(): number {
        return this.recipeCoverage.filter((item) => item.missingRawMaterialSetup).length;
    }

    get consumptionAmount(): number {
        return this.consumptionLedger.reduce((sum, item) => sum + Number(item.amount || 0), 0);
    }

    loadLookups(): void {
        forkJoin({
            rawMaterials: this.inventoryService.getRawMaterials(),
            suppliers: this.restaurantReportingService.getAllSuppliersForTableDropdown(),
            units: this.inventoryService.getUnits(),
        }).subscribe((result) => {
            this.rawMaterials = result.rawMaterials || [];
            this.suppliers = result.suppliers || [];
            this.units = result.units || [];
        });
    }

    refresh(): void {
        this.loading = true;
        forkJoin({
            mappings: this.inventoryService.getSupplierItemMappings(undefined),
            lowStock: this.inventoryService.getLowStockSuggestions(),
            adjustments: this.inventoryService.getStockAdjustments(
                this.toDateTime(this.adjustmentFilter.fromDate),
                this.toDateTime(this.adjustmentFilter.toDate),
                undefined,
                undefined,
                undefined,
            ),
            consumptionLedger: this.inventoryService.getConsumptionLedger(
                this.consumptionFilter.RawMaterialId,
                this.consumptionFilter.MenuProductId,
                this.consumptionFilter.OrderId,
                this.consumptionFilter.SalesMasterId,
                this.toDateTime(this.consumptionFilter.FromDate),
                this.toDateTime(this.consumptionFilter.ToDate),
                this.consumptionFilter.TableId,
                this.consumptionFilter.WaiterUserId,
                this.consumptionFilter.CategoryId,
            ),
            recipeCoverage: this.inventoryService.getRecipeCoverage(undefined, undefined, undefined, undefined, undefined),
        })
            .pipe(finalize(() => (this.loading = false)))
            .subscribe((result) => {
                this.mappings = result.mappings || [];
                this.lowStock = result.lowStock || [];
                this.adjustments = result.adjustments || [];
                this.consumptionLedger = result.consumptionLedger || [];
                this.recipeCoverage = result.recipeCoverage || [];
            });
    }

    saveMapping(): void {
        if (!this.mappingForm.productId || !this.mappingForm.supplierLedgerId || !this.mappingForm.unitId) {
            this.notify.warn('Raw material, supplier, and unit are required');
            return;
        }

        this.saving = true;
        this.inventoryService
            .createOrEditSupplierItemMapping(new CreateOrEditRestaurantSupplierItemMappingDto(this.mappingForm))
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.mappingForm = this.createEmptyMappingForm();
                this.refresh();
            });
    }

    editMapping(mapping: RestaurantSupplierItemMappingDto): void {
        this.mappingForm = { ...mapping };
    }

    resetMappingForm(): void {
        this.mappingForm = this.createEmptyMappingForm();
    }

    deleteMapping(mapping: RestaurantSupplierItemMappingDto): void {
        this.inventoryService.deleteSupplierItemMapping(mapping.id).subscribe(() => {
            this.notify.success(this.l('SuccessfullyDeleted'));
            this.refresh();
        });
    }

    generateDraftOrders(): void {
        const productIds = this.lowStock
            .filter((item) => !item.missingSupplierMapping && item.suggestedQty > 0)
            .map((item) => item.productId);
        if (!productIds.length) {
            this.notify.warn('No mapped low-stock items to order');
            return;
        }

        this.saving = true;
        this.inventoryService
            .generateDraftPurchaseOrders(
                new GenerateDraftPurchaseOrdersDto({
                    productIds,
                    dateMiti: undefined,
                    dueDateMiti: undefined,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe((result) => {
                const count = result?.purchaseOrderIds?.length || 0;
                this.notify.success(count ? `${count} draft purchase order(s) created` : 'No purchase orders required');
                this.refresh();
            });
    }

    addAdjustmentLine(form: RestaurantStockAdjustmentForm): void {
        form.lines.push(this.createEmptyLine());
    }

    removeAdjustmentLine(form: RestaurantStockAdjustmentForm, index: number): void {
        form.lines.splice(index, 1);
    }

    saveAdjustment(form: RestaurantStockAdjustmentForm): void {
        if (!form.lines.length) {
            this.notify.warn('Add at least one line');
            return;
        }

        if (form.lines.some((line) => !line.productId || !line.unitId || Number(line.rate) < 0)) {
            this.notify.warn('Complete raw material, unit, and rate on every line');
            return;
        }

        this.saving = true;
        this.inventoryService
            .createStockAdjustment(this.toStockAdjustmentDto(form))
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                if (form.adjustmentType === 3) {
                    this.wastageForm = this.createEmptyAdjustmentForm(3);
                } else {
                    this.adjustmentForm = this.createEmptyAdjustmentForm(form.adjustmentType);
                }
                this.refresh();
            });
    }

    adjustmentTypeText(type: number): string {
        return ['Increase', 'Decrease', 'Physical Count', 'Wastage'][type] || 'Adjustment';
    }

    setTab(tab: RestaurantInventoryTab): void {
        this.activeTab = tab;
    }

    private createEmptyMappingForm(): RestaurantSupplierItemMappingForm {
        return {
            id: undefined,
            productId: '',
            supplierLedgerId: '',
            supplierSku: '',
            unitId: '',
            rate: 0,
            leadTimeDays: 0,
            minimumOrderQty: 0,
            isPreferred: true,
            isActive: true,
        };
    }

    private createEmptyAdjustmentForm(type: number): RestaurantStockAdjustmentForm {
        return {
            adjustmentType: type,
            dateMiti: '',
            description: '',
            lines: [this.createEmptyLine()],
        };
    }

    private createEmptyLine(): RestaurantStockAdjustmentLineForm {
        return {
            productId: '',
            unitId: '',
            qty: 1,
            countedQty: 0,
            rate: 0,
            reason: '',
        };
    }

    private toStockAdjustmentDto(form: RestaurantStockAdjustmentForm): CreateRestaurantStockAdjustmentDto {
        return new CreateRestaurantStockAdjustmentDto({
            adjustmentType: form.adjustmentType,
            dateMiti: form.dateMiti || undefined,
            description: form.description || undefined,
            lines: form.lines.map(
                (line) =>
                    new CreateRestaurantStockAdjustmentLineDto({
                        productId: line.productId,
                        unitId: line.unitId,
                        qty: Number(line.qty || 0),
                        countedQty: Number(line.countedQty || 0),
                        rate: Number(line.rate || 0),
                        reason: line.reason || undefined,
                    }),
            ),
        });
    }

    private toDateTime(value: string | undefined): DateTime | undefined {
        return value ? DateTime.fromISO(value) : undefined;
    }
}
