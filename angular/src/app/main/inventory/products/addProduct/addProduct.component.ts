import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    EventEmitter,
    inject,
    Input,
    Injector,
    OnInit,
    OnDestroy,
    Output,
    QueryList,
    TemplateRef,
    ViewChild,
    ViewChildren,
} from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { finalize } from 'rxjs/operators';
import { Location } from '@angular/common';
import { ProductServiceProxy, ProductTaxDto, ProductTypeEnum, UniversalDropdownDto } from '@shared/service-proxies/service-proxies';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NgSelectComponent } from '@ng-select/ng-select';
import { Observable, Subscription, forkJoin } from 'rxjs';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { appModuleAnimation } from '@shared/animations/routerTransition';

type ProductTypeSelection = ProductTypeEnum | 'kitchenItem' | 'stockItem' | 'rawMaterial';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-product',
    templateUrl: './addProduct.component.html',
    styleUrls: ['./addProduct.component.css'],
    animations: [appModuleAnimation]

})
export class AddProductComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly _router = inject(Router);

    @Input() serialNo: number;
    @Output() productSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('productGroup') productGroupEvent: NgSelectComponent;
    @ViewChild('productTypes') productTypeEvent: NgSelectComponent;
    @ViewChild('productCodes', { read: ElementRef }) productCodeEvent: ElementRef;
    @ViewChild('productName', { read: ElementRef }) productNameEvent: ElementRef;
    @ViewChild('units') unitsEvent: NgSelectComponent;
    @ViewChild('purchaseRates', { read: ElementRef }) purchaseRateEvent: ElementRef;
    @ViewChild('margin', { read: ElementRef }) marginEvent: ElementRef;
    @ViewChild('salesRates', { read: ElementRef }) salesRateEvent: ElementRef;
    @ViewChild('mrps', { read: ElementRef }) mrpEvent: ElementRef;
    @ViewChild('reOrder', { read: ElementRef }) reOrderEvent: ElementRef;
    @ViewChild('hsCode', { read: ElementRef }) hsCodeEvent: ElementRef;
    @ViewChild('description', { read: ElementRef }) descriptionEvent: ElementRef;
    @ViewChild('minimiumStock', { read: ElementRef }) miniStockEvent: ElementRef;
    @ViewChild('maximumStocks', { read: ElementRef }) maxStockEvent: ElementRef;
    @ViewChild('tax') taxEvent: NgSelectComponent;
    //openning stock
    @ViewChildren('opsBranch') opsBranchEvent: QueryList<NgSelectComponent>;
    @ViewChildren('opsQty', { read: ElementRef }) opsQtyEvent: QueryList<ElementRef>;
    @ViewChildren('oppsUnit') oppsUnitEvent: QueryList<NgSelectComponent>;
    @ViewChildren('opsRate', { read: ElementRef }) opsRateEvent: QueryList<ElementRef>;
    @ViewChildren('opsGodOwn') opsGodOwnEvent: QueryList<NgSelectComponent>;
    @ViewChildren('opsRack') opsRackEvent: QueryList<NgSelectComponent>;
    @ViewChild('confirmationDialog') confirmationDialog?: TemplateRef<any>;

    form: FormGroup;
    modalRef?: BsModalRef;
    id: string;
    unitId: string;
    active = false;
    saving = false;
    expanded = false;
    expanded1 = false;
    price: number;
    title = 'Create Product';
    returnToMenu = false;
    allProductGoups: UniversalDropdownDto[];
    allUnits: UniversalDropdownDto[];
    allUnitsOpeningStock: UniversalDropdownDto[];
    allProducts: UniversalDropdownDto[];
    allHscodes: string[];
    hsCodeMatched = false;
    allUnit: UniversalDropdownDto[];
    allTaxes: ProductTaxDto[];
    allProductType = [
        { displayName: 'Kitchen Item', id: ProductTypeEnum.KitchenItem },
        { displayName: 'Stock Item', id: ProductTypeEnum.Product },
        { displayName: 'Raw Material', id: ProductTypeEnum.RawMaterial },
    ];
    radioInput = [
        { name: 'Paid Sim', value: 0 },
        { name: 'Free Sim', value: 1 },
    ];
    unitSubscription: Subscription;
    shortcuts: ShortcutInput[] = [];
    productForInput: UniversalDropdownDto[];
    opsI: number;
    rowid: number;
    // Add a subscription collection for cleanup
    private subscriptions: Subscription[] = [];
    // Cache unit data to avoid repeated API calls
    private unitDataCache = new Map<string, UniversalDropdownDto[]>();
    // Cache for API responses to reduce network calls
    private apiCache = new Map<string, any>();
    // Performance optimization flags
    private isLoadingData = false;
    private dataLoaded = {
        productGroups: false,
        units: false,
        taxes: false
    };

    constructor(
        private _formBuilder: FormBuilder,
        public injector: Injector,
        private route: ActivatedRoute,
        private _proxy: ProductServiceProxy,
        private _location: Location,
        private modalService: BsModalService
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnDestroy(): void {
        // Cleanup all subscriptions to prevent memory leaks
        this.subscriptions.forEach(sub => {
            if (sub && !sub.closed) {
                sub.unsubscribe();
            }
        });
        this.subscriptions = [];

        // Clear caches to free memory
        this.unitDataCache.clear();
        this.apiCache.clear();

        // Close any open modals
        if (this.modalRef) {
            this.modalRef.hide();
        }
    }

    get openingStock() {
        return this.form.get('openingStock') as FormArray;
    }

    // Memoized getters for better performance
    private _cachedOpeningStockValid: boolean | null = null;
    private _lastFormCheck: number = 0;


    get openingStockValid() {
        const now = Date.now();
        if (this._cachedOpeningStockValid !== null && (now - this._lastFormCheck) < 100) {
            return this._cachedOpeningStockValid;
        }

        if (this.openingStock.length === 0) {
            this._cachedOpeningStockValid = true;
        } else {
            this._cachedOpeningStockValid = this.openingStock.controls.every(control => {
                return control.valid &&
                    control.get('branchId')?.value &&
                    (control.get('openingQty')?.value || 0) > 0 &&
                    control.get('unitId')?.value;
            });
        }

        this._lastFormCheck = now;
        return this._cachedOpeningStockValid;
    }

    // Method to clear validation cache when form changes
    private clearValidationCache(): void {
        this._cachedOpeningStockValid = null;
    }


    ngOnInit(): void {
        this.createForm();
        this.id = this.route.snapshot.params['pid'];
        this.returnToMenu = this.route.snapshot.queryParamMap.get('returnToMenu') === 'true';
        if (!this.id && this.route.snapshot.queryParamMap.get('productType') === 'rawMaterial') {
            this.form.get('productType')?.setValue(ProductTypeEnum.RawMaterial);
            this.onChangeType(ProductTypeEnum.RawMaterial);
        }

        // Initialize collections to prevent undefined errors
        this.allUnits = [];
        this.allUnitsOpeningStock = [];
        // Load essential data first, then secondary data
        this.loadEssentialData();

    }

    // Optimized data loading with batching and prioritization
    private loadEssentialData(): void {
        if (this.isLoadingData) {return;}
        this.isLoadingData = true;

        // Handle edit mode or new product creation
        if (this.id) {
            this.loadProductForEdit();
        } else {
            this.price = 0;
            // Get product code only if creating new product
            this.subscriptions.push(
                this._proxy.getProductCode().subscribe(result => {
                    this.form.get('productCode')?.setValue(result);
                })
            );
        }

        // Load form dropdown data with caching
        this.loadFormDropdownData();

        this.isLoadingData = false;
    }

    // Batch load dropdown data to reduce API calls
    private loadFormDropdownData(): void {
        const requests: Observable<any>[] = [];

        if (!this.dataLoaded.productGroups) {
            requests.push(this._proxy.getAllProductGroupsForTableDropdown());
        }

        if (!this.dataLoaded.units) {
            requests.push(this._proxy.getAllUnitForTableDropdown());
        }
        if (!this.dataLoaded.taxes) {
            requests.push(this._proxy.getAllTaxForTableDropdown());
        }


        if (requests.length > 0) {
            this.subscriptions.push(
                forkJoin(requests).subscribe(results => {
                    let index = 0;
                    if (!this.dataLoaded.productGroups) {
                        this.allProductGoups = results[index++];
                        this.dataLoaded.productGroups = true;
                        if (!this.id && this.allProductGoups?.length > 0) {
                            this.form.get('productGroupId')?.setValue(this.allProductGoups[0].id);
                        }
                    }
                    if (!this.dataLoaded.units) {
                        this.allUnits = results[index++];
                        this.dataLoaded.units = true;
                        if (!this.id && this.allUnits?.length > 0) {
                            this.unitId = this.allUnits[0].id;
                            this.form.get('unitId')?.setValue(this.allUnits[0].id);
                        }
                    }
                    if (!this.dataLoaded.taxes) {
                        this.allTaxes = results[index++];
                        this.dataLoaded.taxes = true;
                        if (!this.id && this.allTaxes?.length > 0) {
                            this.form.get('taxId')?.setValue(this.allTaxes[0].taxId);
                        }
                    }
                })
            );
        }
    }


    loadProductForEdit(): void {
        this.title = 'Edit Product';
        this.subscriptions.push(
            this._proxy.getProductForEdit(this.id).subscribe(result => {
                this.price = result.mrp;
                this.createForm(result);

                // Load units data only once

                this.subscriptions.push(
                    this._proxy.getAllUnitForTableDropdown().subscribe(data => {

                        this.allUnitsOpeningStock = data.filter(
                            unit => unit.id == result.unitId
                        );

                        this.openingStock.controls.forEach(control => {
                            control.get('unitId')?.setValue(result.unitId);
                        });
                    })
                );
                // Set initial unit ID from the result or use cached value
                this.unitId = result.unitId;
                this.onChangeType(result.productType);
            })
        );
    }

    getAllProductForName(): void {
        const cacheKey = 'product_names';
        if (this.apiCache.has(cacheKey)) {
            this.productForInput = this.apiCache.get(cacheKey);
            return;
        }

        this.subscriptions.push(
            this._proxy.getAllProductForTableDropdown().subscribe(data => {
                this.productForInput = data;
                this.apiCache.set(cacheKey, data);
            })
        );
    }



    getAllHsCodes(): void {
        const cacheKey = 'hs_codes';
        if (this.apiCache.has(cacheKey)) {
            this.allHscodes = this.apiCache.get(cacheKey);
            return;
        }

        this.subscriptions.push(
            this._proxy.getAllHsCodes().subscribe(data => {
                this.allHscodes = data;
                this.apiCache.set(cacheKey, data);
            })
        );
    }

    //openning stock
    opsGotoQty(e, i) {
        if (e.which == 13) {
            e.preventDefault();
            this.opsBranchEvent.toArray()[i].close();

            this.opsQtyEvent.toArray()[i].nativeElement.focus();

        }
    }

    opsGotoUnit(e, i) {
        if (e.which == 13) {
            e.preventDefault();
            this.oppsUnitEvent.toArray()[i].open();
        }
    }

    opsGotoRate(e, i) {
        if (e.which == 13) {
            e.preventDefault();
            this.oppsUnitEvent.toArray()[i].close();
            this.opsRateEvent.toArray()[i].nativeElement.focus();
        }
    }

    opsGotoGodOwn(e, i) {
        if (e.which == 13) {
            e.preventDefault();
            this.opsGodOwnEvent?.toArray()[i]?.open();
        }
    }

    //Enter event
    gotoProductGroup() {
        this.productTypeEvent.close();
        this.productGroupEvent.open();
    }

    gotoProductCode(e) {
        if (e.altKey && e.which == 67) {
            e.preventDefault();
        } else if (e.which == 13) {
            e.preventDefault();
            this.productGroupEvent.close();
            this.productCodeEvent.nativeElement.focus();
        }
    }

    gotoProductName(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.productNameEvent.nativeElement.focus();
        }
    }

    gotoDescription(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.descriptionEvent.nativeElement.focus();
        }
    }

    gotohsCode(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.hsCodeEvent.nativeElement.focus();
        }
    }


    gotoUnit(e) {
        if (e.altKey && e.which == 67) {
            e.preventDefault();
        } else if (e.which == 13) {
            e.preventDefault();
            this.unitsEvent.open();
        }
    }


    gotoPurchaseRate(e) {
        if (e.altKey && e.which == 67) {
            e.preventDefault();
        } else if (e.which == 13) {
            e.preventDefault();
            this.purchaseRateEvent.nativeElement.focus();
        }
    }

    gotoSalesRate(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.salesRateEvent.nativeElement.focus();
        }
    }

    gotoMrp(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.mrpEvent.nativeElement.focus();
        }
    }

    setMrp() {
        const taxId = this.form.get('taxId').value;
        const {taxRate} = this.allTaxes.find((x) => x.taxId == taxId);
        const mrp = this.price + (this.price * taxRate) / 100;
        const floored = mrp.toFixed(2);
        this.form.get('mrp').setValue(floored);
    }

    setSalesRate() {
        const taxId = this.form.get('taxId').value;
        const {taxRate} = this.allTaxes.find((x) => x.taxId == taxId);
        const price = Number(this.form.get('mrp').value) || 0;
        const mrp = (price * 100) / (100 + taxRate);
        const floored = mrp.toFixed(2);
        this.form.get('salesRate').setValue(floored);
    }

    gotoReorderLevel(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.reOrderEvent.nativeElement.focus();
        }
    }

    gotoMinimunStock(e) {
        if (this.allHscodes.includes(this.form.get('hsCode').value)) {
            this.hsCodeMatched = true;
        } else {
            this.hsCodeMatched = false;
        }
        if (e.which == 13) {
            e.preventDefault();
            this.miniStockEvent.nativeElement.focus();
        }
    }

    gotoMaximumStock(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.maxStockEvent.nativeElement.focus();
        }
    }
    gotoMargin(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.marginEvent.nativeElement.focus();
        }
    }

    gotoTax(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.taxEvent.open();
        }
    }

    gotoSave(e) {
        if (e.altKey && e.which == 67) {
            e.preventDefault();
        } else if (e.which == 13) {
            this.taxEvent.close();
            if (this.form.valid) {
                if (this.id) {
                    this.save();
                } else {
                    this.message.confirm('', this.l('Do you want to Save ?'), (isConfirmed) => {
                        if (isConfirmed) {
                            this.save();
                        }
                    });
                }
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    createForm(item: any = {}) {
        this.form = this._formBuilder.group({
            productCode: [item.productCode || ''],
            name: new FormControl(item.name || '', [Validators.required, Validators.minLength(3)]),
            mrp: [item.mrp || 0],
            hsCode: [item.hsCode || ''],
            salesRate: [item.salesRate || 0],
            purchaseRate: [item.purchaseRate || 0],
            margin: [item.margin || 0],
            productType: [{ value: this.toProductTypeSelection(item.productType), disabled: !!item.id }, Validators.required],
            minimumStock: [item.minimumStock || 0],
            maximumStock: [item.maximumStock || 0],
            reorderLevel: [item.reorderLevel || 0],
            isOpeningStock: [!!item.isOpeningStock],
            isUnitEdit: [!!item.isUnitEdit],
            description: [item.description],
            isActive: [item.isActive !== undefined ? item.isActive : true],
            taxId: [item.taxId],
            id: [item.id || null],
            productGroupId: [item.productGroupId, Validators.required],
            unitId: [{ value: item.unitId, disabled: !!item.isUnitEdit }, Validators.required],
            openingStock: this._formBuilder.array(
                item.openingStock ? item.openingStock.map((stockItem) => this.createOpeningStock(stockItem)) : []
            ),
        });

        // Add form change listeners for cache invalidation
        this.subscriptions.push(
            this.form.valueChanges.subscribe(() => {
                this.clearValidationCache();
            })
        );

        this.onChangeType(this.form.get('productType')?.value);
    }

    // Optimized onChangeType using configuration map
    private readonly productTypeConfig = new Map([
        [ProductTypeEnum.KitchenItem, { // Kitchen Item
            enable: ['purchaseRate'],
            disable: ['reorderLevel', 'minimumStock', 'maximumStock', 'isOpeningStock']
        }],
        [ProductTypeEnum.Product, { // Stock Item
            enable: ['reorderLevel', 'minimumStock', 'maximumStock', 'isOpeningStock', 'purchaseRate'],
            disable: []
        }],
        [ProductTypeEnum.RawMaterial, { // Raw Material
            enable: ['reorderLevel', 'minimumStock', 'maximumStock', 'isOpeningStock', 'purchaseRate'],
            disable: []
        }],
        [2, { // Services
            enable: ['purchaseRate'],
            disable: ['reorderLevel', 'minimumStock', 'maximumStock', 'isOpeningStock', 'isAllowSerialNo',
                'isShowRemember', 'isAllowBatch', 'isBom', 'isMultipleUnit', 'modelNoId', 'sizeId', 'brandId']
        }],
        [3, { // Software
            enable: ['reorderLevel', 'minimumStock', 'maximumStock', 'isOpeningStock', 'isAllowSerialNo',
                'isShowRemember', 'purchaseRate', 'isAllowBatch', 'isMultipleUnit', 'modelNoId', 'sizeId', 'brandId'],
            disable: ['isBom']
        }]
    ]);



    getBaseUnitItem() {
        return this.allUnits.filter(unit => unit.id === this.form.get('unitId')?.value);
    }

    onChangeType(id: ProductTypeSelection): void {
        const productType = this.toProductTypeEnum(id);
        const config = this.productTypeConfig.get(productType);
        if (!config) {return;}

        if (!this.isStockCalculatedProductType(productType)) {
            this.clearStockCalculationFields();
        }

        // Batch enable/disable operations for better performance
        config.enable.forEach(fieldName => {
            const control = this.form.get(fieldName);
            if (control) {control.enable();}
        });

        config.disable.forEach(fieldName => {
            const control = this.form.get(fieldName);
            if (control) {control.disable();}
        });
    }

    private toProductTypeSelection(productType: ProductTypeSelection | number | string | undefined): ProductTypeSelection {
        if (productType === undefined || productType === null) {
            return ProductTypeEnum.KitchenItem;
        }

        if (productType === ProductTypeEnum.KitchenItem || productType === 'kitchenItem') {
            return ProductTypeEnum.KitchenItem;
        }

        if (productType === ProductTypeEnum.Product || productType === 'stockItem') {
            return ProductTypeEnum.Product;
        }

        if (productType === ProductTypeEnum.RawMaterial || productType === 'rawMaterial') {
            return ProductTypeEnum.RawMaterial;
        }

        return Number(productType) as ProductTypeEnum;
    }

    private toProductTypeEnum(productType: ProductTypeSelection | number | string | undefined): ProductTypeEnum {
        if (productType === 'kitchenItem') {
            return ProductTypeEnum.KitchenItem;
        }

        if (productType === 'stockItem') {
            return ProductTypeEnum.Product;
        }

        if (productType === 'rawMaterial') {
            return ProductTypeEnum.RawMaterial;
        }

        const numericProductType = Number(productType);
        return Number.isFinite(numericProductType) ? numericProductType as ProductTypeEnum : ProductTypeEnum.Product;
    }

    private getProductPayload(): any {
        const payload = this.form.getRawValue();
        payload.productType = this.toProductTypeEnum(payload.productType);
        if (!this.isStockCalculatedProductType(payload.productType)) {
            payload.minimumStock = 0;
            payload.maximumStock = 0;
            payload.reorderLevel = 0;
            payload.isOpeningStock = false;
            payload.openingStock = [];
        }
        return payload;
    }

    isStockCalculatedProductType(productType: ProductTypeSelection | number | string | undefined = this.form?.get('productType')?.value): boolean {
        const type = this.toProductTypeEnum(productType);
        return type === ProductTypeEnum.Product || type === ProductTypeEnum.RawMaterial;
    }

    private clearStockCalculationFields(): void {
        if (!this.form) {return;}

        this.form.get('minimumStock')?.setValue(0);
        this.form.get('maximumStock')?.setValue(0);
        this.form.get('reorderLevel')?.setValue(0);
        this.form.get('isOpeningStock')?.setValue(false);
        this.openingStock.clear();
        this.allUnitsOpeningStock = [];
    }

    createOpeningStock(item: any = {}) {
        // Get current base unit ID from form or use provided item unitId
        const currentUnitId = this.form?.get('unitId')?.value || this.unitId || item.unitId;

        return this._formBuilder.group({
            id: [item.id ? item.id : this.emptyGuId],
            unitId: [item.unitId ? item.unitId : currentUnitId, Validators.required],
            openingQty: [item.openingQty ? item.openingQty : 0, Validators.required],
            rate: [item.rate ? item.rate : 0, [Validators.required, Validators.min(0)]],
        });
    }


    getopeningStock(form) {
        return form.controls.openingStock.controls;
    }

    addstockList() {
        const control = <FormArray>this.form.controls.openingStock;
        control.push(this.createOpeningStock());
        // Use requestAnimationFrame for better performance
        requestAnimationFrame(() => {
            if (this.opsBranchEvent?.first) {
                this.opsBranchEvent.first.open();
            }
        });
    }


    enablestockListDetailsFormGroup(i, form) {
        this.opsI = i;
        this.openingStock.controls.forEach((data) => {
            if (data.valid) {
                data.disable();
            }
        });
        const control = form.controls.openingStock.controls[i];
        control.enable(i);
        this.opsBranchEvent.toArray()[i].open();
    }

    enableAllstockListDetails() {
        this.openingStock.controls.forEach((element) => {
            element.enable();
        });
        setTimeout(() => {
            this.opsBranchEvent.first.open();
        });
    }

    // Optimized method for adding a unit conversion to improve performance

    openOpeningStock() {
        if (!this.isStockCalculatedProductType()) {
            this.clearStockCalculationFields();
            return;
        }

        this.subscriptions.push(
            this._proxy.getAllUnitForTableDropdown().subscribe((res) => {
                this.allUnitsOpeningStock = [...res]; // Create a copy to avoid modifying original data
                const currentUnitId = this.form.get('unitId').value;

                // If no multiple units, show only base unit
                this.allUnitsOpeningStock = this.allUnitsOpeningStock.filter(unit =>
                    unit.id === currentUnitId
                );
            })
        );

    }

    // Instead of calling setTimeout multiple times, use this helper method
    private focusElement(element: any, delay: number = 0): void {
        if (delay === 0) {
            if (element) {
                if (typeof element.focus === 'function') {
                    element.focus();
                } else if (typeof element.open === 'function') {
                    element.open();
                }
            }
        } else {
            setTimeout(() => {
                if (element) {
                    if (typeof element.focus === 'function') {
                        element.focus();
                    } else if (typeof element.open === 'function') {
                        element.open();
                    }
                }
            }, delay);
        }
    }

    // Optimize the addOpeningStock method
    addOpeningStock(): void {
        console.log('Add opening stock is called');
        if (!this.isStockCalculatedProductType()) {
            this.clearStockCalculationFields();
            return;
        }

        const currentUnitId = this.form?.get('unitId')?.value;
        if (!currentUnitId) {
            this.notify.error(this.l('Please select a base unit first'));
            return;
        }

        if (this.openingStock.length === 0) {
            this.addstockList();
        } else {
            if (this.form.get('openingStock').valid) {
                // Only disable if form is valid to avoid unnecessary operations
                this.openingStock.controls.forEach(control => control.disable());
                this.addstockList();
            } else {
                // Check if all required fields are filled
                this.notify.error(this.l('Please complete current opening stock entry before adding new one'));
                return;
            }
        }



        // Ensure opening stock units are loaded
        this.openOpeningStock();
    }

    removeFromStock(i, form) {
        const control = form.controls.openingStock;
        control.removeAt(i);
        if (control.length > 1) {
        }
    }


    keyProductGroup(e) {
        this.getAllProductGroups(e);
    }


    keyUnit(e) {
        this.getAllUnits(e);
    }

    keyTax(e) {
        this.getAllTaxes(e);
    }

    // product group
    getAllProductGroups(e?: any) {
        this._proxy.getAllProductGroupsForTableDropdown().subscribe((result) => {
            if (e == undefined) {
                this.allProductGoups = result;
                if (result == null) {
                    const alertContent = 'Please create Product Group';
                    this.notify.info(alertContent);
                }
                if (!this.id) {
                    this.form.get('productGroupId').setValue(result[0].id);
                }
            } else {
                this.allProductGoups = result;
                const n = result.length;
                this.form.get('productGroupId').setValue(e);
                this.productGroupEvent.focus();
            }
        });
    }


    getAllUnits(e?: any) {
        this._proxy.getAllUnitForTableDropdown().subscribe((result) => {
            if (e == undefined) {
                this.allUnits = result;
                if (!this.id) {
                    this.unitId = this.allUnits[0].id;
                    this.form.get('unitId').setValue(result[0].id);
                }
            } else {
                this.allUnits = result;
                const n = result.length;
                this.form.get('unitId').setValue(this.allUnits[n - 1].id);
                this.unitsEvent.focus();
                this.onChangeUnit(e);
            }
        });
    }

    onChangeUnit(id) {
        this.unitId = id;
        if (!this.isStockCalculatedProductType()) {
            this.clearStockCalculationFields();
            return;
        }

        if (!this.id) {
            if (this.openingStock.length > 0) {
                // Update existing opening stock entries with new unit
                this.openingStock.controls.forEach(control => {
                    control.get('unitId').setValue(id);
                });
            }
        }

        // Clear cached unit data to refresh with new base unit
        this.unitDataCache.clear();
        this.allUnit = null;

        // Load fresh unit data for conversions
        this.openOpeningStock();
    }

    // Filter units to avoid manipulating arrays directly in forEach loops

    // all Taxes
    getAllTaxes(e?: any) {
        this._proxy.getAllTaxForTableDropdown().subscribe((res) => {
            if (e == undefined) {
                this.allTaxes = res;
                if (!this.id) {
                    this.form.get('taxId').setValue(res[0].taxId);
                }
            } else {
                this.allTaxes = res;
                const n = res.length;
                this.form.get('taxId').setValue(this.allTaxes[n - 1].taxId);
                this.taxEvent.focus();
            }
        });
    }

    saveProductlist(pId) {
        const new1 = this.allProducts.filter((x) => x.id != pId);
        this.allProducts = new1;
    }

    // unit detail
    openUnit(bool: boolean) {
        if (bool == true) {
            this.expanded1 = true;
        } else {
            this.expanded1 = false;
        }
    }

    openSN(bool: boolean, tempRef: TemplateRef<any>) {
        if (bool == true) {
            this.openDialog(tempRef);
            setTimeout(() => {
            });
        }
        if (bool == false) {
        }
    }

    //dialog box
    openDialog(dialog?: TemplateRef<any>, i?: number): void {
        if (!dialog) {
            return;
        }
        this.rowid = i;
        // const dialogRef = this.modalService.show(dialog);
        this.modalRef = this.modalService.show(dialog, Object.assign({}, { class: 'gray modal-lg' }));
    }

    getAllProducts(id?: any) {
        this._proxy.getAllProductForTableDropdown().subscribe((data) => {
            this.allProducts = data;
            if (this.id) {
                this.saveProductlist(this.id);
            }
            if (id) {
                this.saveProductlist(id);
            }
        });
    }

    validationForImeNumberInput(particularStock) {
        const control = <FormArray>particularStock.get('imeiList');
        if (control.controls.length < particularStock.value.openingQty) {
            return true;
        } else {
            return false;
        }
    }

    removeIme(i, abcd) {
        const control = abcd.controls['imeiList'];
        control.removeAt(i);
    }

    //ime nummber

    // addImeNumber(stockOpeningControls: any, i: number) {
    //     let item = { imeiNumber: this.imeNumber };
    //     let control = <FormArray>stockOpeningControls.get("imeiList");
    //     if (control.controls.length < stockOpeningControls.value.openingQty) {
    //         control.push(this.createimeiAdd(item));
    //     } else {
    //         alert("not required imei numer");
    //     }

    //     this.imeNumber = "";
    // }

    removeOpeningStockForm(i, form) {
        const control = form.controls.openingStock;
        control.removeAt(i);
    }

    save(): void {
        this.saving = false;
        if (!this.id) {
            if (this.form.valid) {
                this.saving = true;
                this._proxy
                    .createOrEdit(this.getProductPayload())
                    .pipe(
                        finalize(() => {
                            this.saving = false;
                        })
                    )
                    .subscribe((data) => {
                        this.notify.info(this.l('Saved Successfully'));
                        this.productSave.emit(data);
                        if (this.serialNo) {
                            return;
                        }

                        this.returnAfterSave();
                    });
            } else {
                this.notify.error('Form is invalid !!');
            }
        } else {
            if (true) {
                if (this.form.valid) {
                    if (this.id) {
                        this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                            if (isConfirmed) {
                                this.createapi();
                            }
                        });
                    } else {
                        this.createapi();
                    }
                } else {
                    this.notify.error('Form is invalid !!');
                }
            }
        }
    }

    createapi() {
        this.saving = true;
        this._proxy
            .createOrEdit(this.getProductPayload())
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe(() => {
                if (this.id) {
                    this.notify.info(this.l('Updated Successfully'));
                    this.returnAfterSave();
                } else {
                    this.notify.info(this.l('Saved Successfully'));
                    this.returnAfterSave();
                }
            });
    }

    private returnAfterSave(): void {
        if (this.returnToMenu) {
            this._router.navigate(['/app/main/restaurant/menu'], {
                queryParams: { resumeWizard: 'true' },
            });
            return;
        }

        this._location.back();
    }



    keySAdd(event: boolean) {
        if (!this.isStockCalculatedProductType()) {
            this.clearStockCalculationFields();
            return;
        }

        const ops = this.form.get('isOpeningStock').value;
        if (event == true) {
            if (ops) {
                this.addOpeningStock();
            }
        } else {
            return;
        }
    }

    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.cancelForm();
                }
            });
        } else {
            this.cancelForm();
        }
    }

    private cancelForm(): void {
        this.form.reset();

        if (this.serialNo) {
            this.productSave.emit(null);
            return;
        }

        if (!this.route.snapshot.routeConfig?.path?.startsWith('products/')) {
            return;
        }

        void this._router.navigate(['/app/main/inventory/products']);
    }

    onEnterPressed(e, i) {
        const keyCode = e.which || e.keyCode;
        if (keyCode == 13) {
            e.preventDefault();
        }
    }


    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete?'), (isConfirmed) => {
            if (isConfirmed) {
                this._proxy.delete(id).subscribe(() => {
                    this._location.back();
                    this.notify.success('Successfully Deleted');
                });
            }
        });
    }

    titleCase(str) {
        const separateWord = str.split(' ');
        for (let i = 0; i < separateWord.length; i++) {
            separateWord[i] = separateWord[i].charAt(0).toUpperCase() + separateWord[i].substring(1);
        }
        const final = separateWord.join(' ');
        this.form.get('name').setValue(final);
    }



    // Performance optimization: Debounced validation check
    private validationDebounceTimer: any;

    private debouncedValidationCheck(delay: number = 100): void {
        if (this.validationDebounceTimer) {
            clearTimeout(this.validationDebounceTimer);
        }

        this.validationDebounceTimer = setTimeout(() => {
            this.clearValidationCache();
        }, delay);
    }

}
