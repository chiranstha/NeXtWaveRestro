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
import { CellClickedEvent, ColDef } from 'ag-grid-community';

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
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantInventoryComponent extends AppComponentBase implements OnInit {
    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        suppressHeaderFilterButton: false,
        resizable: true,
        minWidth: 120,
    };
    lowStockColumnDefs: ColDef[] = [
        { field: 'productName', headerName: this.l('Raw Material'), minWidth: 180, flex: 2 },
        {
            field: 'availableQty',
            headerName: this.l('Available'),
            type: 'rightAligned',
            valueFormatter: (params) => `${this.formatQuantity(params.value)} ${params.data?.unitName || ''}`.trim(),
        },
        {
            headerName: this.l('Min / Max'),
            type: 'rightAligned',
            valueGetter: (params) => `${this.formatQuantity(params.data?.minimumStock)} / ${this.formatQuantity(params.data?.maximumStock)}`,
        },
        {
            field: 'pendingPurchaseQty',
            headerName: this.l('Pending PO'),
            type: 'rightAligned',
            valueFormatter: (params) => this.formatQuantity(params.value),
        },
        { field: 'supplierName', headerName: this.l('Supplier'), valueFormatter: (params) => params.value || '-' },
        { field: 'supplierSku', headerName: this.l('Supplier SKU'), valueFormatter: (params) => params.value || '-' },
        {
            field: 'suggestedQty',
            headerName: this.l('Suggested Qty'),
            type: 'rightAligned',
            valueFormatter: (params) => `${this.formatQuantity(params.value)} ${params.data?.unitName || ''}`.trim(),
        },
        {
            field: 'amount',
            headerName: this.l('Amount'),
            type: 'rightAligned',
            valueFormatter: (params) => this.formatAmount(params.value),
        },
        {
            field: 'missingSupplierMapping',
            headerName: this.l('Mapping'),
            valueFormatter: (params) => (params.value ? this.l('Mapping Missing') : this.l('Ready')),
        },
    ];
    consumptionColumnDefs: ColDef[] = [
        {
            headerName: this.l('Date'),
            minWidth: 160,
            valueGetter: (params) => {
                const date = params.data?.date;
                const gregorianDate = date?.toFormat ? date.toFormat('yyyy-MM-dd') : '';
                return [gregorianDate, params.data?.dateMiti].filter(Boolean).join(' · ') || '-';
            },
        },
        {
            headerName: this.l('Bill'),
            minWidth: 160,
            valueGetter: (params) => {
                const row = params.data;
                const bill = row?.salesVoucherNo || row?.voucherNo || '-';
                return row?.salesVoucherNo && row?.voucherNo ? `${bill} · ${this.l('Consumption')}: ${row.voucherNo}` : bill;
            },
        },
        { field: 'orderNo', headerName: this.l('Order') },
        {
            headerName: this.l('Menu Item'),
            minWidth: 170,
            valueGetter: (params) => {
                const row = params.data;
                return row?.menuItemName && row.menuItemName !== row.menuProductName
                    ? `${row.menuItemName} · ${row.menuProductName}`
                    : row?.menuItemName || row?.menuProductName || '-';
            },
        },
        { field: 'rawMaterialName', headerName: this.l('Raw Material'), minWidth: 170 },
        {
            field: 'qty',
            headerName: this.l('Qty'),
            type: 'rightAligned',
            valueFormatter: (params) => `${this.formatQuantity(params.value)} ${params.data?.unitName || ''}`.trim(),
        },
        { field: 'rate', headerName: this.l('Rate'), type: 'rightAligned', valueFormatter: (params) => this.formatAmount(params.value) },
        { field: 'amount', headerName: this.l('Amount'), type: 'rightAligned', valueFormatter: (params) => this.formatAmount(params.value) },
    ];
    recipeCoverageColumnDefs: ColDef[] = [
        { field: 'productName', headerName: this.l('Menu Item'), minWidth: 180, flex: 2 },
        { field: 'categoryName', headerName: this.l('Category'), valueFormatter: (params) => params.value || '-' },
        {
            field: 'hasRecipe',
            headerName: this.l('Recipe'),
            valueFormatter: (params) => (params.value ? this.l('BOM active') : this.l('No BOM')),
        },
        { field: 'activeRecipeLineCount', headerName: this.l('Lines'), type: 'rightAligned' },
        {
            field: 'estimatedRecipeCost',
            headerName: this.l('Recipe Cost'),
            type: 'rightAligned',
            valueFormatter: (params) => this.formatAmount(params.value),
        },
        { field: 'menuPrice', headerName: this.l('Menu Price'), type: 'rightAligned', valueFormatter: (params) => this.formatAmount(params.value) },
        {
            field: 'foodCostPercent',
            headerName: this.l('Food Cost %'),
            type: 'rightAligned',
            valueFormatter: (params) => `${this.formatAmount(params.value)}%`,
        },
        {
            field: 'missingRawMaterialSetup',
            headerName: this.l('Status'),
            valueFormatter: (params) => (params.value ? this.l('Missing Recipe') : this.l('Ready')),
        },
    ];
    mappingColumnDefs: ColDef[] = [
        { field: 'productName', headerName: this.l('Raw Material'), minWidth: 180, flex: 2 },
        { field: 'supplierSku', headerName: this.l('Supplier SKU'), valueFormatter: (params) => params.value || '-' },
        { field: 'supplierName', headerName: this.l('Supplier'), minWidth: 160 },
        { field: 'unitName', headerName: this.l('Unit') },
        { field: 'rate', headerName: this.l('Rate'), type: 'rightAligned', valueFormatter: (params) => this.formatAmount(params.value) },
        {
            headerName: this.l('Status'),
            valueGetter: (params) => {
                const labels = [];
                if (params.data?.isPreferred) labels.push(this.l('Preferred'));
                labels.push(params.data?.isActive ? this.l('Active') : this.l('Inactive'));
                return labels.join(' · ');
            },
        },
        {
            colId: 'actions',
            headerName: this.l('Actions'),
            sortable: false,
            filter: false,
            resizable: false,
            width: 105,
            minWidth: 105,
            maxWidth: 105,
            cellRenderer: () =>
                '<button type="button" class="btn btn-xs btn-light-primary me-1" data-action="edit" aria-label="Edit"><i class="fa fa-pencil"></i></button>' +
                '<button type="button" class="btn btn-xs btn-light-danger" data-action="delete" aria-label="Delete"><i class="fa fa-trash"></i></button>',
        },
    ];
    adjustmentColumnDefs: ColDef[] = [
        { field: 'voucherNo', headerName: this.l('Voucher'), valueFormatter: (params) => params.value || '-' },
        { field: 'dateMiti', headerName: this.l('Date'), valueFormatter: (params) => params.value || '-' },
        { field: 'adjustmentType', headerName: this.l('Type'), valueFormatter: (params) => this.adjustmentTypeText(params.value) },
        { field: 'createUserName', headerName: this.l('User'), valueFormatter: (params) => params.value || '-' },
        {
            colId: 'lineCount',
            headerName: this.l('Lines'),
            type: 'rightAligned',
            valueGetter: (params) => params.data?.lines?.length || 0,
        },
        { field: 'totalAmount', headerName: this.l('Amount'), type: 'rightAligned', valueFormatter: (params) => this.formatAmount(params.value) },
    ];
    adjustmentLineColumnDefs: ColDef<RestaurantStockAdjustmentLineForm>[] = [];
    wastageLineColumnDefs: ColDef<RestaurantStockAdjustmentLineForm>[] = [];
    readonly adjustmentLineDefaultColDef: ColDef = {
        editable: true,
        resizable: true,
        sortable: false,
        filter: false,
        minWidth: 130,
    };

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
        this.adjustmentLineColumnDefs = this.createAdjustmentLineColumnDefs();
        this.wastageLineColumnDefs = this.createWastageLineColumnDefs();
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
        form.lines = [...form.lines, this.createEmptyLine()];
    }

    removeAdjustmentLine(form: RestaurantStockAdjustmentForm, index: number): void {
        form.lines = form.lines.filter((_, lineIndex) => lineIndex !== index);
    }

    onAdjustmentTypeChanged(adjustmentType: number): void {
        this.adjustmentForm.adjustmentType = Number(adjustmentType);
        this.adjustmentLineColumnDefs = this.createAdjustmentLineColumnDefs();
    }

    onAdjustmentLineGridCellClicked(
        event: CellClickedEvent<RestaurantStockAdjustmentLineForm>,
        form: RestaurantStockAdjustmentForm,
    ): void {
        if (event.column.getColId() !== 'actions' || !event.data) {
            return;
        }

        const target = event.event?.target as HTMLElement | null;
        if (target?.closest('[data-action="remove"]')) {
            this.removeAdjustmentLine(form, form.lines.indexOf(event.data));
        }
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

    onMappingGridCellClicked(params: CellClickedEvent<RestaurantSupplierItemMappingDto>): void {
        if (params.column.getColId() !== 'actions' || !params.data) {
            return;
        }

        const target = params.event?.target as HTMLElement | null;
        const action = target?.closest<HTMLElement>('[data-action]')?.dataset.action;
        if (action === 'edit') {
            this.editMapping(params.data);
        } else if (action === 'delete') {
            this.deleteMapping(params.data);
        }
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

    private createAdjustmentLineColumnDefs(): ColDef<RestaurantStockAdjustmentLineForm>[] {
        return [
            this.adjustmentMaterialColumn(),
            this.adjustmentUnitColumn(),
            this.adjustmentNumberColumn('qty', 'Qty'),
            {
                ...this.adjustmentNumberColumn('countedQty', 'Counted Qty'),
                hide: this.adjustmentForm.adjustmentType !== 2,
            },
            this.adjustmentNumberColumn('rate', 'Rate'),
            { field: 'reason', headerName: this.l('Reason'), minWidth: 180 },
            this.adjustmentRemoveColumn(),
        ];
    }

    private createWastageLineColumnDefs(): ColDef<RestaurantStockAdjustmentLineForm>[] {
        return [
            this.adjustmentMaterialColumn(),
            this.adjustmentUnitColumn(),
            this.adjustmentNumberColumn('qty', 'Qty'),
            this.adjustmentNumberColumn('rate', 'Rate'),
            { field: 'reason', headerName: this.l('Reason'), minWidth: 180 },
            this.adjustmentRemoveColumn(),
        ];
    }

    private adjustmentMaterialColumn(): ColDef<RestaurantStockAdjustmentLineForm> {
        return {
            field: 'productId',
            headerName: this.l('Raw Material'),
            minWidth: 220,
            flex: 1,
            cellEditor: 'agSelectCellEditor',
            cellEditorParams: () => ({ values: this.rawMaterials.map((item) => item.id) }),
            valueFormatter: (params) => this.rawMaterials.find((item) => item.id === params.value)?.displayName || '',
        };
    }

    private adjustmentUnitColumn(): ColDef<RestaurantStockAdjustmentLineForm> {
        return {
            field: 'unitId',
            headerName: this.l('Unit'),
            minWidth: 160,
            cellEditor: 'agSelectCellEditor',
            cellEditorParams: () => ({ values: this.units.map((unit) => unit.id) }),
            valueFormatter: (params) => this.units.find((unit) => unit.id === params.value)?.displayName || '',
        };
    }

    private adjustmentNumberColumn(
        field: 'qty' | 'countedQty' | 'rate',
        label: string,
    ): ColDef<RestaurantStockAdjustmentLineForm> {
        return {
            field,
            headerName: this.l(label),
            type: 'rightAligned',
            minWidth: 125,
            cellEditor: 'agNumberCellEditor',
            valueParser: (params) => Number(params.newValue || 0),
        };
    }

    private adjustmentRemoveColumn(): ColDef<RestaurantStockAdjustmentLineForm> {
        return {
            colId: 'actions',
            headerName: '',
            width: 64,
            minWidth: 64,
            maxWidth: 64,
            editable: false,
            resizable: false,
            cellClass: 'text-end',
            cellRenderer: () =>
                `<button type="button" class="btn btn-xs btn-light-danger align-items-center d-inline-flex fs-9 justify-content-center" data-action="remove" aria-label="${this.l('Remove')}" title="${this.l('Remove')}"><i class="fa fa-trash"></i></button>`,
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

    private formatQuantity(value: unknown): string {
        return Number(value || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 3 });
    }

    private formatAmount(value: unknown): string {
        return Number(value || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
}
