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
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRestaurantMenuCategoryDto,
    CreateOrEditRestaurantMenuItemDto,
    CreateOrEditRestaurantMenuVariantDto,
    CreateOrEditRestaurantModifierDto,
    CreateOrEditRestaurantModifierGroupDto,
    ProductServiceProxy,
    ProductTypeEnum,
    RestaurantMenuCategoryDto,
    RestaurantMenuItemDto,
    RestaurantMenuItemTagDto,
    RestaurantRecipeCostDto,
    RestaurantMenuVariantDto,
    RestaurantModifierDto,
    RestaurantModifierGroupDto,
    RestaurantMenuServiceProxy,
    RestaurantRecipeLineDto,
    RestaurantStationDto,
    RestaurantSetupServiceProxy,
    RestaurantInventoryServiceProxy,
    SaveRestaurantMenuItemModifierGroupsDto,
    SaveRestaurantMenuItemTagsDto,
    SaveRestaurantRecipeDto,
    SetRestaurantMenuItemAvailabilityDto,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs';

type MenuRoute = 'kitchen' | 'stock';
type DateTimeInput = DateTime | string | null | undefined;
type MenuPatch = Partial<RestaurantMenuItemForm>;

interface RestaurantMenuRouteSource {
    stationId?: string | undefined;
}

interface RestaurantMenuCategoryForm {
    id?: string;
    name: string;
    description: string;
    sortOrder: number;
    isActive: boolean;
}

interface RestaurantMenuItemForm extends RestaurantMenuRouteSource {
    id?: string;
    categoryId: string;
    productId: string;
    displayName: string;
    shortCode: string;
    description: string;
    colorHex: string;
    imageUrl: string;
    itemRoute: MenuRoute;
    price: number;
    preparationMinutes: number;
    sortOrder: number;
    isAvailable: boolean;
    unavailableUntil: string;
    isVeg: boolean | undefined;
    spiceLevel: number;
    isFeatured: boolean;
    isActive: boolean;
}

interface RestaurantMenuVariantForm {
    id?: string;
    name: string;
    priceDelta: number;
    isAbsolutePrice: boolean;
    isDefault: boolean;
    sortOrder: number;
    isActive: boolean;
}

interface RestaurantModifierGroupForm {
    id?: string;
    name: string;
    minSelect: number;
    maxSelect: number;
    isRequired: boolean;
    sortOrder: number;
    isActive: boolean;
}

interface RestaurantModifierForm {
    id?: string;
    modifierGroupId: string;
    name: string;
    priceDelta: number;
    sortOrder: number;
    isActive: boolean;
}

interface RestaurantRecipeLineForm {
    id?: string;
    rawMaterialId: string;
    rawMaterialName?: string;
    quantity: number;
    unitId: string;
    unitName?: string;
    wastagePercentage: number;
    costRate: number;
    isActive: boolean;
}

interface RestaurantMenuItemTagForm {
    id?: string;
    menuItemId?: string;
    name: string | undefined;
    colorHex: string | undefined;
    sortOrder: number;
}

@Component({
    selector: 'restaurant-menu',
    templateUrl: './restaurant-menu.component.html',
    styleUrls: ['../restaurant-shared.css'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantMenuComponent extends AppComponentBase implements OnInit, AfterViewInit {
    categories: RestaurantMenuCategoryDto[] = [];
    menuItems: RestaurantMenuItemDto[] = [];
    products: UniversalDropdownDto[] = [];
    rawMaterials: UniversalDropdownDto[] = [];
    units: UniversalDropdownDto[] = [];
    stations: RestaurantStationDto[] = [];
    modifierGroups: RestaurantModifierGroupDto[] = [];
    recipeLines: RestaurantRecipeLineForm[] = [];
    selectedMenuItem: RestaurantMenuItemDto | null = null;
    recipeCost: RestaurantRecipeCostDto | null = null;
    saving = false;
    loading = false;
    categoryFilterId = '';
    stationFilterId = '';
    routeFilter: MenuRoute | '' = '';
    menuSearch = '';
    selectedModifierGroupIds: string[] = [];
    tagRows: RestaurantMenuItemTagForm[] = [];

    categoryForm: RestaurantMenuCategoryForm = this.createEmptyCategoryForm();
    itemForm: RestaurantMenuItemForm = this.createEmptyItemForm();
    variantForm: RestaurantMenuVariantForm = this.createEmptyVariantForm();
    modifierGroupForm: RestaurantModifierGroupForm = this.createEmptyModifierGroupForm();
    modifierForm: RestaurantModifierForm = this.createEmptyModifierForm();

    private restaurantInventoryService = inject(RestaurantInventoryServiceProxy);
    private restaurantMenuService = inject(RestaurantMenuServiceProxy);
    private restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private productService = inject(ProductServiceProxy);
    private cdr = inject(ChangeDetectorRef);

    constructor() {
        super(inject(Injector));
    }

    ngOnInit(): void {
        this.loadLookups();
        this.refresh();
    }

    ngAfterViewInit(): void {
        this.cdr.detectChanges();
    }

    loadLookups(): void {
        this.restaurantMenuService.getMenuProducts().subscribe((result) => {
            this.products = result || [];
            this.cdr.markForCheck();
        });
        this.restaurantInventoryService.getRawMaterials().subscribe((result) => {
            this.rawMaterials = result || [];
            this.cdr.markForCheck();
        });
        this.productService.getAllUnitForTableDropdown().subscribe((result) => {
            this.units = result || [];
            this.cdr.markForCheck();
        });
        this.restaurantSetupService.getStations().subscribe((result) => {
            this.stations = result || [];
            this.cdr.markForCheck();
        });
    }

    refresh(selectMenuItemId?: string): void {
        this.loading = true;
        this.restaurantMenuService
            .getMenuEditorData()
            .pipe(finalize(() => (this.loading = false)))
            .subscribe((result) => {
                this.categories = result?.categories || [];
                this.menuItems = result?.menuItems || [];
                this.modifierGroups = result?.modifierGroups || [];
                this.stations = result?.stations?.length ? result.stations : this.stations;

                if (!this.itemForm.categoryId && this.categories.length) {
                    this.itemForm.categoryId = this.categories[0].id;
                }

                if (selectMenuItemId) {
                    const item = this.menuItems.find((x) => x.id === selectMenuItemId);
                    if (item) {
                        this.editMenuItem(item);
                    }
                }
                this.cdr.markForCheck();
            });
    }

    get filteredMenuItems(): RestaurantMenuItemDto[] {
        const search = (this.menuSearch || '').toLowerCase().trim();
        return this.menuItems.filter((item) => {
            const categoryMatch = !this.categoryFilterId || item.categoryId === this.categoryFilterId;
            const stationMatch = !this.stationFilterId || item.stationId === this.stationFilterId;
            const routeMatch = !this.routeFilter || this.menuRoute(item) === this.routeFilter;
            const tagText = (item.tags || []).map((tag) => tag.name).join(' ');
            const searchMatch =
                !search ||
                (item.displayName || '').toLowerCase().includes(search) ||
                (item.productName || '').toLowerCase().includes(search) ||
                (item.categoryName || '').toLowerCase().includes(search) ||
                (item.shortCode || '').toLowerCase().includes(search) ||
                tagText.toLowerCase().includes(search);
            return categoryMatch && stationMatch && routeMatch && searchMatch;
        });
    }

    get kitchenItemCount(): number {
        return this.menuItems.filter((item) => this.menuRoute(item) === 'kitchen').length;
    }

    get stockItemCount(): number {
        return this.menuItems.filter((item) => this.menuRoute(item) === 'stock').length;
    }

    get soldOutCount(): number {
        return this.menuItems.filter((item) => !item.isAvailable).length;
    }

    get featuredCount(): number {
        return this.menuItems.filter((item) => item.isFeatured).length;
    }

    get activeItemCount(): number {
        return this.menuItems.filter((item) => item.isActive !== false).length;
    }

    get inactiveItemCount(): number {
        return this.menuItems.filter((item) => item.isActive === false).length;
    }

    get visibleItemCount(): number {
        return this.filteredMenuItems.length;
    }

    get assignedModifierGroupCount(): number {
        return this.selectedModifierGroupIds.length;
    }

    get recipeMarginPercentage(): number | null {
        if (!this.selectedMenuItem || !this.recipeCost?.totalCost || !this.selectedMenuItem.price) {
            return null;
        }

        return ((this.selectedMenuItem.price - this.recipeCost.totalCost) / this.selectedMenuItem.price) * 100;
    }

    saveCategory(): void {
        if (!this.categoryForm.name) {
            this.notify.warn('Category name is required');
            return;
        }

        this.saving = true;
        this.restaurantMenuService
            .createOrEditCategory(
                new CreateOrEditRestaurantMenuCategoryDto({
                    id: this.categoryForm.id,
                    name: this.categoryForm.name,
                    description: this.categoryForm.description || undefined,
                    sortOrder: Number(this.categoryForm.sortOrder || 0),
                    isActive: this.categoryForm.isActive !== false,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe((id) => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.categoryFilterId = id || this.categoryFilterId;
                this.categoryForm = this.createEmptyCategoryForm();
                this.refresh();
            });
    }

    saveMenuItem(): void {
        if (!this.itemForm.categoryId || !this.itemForm.productId || !this.itemForm.displayName) {
            this.notify.warn('Category, product, and display name are required');
            return;
        }

        if (this.itemForm.itemRoute === 'kitchen' && !this.itemForm.stationId) {
            this.notify.warn('Select KOT/BOT station for kitchen item');
            return;
        }

        this.saving = true;
        const payload = new CreateOrEditRestaurantMenuItemDto({
            id: this.itemForm.id,
            categoryId: this.itemForm.categoryId,
            productId: this.itemForm.productId,
            stationId: this.itemForm.itemRoute === 'kitchen' ? this.itemForm.stationId || undefined : undefined,
            displayName: this.itemForm.displayName,
            shortCode: this.itemForm.shortCode || undefined,
            description: this.itemForm.description || undefined,
            colorHex: this.itemForm.colorHex || undefined,
            imageUrl: this.itemForm.imageUrl || undefined,
            price: Number(this.itemForm.price || 0),
            preparationMinutes: Number(this.itemForm.preparationMinutes || 0),
            sortOrder: Number(this.itemForm.sortOrder || 0),
            unavailableUntil: this.toDateTime(this.itemForm.unavailableUntil),
            isVeg: this.itemForm.isVeg,
            isAvailable: this.itemForm.isAvailable !== false,
            spiceLevel: Number(this.itemForm.spiceLevel || 0),
            isFeatured: this.itemForm.isFeatured === true,
            isActive: this.itemForm.isActive !== false,
        });

        this.restaurantMenuService
            .createOrEditMenuItem(payload)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe((id) => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh(id);
            });
    }

    editCategory(category: RestaurantMenuCategoryDto): void {
        this.categoryForm = { ...category };
    }

    editMenuItem(item: RestaurantMenuItemDto): void {
        this.selectedMenuItem = item;
        this.itemForm = {
            id: item.id,
            categoryId: item.categoryId,
            productId: item.productId,
            displayName: item.displayName || '',
            price: item.price,
            preparationMinutes: item.preparationMinutes,
            sortOrder: item.sortOrder,
            itemRoute: this.menuRoute(item),
            stationId: item.stationId || '',
            shortCode: item.shortCode || '',
            description: item.description || '',
            colorHex: item.colorHex || '',
            imageUrl: item.imageUrl || '',
            unavailableUntil: this.fromDateTime(item.unavailableUntil),
            isVeg: item.isVeg,
            spiceLevel: item.spiceLevel || 0,
            isAvailable: item.isAvailable !== false,
            isFeatured: item.isFeatured === true,
            isActive: item.isActive !== false,
        };
        this.selectedModifierGroupIds = (item.modifierGroups || []).map((group) => group.id);
        this.tagRows = (item.tags || []).map((tag) => ({ ...tag }));
        this.resetVariantForm();
        this.resetModifierForm();
        this.loadRecipe(item);
    }

    loadRecipe(item: RestaurantMenuItemDto): void {
        this.selectedMenuItem = item;
        this.recipeCost = null;
        this.restaurantMenuService.getRecipe(item.productId).subscribe((result) => {
            this.recipeLines = result || [];
            this.cdr.markForCheck();
        });
        this.restaurantMenuService.getRecipeCost(item.productId).subscribe((result) => {
            this.recipeCost = result;
            this.cdr.markForCheck();
        });
    }

    addRecipeLine(): void {
        this.recipeLines.push({
            rawMaterialId: '',
            quantity: 1,
            unitId: this.units[0]?.id || '',
            wastagePercentage: 0,
            costRate: 0,
            isActive: true,
        });
    }

    removeRecipeLine(index: number): void {
        this.recipeLines.splice(index, 1);
    }

    saveRecipe(): void {
        if (!this.selectedMenuItem) {
            return;
        }

        if (this.recipeLines.some((line) => !line.rawMaterialId || !line.unitId || line.quantity <= 0)) {
            this.notify.warn('Complete raw material, unit, and quantity on every recipe line');
            return;
        }

        this.saving = true;
        this.restaurantMenuService
            .saveRecipe(
                new SaveRestaurantRecipeDto({
                    productId: this.selectedMenuItem.productId,
                    lines: this.recipeLines.map(
                        (line) =>
                            new RestaurantRecipeLineDto({
                                id: line.id,
                                rawMaterialId: line.rawMaterialId,
                                rawMaterialName: line.rawMaterialName,
                                quantity: line.quantity,
                                unitId: line.unitId,
                                unitName: line.unitName,
                                wastagePercentage: line.wastagePercentage || 0,
                                costRate: line.costRate || 0,
                                isActive: line.isActive !== false,
                            }),
                    ),
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh(this.selectedMenuItem.id);
            });
    }

    saveVariant(): void {
        if (!this.selectedMenuItem) {
            this.notify.warn('Select a menu item first');
            return;
        }
        if (!this.variantForm.name) {
            this.notify.warn('Variant name is required');
            return;
        }

        this.saving = true;
        this.restaurantMenuService
            .createOrEditVariant(
                new CreateOrEditRestaurantMenuVariantDto({
                    id: this.variantForm.id,
                    menuItemId: this.selectedMenuItem.id,
                    name: this.variantForm.name,
                    priceDelta: Number(this.variantForm.priceDelta || 0),
                    isAbsolutePrice: this.variantForm.isAbsolutePrice === true,
                    isDefault: this.variantForm.isDefault === true,
                    sortOrder: Number(this.variantForm.sortOrder || 0),
                    isActive: this.variantForm.isActive !== false,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetVariantForm();
                this.refresh(this.selectedMenuItem.id);
            });
    }

    editVariant(variant: RestaurantMenuVariantDto): void {
        this.variantForm = { ...variant };
    }

    deleteVariant(variant: RestaurantMenuVariantDto): void {
        this.restaurantMenuService.deleteVariant(variant.id).subscribe(() => {
            this.notify.success(this.l('SuccessfullyDeleted'));
            this.refresh(this.selectedMenuItem?.id);
        });
    }

    saveModifierGroup(): void {
        if (!this.modifierGroupForm.name) {
            this.notify.warn('Modifier group name is required');
            return;
        }

        this.saving = true;
        this.restaurantMenuService
            .createOrEditModifierGroup(
                new CreateOrEditRestaurantModifierGroupDto({
                    id: this.modifierGroupForm.id,
                    name: this.modifierGroupForm.name,
                    minSelect: Number(this.modifierGroupForm.minSelect || 0),
                    maxSelect: Number(this.modifierGroupForm.maxSelect || 1),
                    sortOrder: Number(this.modifierGroupForm.sortOrder || 0),
                    isRequired: this.modifierGroupForm.isRequired === true,
                    isActive: this.modifierGroupForm.isActive !== false,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetModifierGroup();
                this.refresh(this.selectedMenuItem?.id);
            });
    }

    editModifierGroup(group: RestaurantModifierGroupDto): void {
        this.modifierGroupForm = { ...group };
    }

    saveModifier(): void {
        const groupId = this.modifierForm.modifierGroupId || this.modifierGroupForm.id;
        if (!groupId || !this.modifierForm.name) {
            this.notify.warn('Modifier group and modifier name are required');
            return;
        }

        this.saving = true;
        this.restaurantMenuService
            .createOrEditModifier(
                new CreateOrEditRestaurantModifierDto({
                    id: this.modifierForm.id,
                    modifierGroupId: groupId,
                    name: this.modifierForm.name,
                    priceDelta: Number(this.modifierForm.priceDelta || 0),
                    sortOrder: Number(this.modifierForm.sortOrder || 0),
                    isActive: this.modifierForm.isActive !== false,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetModifierForm();
                this.refresh(this.selectedMenuItem?.id);
            });
    }

    editModifier(group: RestaurantModifierGroupDto, modifier: RestaurantModifierDto): void {
        this.modifierForm = { ...modifier, modifierGroupId: group.id };
    }

    saveMenuItemModifierGroups(): void {
        if (!this.selectedMenuItem) {
            return;
        }

        this.saving = true;
        this.restaurantMenuService
            .saveMenuItemModifierGroups(
                new SaveRestaurantMenuItemModifierGroupsDto({
                    menuItemId: this.selectedMenuItem.id,
                    modifierGroupIds: this.selectedModifierGroupIds || [],
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh(this.selectedMenuItem.id);
            });
    }

    toggleModifierGroup(groupId: string, checked: boolean): void {
        if (checked) {
            if (!this.selectedModifierGroupIds.includes(groupId)) {
                this.selectedModifierGroupIds = [...this.selectedModifierGroupIds, groupId];
            }
            return;
        }

        this.selectedModifierGroupIds = this.selectedModifierGroupIds.filter((id) => id !== groupId);
    }

    onModifierGroupChanged(groupId: string, event: Event): void {
        this.toggleModifierGroup(groupId, (event.target as HTMLInputElement).checked);
    }

    addTag(): void {
        this.tagRows.push({
            menuItemId: this.selectedMenuItem?.id,
            name: '',
            colorHex: '#eef6ff',
            sortOrder: this.tagRows.length,
        });
    }

    removeTag(index: number): void {
        this.tagRows.splice(index, 1);
    }

    saveTags(): void {
        if (!this.selectedMenuItem) {
            return;
        }

        this.saving = true;
        this.restaurantMenuService
            .saveMenuItemTags(
                new SaveRestaurantMenuItemTagsDto({
                    menuItemId: this.selectedMenuItem.id,
                    tags: this.tagRows.map(
                        (tag, index) =>
                            new RestaurantMenuItemTagDto({
                                id: tag.id,
                                menuItemId: this.selectedMenuItem.id,
                                name: tag.name,
                                colorHex: tag.colorHex || '#eef6ff',
                                sortOrder: tag.sortOrder ?? index,
                            }),
                    ),
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh(this.selectedMenuItem.id);
            });
    }

    setAvailability(item: RestaurantMenuItemDto, isAvailable: boolean): void {
        this.restaurantMenuService
            .setItemAvailability(
                new SetRestaurantMenuItemAvailabilityDto({
                    menuItemId: item.id,
                    isAvailable,
                    unavailableUntil: isAvailable ? undefined : this.toDateTime(item.unavailableUntil),
                }),
            )
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh(item.id);
            });
    }

    toggleMenuItemActive(item: RestaurantMenuItemDto, event?: Event): void {
        event?.stopPropagation();
        this.saveMenuItemPatch(item, { isActive: item.isActive === false });
    }

    toggleFeatured(item: RestaurantMenuItemDto, event?: Event): void {
        event?.stopPropagation();
        this.saveMenuItemPatch(item, { isFeatured: item.isFeatured !== true });
    }

    toggleCategoryActive(category: RestaurantMenuCategoryDto, event?: Event): void {
        event?.stopPropagation();
        this.saving = true;
        this.restaurantMenuService
            .createOrEditCategory(
                new CreateOrEditRestaurantMenuCategoryDto({
                    id: category.id,
                    name: category.name || '',
                    description: category.description || undefined,
                    sortOrder: category.sortOrder || 0,
                    isActive: category.isActive === false,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh(this.selectedMenuItem?.id);
            });
    }

    duplicateMenuItem(item: RestaurantMenuItemDto, event?: Event): void {
        event?.stopPropagation();
        this.editMenuItem(item);
        this.itemForm.id = undefined;
        this.itemForm.displayName = `${item.displayName || item.productName || 'Menu item'} Copy`;
        this.itemForm.shortCode = '';
        this.selectedMenuItem = null;
        this.recipeLines = [];
        this.recipeCost = null;
        this.selectedModifierGroupIds = [];
        this.tagRows = [];
        this.resetVariantForm();
    }

    clearFilters(): void {
        this.categoryFilterId = '';
        this.stationFilterId = '';
        this.routeFilter = '';
        this.menuSearch = '';
    }

    onProductChanged(productId: string): void {
        if (this.itemForm.displayName || !productId) {
            return;
        }

        this.itemForm.displayName = this.products.find((product) => product.id === productId)?.displayName || '';
    }

    selectCategory(categoryId: string): void {
        this.categoryFilterId = this.categoryFilterId === categoryId ? '' : categoryId;
        this.itemForm.categoryId = categoryId || this.itemForm.categoryId;
    }

    resetCategory(): void {
        this.categoryForm = this.createEmptyCategoryForm();
    }

    resetMenuItem(): void {
        this.itemForm = this.createEmptyItemForm();
        this.itemForm.categoryId = this.categories[0]?.id || '';
        this.selectedMenuItem = null;
        this.selectedModifierGroupIds = [];
        this.tagRows = [];
        this.recipeLines = [];
        this.recipeCost = null;
        this.resetVariantForm();
    }

    resetVariantForm(): void {
        this.variantForm = this.createEmptyVariantForm();
    }

    resetModifierGroup(): void {
        this.modifierGroupForm = this.createEmptyModifierGroupForm();
    }

    resetModifierForm(): void {
        this.modifierForm = this.createEmptyModifierForm();
    }

    variantDisplayPrice(item: RestaurantMenuItemDto, variant: RestaurantMenuVariantDto): number {
        return variant.isAbsolutePrice
            ? Number(variant.priceDelta || 0)
            : Number(item?.price || 0) + Number(variant.priceDelta || 0);
    }

    productTypeText(type: number): string {
        return ['Stock Item', 'Raw Material', 'Service', 'Software', 'Spare', 'Fixed Asset', 'Kitchen Item'][type] || 'Stock Item';
    }

    menuRoute(item: RestaurantMenuRouteSource | null | undefined): MenuRoute {
        return item?.stationId ? 'kitchen' : 'stock';
    }

    onItemRouteChanged(route: MenuRoute): void {
        this.itemForm.itemRoute = route;
        if (route === 'stock') {
            this.itemForm.stationId = '';
        }
    }

    itemRouteText(item: RestaurantMenuRouteSource): string {
        return this.menuRoute(item) === 'kitchen' ? 'Kitchen Item' : 'Stock Item';
    }

    itemRouteClass(item: RestaurantMenuRouteSource): string {
        return this.menuRoute(item) === 'kitchen' ? 'restaurant-status-warning' : 'restaurant-status-success';
    }

    menuBehaviorText(item: RestaurantMenuItemDto): string {
        if (item.productType === ProductTypeEnum.Services) {
            return 'Service';
        }

        return item.hasRecipe ? 'Recipe/BOM' : 'Direct';
    }

    menuBehaviorClass(item: RestaurantMenuItemDto): string {
        if (item.productType === ProductTypeEnum.Services) {
            return 'restaurant-status-neutral';
        }

        return item.hasRecipe ? 'restaurant-status-success' : 'restaurant-status-primary';
    }

    stationTypeText(type: number): string {
        return ['Kitchen', 'Bar', 'Counter', 'Other'][type] || 'Other';
    }

    private createEmptyCategoryForm(): RestaurantMenuCategoryForm {
        return {
            name: '',
            description: '',
            sortOrder: 0,
            isActive: true,
        };
    }

    private createEmptyItemForm(): RestaurantMenuItemForm {
        return {
            categoryId: '',
            productId: '',
            stationId: '',
            itemRoute: 'kitchen',
            displayName: '',
            shortCode: '',
            description: '',
            colorHex: '',
            imageUrl: '',
            price: 0,
            preparationMinutes: 0,
            sortOrder: 0,
            isAvailable: true,
            unavailableUntil: '',
            isVeg: undefined,
            spiceLevel: 0,
            isFeatured: false,
            isActive: true,
        };
    }

    private createEmptyVariantForm(): RestaurantMenuVariantForm {
        return {
            name: '',
            priceDelta: 0,
            isAbsolutePrice: false,
            isDefault: false,
            sortOrder: 0,
            isActive: true,
        };
    }

    private createEmptyModifierGroupForm(): RestaurantModifierGroupForm {
        return {
            name: '',
            minSelect: 0,
            maxSelect: 1,
            isRequired: false,
            sortOrder: 0,
            isActive: true,
        };
    }

    private createEmptyModifierForm(): RestaurantModifierForm {
        return {
            modifierGroupId: '',
            name: '',
            priceDelta: 0,
            sortOrder: 0,
            isActive: true,
        };
    }

    private fromDateTime(value: DateTimeInput): string {
        if (!value) {
            return '';
        }

        if (typeof value === 'string') {
            return value.substring(0, 16);
        }

        return value.toFormat('yyyy-LL-dd\'T\'HH:mm');
    }

    private toDateTime(value: DateTimeInput): DateTime | undefined {
        if (!value) {
            return undefined;
        }

        if (typeof value !== 'string') {
            return value;
        }

        return DateTime.fromISO(value);
    }

    private saveMenuItemPatch(item: RestaurantMenuItemDto, patch: MenuPatch): void {
        const route = patch.itemRoute || this.menuRoute(item);
        const stationId = route === 'kitchen' ? patch.stationId ?? item.stationId : undefined;
        this.saving = true;
        this.restaurantMenuService
            .createOrEditMenuItem(
                new CreateOrEditRestaurantMenuItemDto({
                    id: item.id,
                    categoryId: patch.categoryId ?? item.categoryId,
                    productId: patch.productId ?? item.productId,
                    stationId: stationId || undefined,
                    displayName: patch.displayName ?? item.displayName ?? '',
                    shortCode: patch.shortCode ?? item.shortCode ?? undefined,
                    description: patch.description ?? item.description ?? undefined,
                    colorHex: patch.colorHex ?? item.colorHex ?? undefined,
                    imageUrl: patch.imageUrl ?? item.imageUrl ?? undefined,
                    price: Number(patch.price ?? item.price ?? 0),
                    preparationMinutes: Number(patch.preparationMinutes ?? item.preparationMinutes ?? 0),
                    sortOrder: Number(patch.sortOrder ?? item.sortOrder ?? 0),
                    unavailableUntil: this.toDateTime(patch.unavailableUntil ?? item.unavailableUntil),
                    isVeg: patch.isVeg ?? item.isVeg,
                    isAvailable: patch.isAvailable ?? item.isAvailable !== false,
                    spiceLevel: Number(patch.spiceLevel ?? item.spiceLevel ?? 0),
                    isFeatured: patch.isFeatured ?? item.isFeatured === true,
                    isActive: patch.isActive ?? item.isActive !== false,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh(item.id);
            });
    }
}
