import {
    AfterViewInit,
    ChangeDetectorRef,
    Component,
    ElementRef,
    Injector,
    OnDestroy,
    OnInit,
    ViewChild,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CancelRestaurantTicketDto,
    CreateOrEditRestaurantOrderDto,
    CreateOrEditRestaurantOrderItemDto,
    CreateOrEditRestaurantOrderItemModifierDto,
    EntityDtoOfGuid,
    FinalizeRestaurantBillDto,
    FinalizeRestaurantBillLineDto,
    ApplyRestaurantOrderDiscountDto,
    MergeRestaurantOrdersDto,
    RestaurantBillingServiceProxy,
    RestaurantAreaDto,
    RestaurantMaterialRequirementDto,
    RestaurantMenuCategoryDto,
    RestaurantMenuItemDto,
    RestaurantMenuVariantDto,
    RestaurantModifierDto,
    RestaurantOrderDto,
    RestaurantOrderItemDto,
    RestaurantOrderItemModifierDto,
    RestaurantOrderItemStatus,
    RestaurantMenuServiceProxy,
    RestaurantOrderServiceProxy,
    RestaurantOrderType,
    RestaurantSetupServiceProxy,
    RestaurantStockValidationDto,
    RestaurantTableDto,
    RestaurantTicketDto,
    RestaurantTicketPurpose,
    RestaurantTicketStatus,
    FinalizeRestaurantBillResultDto,
    ReprintRestaurantTicketDto,
    PaymentMethod,
    SalesMasterAccountLedgerTableDto,
    SalesMastersServiceProxy,
    SplitRestaurantOrderDto,
    TransferRestaurantTableDto,
    ValidateRestaurantBillStockDto,
    VoidRestaurantOrderItemDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { AllowIn, ShortcutInput } from 'ng-keyboard-shortcuts';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs';
import { RestaurantCashShift, RestaurantCashShiftApiService } from '../restaurant-cash-shift-api.service';

type DateTimeInput = DateTime | string | number | null | undefined;

type PosLineModifier = Partial<RestaurantOrderItemModifierDto> & {
    name?: string;
};

interface PosModifierGroup {
    id: string;
    name: string | undefined;
    minSelect: number;
    maxSelect: number;
    isRequired: boolean;
    sortOrder: number;
    isActive: boolean;
    modifiers?: RestaurantModifierDto[];
}

type PosCartLine = Omit<
    Partial<RestaurantOrderItemDto>,
    'modifiers' | 'stationId' | 'unitId' | 'taxId' | 'status'
> & {
    id?: string;
    configKey?: string;
    productId: string;
    productName?: string;
    displayName?: string;
    menuItemId?: string;
    variantId?: string;
    variantName?: string;
    itemNameSnapshot?: string;
    variantNameSnapshot?: string;
    stationId?: string | null;
    unitId?: string;
    taxId?: string;
    qty: number;
    rate: number;
    modifierUnitTotal?: number;
    modifierTotal?: number;
    modifierSummary?: string;
    modifiers?: PosLineModifier[];
    discountAmount?: number;
    notes?: string;
    status?: RestaurantOrderItemStatus | number;
    billedQty?: number;
    unbilledQty?: number;
    amount?: number;
    cancelReason?: string;
};

interface PosOperationForm {
    discountAmount: number;
    transferTableId: string;
    splitTableId: string;
    splitItemIds: string[];
    mergeSourceOrderIds: string[];
}

interface PosBillForm {
    dateMiti: string;
    salesAccountId: string;
    ledgerId: string;
    customerName: string;
    customerAddress: string;
    customerVatNo: string;
    customerPhoneNo: string;
    paymentMethod: PaymentMethod;
    paymentMethodLedgerId: string | null;
    tipAmount: number;
    customerPaidAmount: number | '' | null;
    isPrint: boolean;
}

interface PosBillReceiptLine {
    name: string;
    details: string;
    qty: number;
    rate: number;
    amount: number;
}

interface PosBillReceiptSnapshot {
    orderNo: string;
    orderType: string;
    tableName: string;
    customerName: string;
    customerPhoneNo: string;
    paymentMethod: string;
    printedAt: DateTime;
    lines: PosBillReceiptLine[];
}

type RestaurantPosContext = 'tables' | 'menu' | 'orders' | 'tickets';

type PendingOrderSwitch =
    | { kind: 'table'; table: RestaurantTableDto }
    | { kind: 'order'; order: RestaurantOrderDto }
    | { kind: 'new'; orderType: RestaurantOrderType };

@Component({
    selector: 'restaurant-pos',
    templateUrl: './restaurant-pos.component.html',
    styleUrls: ['./restaurant-pos.component.css'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantPosComponent extends AppComponentBase implements OnInit, AfterViewInit, OnDestroy {
    areas: RestaurantAreaDto[] = [];
    categories: RestaurantMenuCategoryDto[] = [];
    tables: RestaurantTableDto[] = [];
    menuItems: RestaurantMenuItemDto[] = [];
    openOrders: RestaurantOrderDto[] = [];
    ledgers: SalesMasterAccountLedgerTableDto[] = [];
    salesLedgerList: SalesMasterAccountLedgerTableDto[] = [];
    selectedTable: RestaurantTableDto | null = null;
    selectedOrder: RestaurantOrderDto | null = null;
    cart: PosCartLine[] = [];
    tickets: RestaurantTicketDto[] = [];
    saving = false;
    loading = false;
    cashShift: RestaurantCashShift | null = null;
    browserOnline = navigator.onLine;
    serverReachable = false;
    localDraftAvailable = false;
    localDraftSavedAt: Date | null = null;
    stockValidation: RestaurantStockValidationDto | null = null;
    isFullscreen = false;
    selectedAreaId = '';
    selectedCategoryId = '';
    selectedRouteFilter = '';
    menuSearch = '';
    tableSearch = '';
    tableStatusFilter = '';
    orderSearch = '';
    orderTypeFilter = '';
    orderStatusFilter = '';
    filteredTablesList: RestaurantTableDto[] = [];
    tableStatusCounts: Record<string, number> = {};
    matchingTableCount = 0;
    filteredMenuItemsList: RestaurantMenuItemDto[] = [];
    filteredOrdersList: RestaurantOrderDto[] = [];
    operationTablesList: RestaurantTableDto[] = [];
    mergeCandidatesList: RestaurantOrderDto[] = [];
    splittableLinesList: PosCartLine[] = [];
    unbilledLinesList: PosCartLine[] = [];
    pendingVariants: RestaurantMenuVariantDto[] = [];
    pendingModifierGroups: PosModifierGroup[] = [];
    selectedVariantItem: RestaurantMenuVariantDto | null = null;
    cartItemCountValue = 0;
    cartTotalAmount = 0;
    draftLineCountValue = 0;
    configuratorTotalAmount = 0;
    selectedBillTotalAmount = 0;
    selectedBillLineCountValue = 0;
    billPayableAmount = 0;
    billReturnAmount = 0;
    activeTicketCountValue = 0;
    kitchenItemCountValue = 0;
    stockItemCountValue = 0;
    kitchenLineCountValue = 0;
    stockLineCountValue = 0;
    draftKitchenLineCountValue = 0;
    draftStockLineCountValue = 0;
    currentClock = DateTime.now();
    currentOrderTitleText = '';
    currentOrderSubtitleText = '';
    billMode: 'full' | 'split' = 'full';
    activeContext: RestaurantPosContext = 'tables';
    orderType = RestaurantOrderType.DineIn;
    pendingMenuItem: RestaurantMenuItemDto | null = null;
    pendingVariantId = '';
    pendingModifierIds: Record<string, string[]> = {};
    splitBillQuantities: Record<string, number> = {};
    activeOperation: 'discount' | 'transfer' | 'split' | 'merge' | '' = '';
    orderDirty = false;
    checkoutVisible = false;
    advancedCheckoutOpen = false;
    mobileCartOpen = false;
    pendingOrderSwitch: PendingOrderSwitch | null = null;
    shortcuts: ShortcutInput[] = [];
    operationForm: PosOperationForm = {
        discountAmount: 0,
        transferTableId: '',
        splitTableId: '',
        splitItemIds: [],
        mergeSourceOrderIds: [],
    };
    readonly orderTypeEnum = RestaurantOrderType;
    readonly paymentMethodEnum = PaymentMethod;
    readonly ticketPurpose = RestaurantTicketPurpose;
    readonly tableStatuses = [
        { value: '0', label: 'Available' },
        { value: '1', label: 'Occupied' },
        { value: '2', label: 'Reserved' },
        { value: '3', label: 'Maintenance' },
    ];
    readonly orderTypeFilters = [
        { value: RestaurantOrderType.DineIn.toString(), label: 'Dine In' },
        { value: RestaurantOrderType.TakeAway.toString(), label: 'Takeaway' },
        { value: RestaurantOrderType.Delivery.toString(), label: 'Delivery' },
    ];
    readonly orderStatusFilters = [
        { value: '0', label: 'Draft' },
        { value: '1', label: 'Sent' },
        { value: '2', label: 'In Progress' },
        { value: '3', label: 'Ready' },
        { value: '4', label: 'Served' },
    ];
    readonly paymentMethods = [
        { value: PaymentMethod.Cash, label: 'Cash', icon: 'fa-money-bill' },
        { value: PaymentMethod.Card_Swipe, label: 'Card', icon: 'fa-credit-card' },
        { value: PaymentMethod.QR, label: 'QR', icon: 'fa-qrcode' },
        { value: PaymentMethod.Credit, label: 'Credit', icon: 'fa-file-invoice' },
        { value: PaymentMethod.Cheque, label: 'Cheque', icon: 'fa-money-check' },
    ];

    billForm: PosBillForm = {
        dateMiti: '',
        salesAccountId: '',
        ledgerId: '',
        customerName: '',
        customerAddress: '',
        customerVatNo: '',
        customerPhoneNo: '',
        paymentMethod: PaymentMethod.Cash,
        paymentMethodLedgerId: null,
        tipAmount: 0,
        customerPaidAmount: null,
        isPrint: true,
    };

    private restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private restaurantMenuService = inject(RestaurantMenuServiceProxy);
    private restaurantOrderService = inject(RestaurantOrderServiceProxy);
    private restaurantBillingService = inject(RestaurantBillingServiceProxy);
    private cashShiftService = inject(RestaurantCashShiftApiService);
    private salesMastersService = inject(SalesMastersServiceProxy);
    private cdr = inject(ChangeDetectorRef);
    private hostElement = inject(ElementRef<HTMLElement>);
    private timerHandle?: ReturnType<typeof setInterval>;

    @ViewChild('menuSearchInput') menuSearchInput?: ElementRef<HTMLInputElement>;
    @ViewChild('checkoutModal') checkoutModal?: ModalDirective;
    @ViewChild('checkoutButton') checkoutButton?: ElementRef<HTMLButtonElement>;
    @ViewChild('mobileOrderButton') mobileOrderButton?: ElementRef<HTMLButtonElement>;
    @ViewChild('checkoutCustomerInput') checkoutCustomerInput?: ElementRef<HTMLInputElement>;

    constructor() {
        super(inject(Injector));
    }

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.billForm.dateMiti = this.billForm.dateMiti || this.today;
        this.initShortcuts();
        this.timerHandle = setInterval(() => {
            this.currentClock = DateTime.now();
            this.recalculateViewState();
            this.loadCashShift();
        }, 30000);
        this.recalculateViewState();
        this.refresh();
        this.loadCashShift();
        this.loadLocalDraftState();
        window.addEventListener('online', this.onConnectionChange);
        window.addEventListener('offline', this.onConnectionChange);
        document.addEventListener('fullscreenchange', this.onFullscreenChange);
        this.salesMastersService.getAllAccountLedgerForTableDropdown().subscribe((result) => {
            this.ledgers = result || [];
            this.applyDefaultLedgerSelection();
        });

        this.salesMastersService.getAllSalesAccountForTableDropdown().subscribe((result) => {
            this.salesLedgerList = result || [];
            this.applyDefaultLedgerSelection();
        });
    }

    ngAfterViewInit(): void {
        this.cdr.detectChanges();
    }

    private initShortcuts(): void {
        const allowIn = [AllowIn.Textarea, AllowIn.Input, AllowIn.Select];
        this.shortcuts = [
            this.createShortcut('alt + t', 'Floor', () => this.switchContext('tables'), allowIn),
            this.createShortcut('alt + p', 'Product search', () => this.switchContext('menu', true), allowIn),
            this.createShortcut('alt + o', 'Open orders', () => this.switchContext('orders'), allowIn),
            this.createShortcut('alt + s', 'Save order', () => this.saveOrder(false), allowIn),
            this.createShortcut('alt + k', 'Send KOT/BOT', () => this.saveOrder(true), allowIn),
            this.createShortcut('alt + b', 'Checkout', () => this.openCheckout(), allowIn),
            this.createShortcut('esc', 'Close', () => this.closeActiveOverlay(), allowIn),
        ];
    }

    private createShortcut(
        key: string,
        label: string,
        command: () => void,
        allowIn: AllowIn[],
    ): ShortcutInput {
        return {
            key: [key],
            label,
            description: label,
            allowIn,
            preventDefault: true,
            command: (output) => {
                output.event?.preventDefault();
                if (!this.saving) {
                    command();
                }
                return false;
            },
        };
    }

    ngOnDestroy(): void {
        if (this.timerHandle) {
            clearInterval(this.timerHandle);
        }
        window.removeEventListener('online', this.onConnectionChange);
        window.removeEventListener('offline', this.onConnectionChange);
        document.removeEventListener('fullscreenchange', this.onFullscreenChange);
    }

    async toggleFullscreen(): Promise<void> {
        try {
            if (document.fullscreenElement) {
                await document.exitFullscreen();
                return;
            }

            await this.hostElement.nativeElement.requestFullscreen();
        } catch {
            this.notify.warn('Full-screen mode is unavailable in this browser.');
        }
    }

    private onFullscreenChange = (): void => {
        this.isFullscreen = document.fullscreenElement === this.hostElement.nativeElement;
        this.cdr.markForCheck();
    };

    private onConnectionChange = (): void => {
        this.browserOnline = navigator.onLine;
        this.serverReachable = false;
        if (this.browserOnline) this.loadCashShift();
        this.cdr.markForCheck();
    };

    loadCashShift(): void {
        this.cashShiftService.current().subscribe({
            next: (shift) => {
                this.cashShift = shift;
                this.serverReachable = true;
                this.cdr.markForCheck();
            },
            error: () => {
                this.serverReachable = false;
                this.cdr.markForCheck();
            },
        });
    }

    get posOnline(): boolean {
        return this.browserOnline && this.serverReachable;
    }

    openCashShift(): void {
        if (!this.posOnline) {
            this.notify.warn('Connect to the internet before opening a cash shift');
            return;
        }
        const value = window.prompt('Opening cash in the register', '0');
        if (value === null) return;
        const amount = Number(value);
        if (!Number.isFinite(amount) || amount < 0) {
            this.notify.warn('Enter a valid opening cash amount');
            return;
        }
        this.cashShiftService.open(amount).subscribe((shift) => {
            this.cashShift = shift;
            this.notify.success('Cash shift opened');
        });
    }

    recordCashMovement(isCashIn: boolean): void {
        if (!this.cashShift || !this.posOnline) return;
        const amountText = window.prompt(isCashIn ? 'Cash received into register' : 'Cash removed from register');
        if (amountText === null) return;
        const amount = Number(amountText);
        const reason = (window.prompt('Reason for this cash movement') || '').trim();
        if (!Number.isFinite(amount) || amount <= 0 || !reason) {
            this.notify.warn('Enter a valid amount and reason');
            return;
        }
        this.cashShiftService.movement(this.cashShift.id, isCashIn, amount, reason).subscribe((shift) => {
            this.cashShift = shift;
            this.notify.success(isCashIn ? 'Cash added to shift' : 'Cash withdrawal recorded');
        });
    }

    closeCashShift(): void {
        if (!this.cashShift || !this.posOnline) return;
        const countedText = window.prompt(`Counted cash (expected ${this.cashShift.expectedClosingCash.toFixed(2)})`);
        if (countedText === null) return;
        const counted = Number(countedText);
        if (!Number.isFinite(counted) || counted < 0) {
            this.notify.warn('Enter a valid counted cash amount');
            return;
        }
        const variance = Math.abs(counted - this.cashShift.expectedClosingCash) > 0.0001;
        const note = variance ? (window.prompt('Explain the cash difference') || '').trim() : '';
        if (variance && !note) {
            this.notify.warn('A reason is required for a cash difference');
            return;
        }
        const managerPin = variance ? (window.prompt('Manager approval PIN') || '') : '';
        this.cashShiftService.close(this.cashShift.id, counted, note, managerPin).subscribe((shift) => {
            this.cashShift = null;
            this.notify.success(`Shift closed. Cash variance: ${Number(shift.cashVariance || 0).toFixed(2)}`);
        });
    }

    refresh(): void {
        this.loading = true;
        this.restaurantSetupService.getAreas().subscribe((result) => (this.areas = result || []));
        this.restaurantSetupService.getTables(null).subscribe((result) => {
            this.tables = result || [];
            this.recalculateViewState();
        });
        this.restaurantMenuService.getCategories().subscribe((result) => (this.categories = result || []));
        this.restaurantMenuService.getPosMenu(null, null).subscribe((result) => {
            this.menuItems = result || [];
            this.recalculateViewState();
        });
        this.restaurantOrderService
            .getOpenOrdersForPos()
            .pipe(finalize(() => (this.loading = false)))
            .subscribe((result) => {
                this.openOrders = result || [];
                this.recalculateViewState();
            });
    }

    selectTable(table: RestaurantTableDto): void {
        if (this.shouldProtectCurrentOrder('table', table.id)) {
            this.pendingOrderSwitch = { kind: 'table', table };
            return;
        }

        this.performSelectTable(table);
    }

    private performSelectTable(table: RestaurantTableDto): void {
        this.orderType = RestaurantOrderType.DineIn;
        this.activeContext = 'menu';
        this.selectedTable = table;
        this.selectedOrder = this.openOrders.find((order) => order.tableId === table.id) || null;
        this.cart = this.selectedOrder ? this.hydrateCartLines(this.selectedOrder.items || []) : [];
        this.syncCustomerFromOrder();
        this.orderDirty = false;
        this.stockValidation = null;
        this.resetBillSelection();
        this.resetTenderFields();
        this.cancelConfigurator(false);
        this.resetOperationForm(false);
        this.loadTickets();
        this.recalculateViewState();
        this.focusMenuSearch();
    }

    selectOrder(order: RestaurantOrderDto): void {
        if (this.shouldProtectCurrentOrder('order', order.id)) {
            this.pendingOrderSwitch = { kind: 'order', order };
            return;
        }

        this.performSelectOrder(order);
    }

    private performSelectOrder(order: RestaurantOrderDto): void {
        this.selectedOrder = order;
        this.selectedTable = this.tables.find((table) => table.id === order.tableId) || null;
        this.orderType = this.selectedTable
            ? RestaurantOrderType.DineIn
            : (order.orderType ?? RestaurantOrderType.TakeAway);
        this.activeContext = 'menu';
        this.cart = this.hydrateCartLines(order.items || []);
        this.syncCustomerFromOrder();
        this.orderDirty = false;
        this.stockValidation = null;
        this.resetBillSelection();
        this.resetTenderFields();
        this.cancelConfigurator(false);
        this.resetOperationForm(false);
        this.loadTickets();
        this.recalculateViewState();
        this.focusMenuSearch();
    }

    startTakeaway(): void {
        this.startNewOrder(RestaurantOrderType.TakeAway);
    }

    startDelivery(): void {
        this.startNewOrder(RestaurantOrderType.Delivery);
    }

    startNewOrder(orderType = RestaurantOrderType.TakeAway): void {
        this.requestStartNewOrder(orderType);
    }

    requestStartNewOrder(orderType = RestaurantOrderType.TakeAway): void {
        if (this.shouldProtectCurrentOrder('new', orderType.toString())) {
            this.pendingOrderSwitch = { kind: 'new', orderType };
            return;
        }

        this.performStartNewOrder(orderType);
    }

    private performStartNewOrder(orderType = RestaurantOrderType.TakeAway): void {
        this.orderType = orderType;
        this.activeContext = orderType === RestaurantOrderType.DineIn ? 'tables' : 'menu';
        this.selectedTable = null;
        this.selectedOrder = null;
        this.cart = [];
        this.tickets = [];
        this.billForm.customerName = '';
        this.billForm.customerPhoneNo = '';
        this.billForm.customerAddress = '';
        this.billForm.customerVatNo = '';
        this.orderDirty = false;
        this.mobileCartOpen = false;
        this.stockValidation = null;
        this.resetBillSelection();
        this.resetTenderFields();
        this.cancelConfigurator(false);
        this.resetOperationForm(false);
        this.recalculateViewState();
        if (this.activeContext === 'menu') {
            this.focusMenuSearch();
        }
    }

    switchContext(context: RestaurantPosContext, focusSearch = false): void {
        this.activeContext = context;
        if (context === 'menu' || focusSearch) {
            this.focusMenuSearch();
        }
        this.cdr.markForCheck();
    }

    saveAndSwitchOrder(): void {
        if (!this.pendingOrderSwitch) {
            return;
        }

        this.saveOrder(false, () => {
            const pending = this.pendingOrderSwitch;
            this.pendingOrderSwitch = null;
            this.executePendingSwitch(pending);
        });
    }

    discardAndSwitchOrder(): void {
        const pending = this.pendingOrderSwitch;
        this.pendingOrderSwitch = null;
        this.executePendingSwitch(pending);
    }

    stayOnCurrentOrder(): void {
        this.pendingOrderSwitch = null;
        this.cdr.markForCheck();
    }

    private executePendingSwitch(pending: PendingOrderSwitch | null): void {
        if (!pending) {
            return;
        }

        if (pending.kind === 'table') {
            this.performSelectTable(pending.table);
        } else if (pending.kind === 'order') {
            this.performSelectOrder(pending.order);
        } else {
            this.performStartNewOrder(pending.orderType);
        }
    }

    private shouldProtectCurrentOrder(kind: PendingOrderSwitch['kind'], targetId: string): boolean {
        if (!this.orderDirty || (!this.cart.length && !this.selectedOrder)) {
            return false;
        }

        if (kind === 'table' && this.selectedTable?.id === targetId) {
            return false;
        }

        if (kind === 'order' && this.selectedOrder?.id === targetId) {
            return false;
        }

        return true;
    }

    private focusMenuSearch(): void {
        setTimeout(() => {
            this.cdr.detectChanges();
            this.menuSearchInput?.nativeElement.focus();
        }, 50);
    }

    private syncCustomerFromOrder(): void {
        this.billForm.customerName = this.selectedOrder?.customerName || '';
        this.billForm.customerPhoneNo = this.selectedOrder?.customerPhoneNo || '';
    }

    addMenuItem(item: RestaurantMenuItemDto): void {
        if (this.isSoldOut(item)) {
            this.notify.warn('Item is sold out');
            return;
        }

        if (this.getActiveVariants(item).length || this.getActiveModifierGroups(item).length) {
            this.openConfigurator(item);
            return;
        }

        this.pushConfiguredLine(item, null, []);
    }

    openConfigurator(item: RestaurantMenuItemDto): void {
        this.pendingMenuItem = item;
        const defaultVariant =
            this.getActiveVariants(item).find((variant) => variant.isDefault) || this.getActiveVariants(item)[0];
        this.pendingVariantId = defaultVariant?.id || '';
        this.pendingModifierIds = {};
        for (const group of this.getActiveModifierGroups(item)) {
            this.pendingModifierIds[group.id] = [];
        }
        this.recalculateViewState();
    }

    cancelConfigurator(recalculate = true): void {
        this.pendingMenuItem = null;
        this.pendingVariantId = '';
        this.pendingModifierIds = {};
        if (recalculate) {
            this.recalculateViewState();
        }
    }

    confirmConfiguredItem(): void {
        if (!this.pendingMenuItem) {
            return;
        }

        for (const group of this.pendingModifierGroups) {
            const selectedCount = (this.pendingModifierIds[group.id] || []).length;
            if ((group.isRequired || group.minSelect > 0) && selectedCount < Math.max(1, group.minSelect || 0)) {
                this.notify.warn(`${group.name}: select required add-ons`);
                return;
            }
        }

        this.pushConfiguredLine(this.pendingMenuItem, this.selectedVariantItem, this.selectedModifiers());
        this.cancelConfigurator(false);
        this.recalculateViewState();
    }

    selectCategory(categoryId: string): void {
        this.selectedCategoryId = categoryId;
        this.recalculateViewState();
    }

    selectArea(areaId: string): void {
        this.selectedAreaId = areaId;
        this.recalculateViewState();
    }

    onTableFilterChange(): void {
        this.recalculateViewState();
    }

    selectTableStatus(status: string): void {
        this.tableStatusFilter = status;
        this.recalculateViewState();
    }

    resetTableFilters(): void {
        this.tableSearch = '';
        this.selectedAreaId = '';
        this.tableStatusFilter = '';
        this.recalculateViewState();
    }

    onOrderFilterChange(): void {
        this.recalculateViewState();
    }

    onMenuFilterChange(): void {
        this.recalculateViewState();
    }

    onCartChanged(): void {
        this.orderDirty = true;
        this.recalculateViewState();
    }

    onCustomerChanged(): void {
        this.orderDirty = true;
        this.recalculateViewState();
    }

    onVariantChanged(): void {
        this.recalculatePendingConfigurator();
    }

    setPaymentMethod(method: number): void {
        this.billForm.paymentMethod = method;
        this.applyPaymentDefaults(true);
        this.recalculateTenderState();
    }

    get payButtonAmount(): number {
        return this.billPayableAmount;
    }

    get canFinalizePayment(): boolean {
        return (
            !this.saving &&
            !!this.selectedOrder?.id &&
            !!this.billForm.ledgerId &&
            !!this.billForm.salesAccountId &&
            this.billPayableAmount > 0
        );
    }

    get checkoutBlockReason(): string {
        if (!this.selectedOrder?.id) {
            return 'Save the order before payment.';
        }
        if (!this.billForm.ledgerId || !this.billForm.salesAccountId) {
            return 'Choose the required ledger and sales account under Advanced.';
        }
        if (this.billPayableAmount <= 0) {
            return 'There is no payable amount on this order.';
        }
        return '';
    }

    openCheckout(): void {
        if (this.saving) {
            return;
        }
        if (!this.cart.length) {
            this.notify.warn('Add at least one menu item before checkout');
            return;
        }

        if (!this.selectedOrder?.id || this.orderDirty) {
            this.saveOrder(false, () => this.showCheckout());
            return;
        }

        this.showCheckout();
    }

    private showCheckout(): void {
        this.applyDefaultLedgerSelection();
        this.advancedCheckoutOpen = !this.billForm.ledgerId || !this.billForm.salesAccountId;
        this.checkoutVisible = true;
        this.mobileCartOpen = false;
        this.recalculateViewState();
        setTimeout(() => this.checkoutModal?.show());
    }

    closeCheckout(force = false): void {
        if (this.saving && !force) {
            return;
        }
        this.checkoutModal?.hide();
    }

    onCheckoutShown(): void {
        this.checkoutVisible = true;
        setTimeout(() => this.checkoutCustomerInput?.nativeElement.focus());
    }

    onCheckoutHidden(): void {
        this.checkoutVisible = false;
        setTimeout(() => {
            if (window.matchMedia('(max-width: 959px)').matches) {
                this.mobileOrderButton?.nativeElement.focus();
            } else {
                this.checkoutButton?.nativeElement.focus();
            }
        });
    }

    openMobileCart(): void {
        this.mobileCartOpen = true;
        this.cdr.markForCheck();
    }

    closeMobileCart(): void {
        this.mobileCartOpen = false;
        this.cdr.markForCheck();
    }

    closeActiveOverlay(): void {
        if (this.pendingOrderSwitch) {
            this.stayOnCurrentOrder();
        } else if (this.pendingMenuItem) {
            this.cancelConfigurator();
        } else if (this.checkoutVisible) {
            this.closeCheckout();
        } else if (this.mobileCartOpen) {
            this.closeMobileCart();
        } else if (this.activeOperation) {
            this.resetOperationForm();
        }
    }

    showOperation(operation: 'discount' | 'transfer' | 'split' | 'merge'): void {
        if (!this.selectedOrder?.id) {
            this.notify.warn('Save or select an order first');
            return;
        }

        this.activeOperation = this.activeOperation === operation ? '' : operation;
    }

    applyDiscount(): void {
        if (!this.selectedOrder?.id) {
            this.notify.warn('Save or select an order first');
            return;
        }

        this.saving = true;
        this.restaurantOrderService
            .applyOrderDiscount(
                new ApplyRestaurantOrderDiscountDto({
                    orderId: this.selectedOrder.id,
                    discountAmount: Number(this.operationForm.discountAmount || 0),
                    approvalPin: undefined,
                    approvalNote: undefined,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.afterOrderChange(this.selectedOrder.id);
            });
    }

    transferTable(): void {
        if (!this.selectedOrder?.id || !this.operationForm.transferTableId) {
            this.notify.warn('Select target table');
            return;
        }

        this.saving = true;
        this.restaurantOrderService
            .transferTable(
                new TransferRestaurantTableDto({
                    orderId: this.selectedOrder.id,
                    newTableId: this.operationForm.transferTableId,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success('Table transferred');
                this.afterOrderChange(this.selectedOrder.id);
                this.restaurantSetupService.getTables(null).subscribe((tables) => {
                    this.tables = tables || [];
                    this.recalculateViewState();
                });
            });
    }

    toggleSplitItem(lineId: string, checked: boolean): void {
        const current = this.operationForm.splitItemIds || [];
        this.operationForm.splitItemIds = checked
            ? [...current.filter((id) => id !== lineId), lineId]
            : current.filter((id) => id !== lineId);
    }

    isSplitItemSelected(lineId: string): boolean {
        return (this.operationForm.splitItemIds || []).includes(lineId);
    }

    toggleMergeOrder(orderId: string, checked: boolean): void {
        const current = this.operationForm.mergeSourceOrderIds || [];
        this.operationForm.mergeSourceOrderIds = checked
            ? [...current.filter((id) => id !== orderId), orderId]
            : current.filter((id) => id !== orderId);
    }

    splitOrder(): void {
        if (!this.selectedOrder?.id || !this.operationForm.splitItemIds?.length) {
            this.notify.warn('Select items to split');
            return;
        }

        this.saving = true;
        this.restaurantOrderService
            .splitOrder(
                new SplitRestaurantOrderDto({
                    sourceOrderId: this.selectedOrder.id,
                    newTableId: this.operationForm.splitTableId || undefined,
                    orderItemIds: this.operationForm.splitItemIds,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe((newOrderId) => {
                this.notify.success('Order split');
                this.resetOperationForm();
                this.afterOrderChange(newOrderId);
            });
    }

    mergeOrders(): void {
        if (!this.selectedOrder?.id || !this.operationForm.mergeSourceOrderIds?.length) {
            this.notify.warn('Select orders to merge');
            return;
        }

        this.saving = true;
        this.restaurantOrderService
            .mergeOrders(
                new MergeRestaurantOrdersDto({
                    targetOrderId: this.selectedOrder.id,
                    sourceOrderIds: this.operationForm.mergeSourceOrderIds,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success('Orders merged');
                this.resetOperationForm();
                this.afterOrderChange(this.selectedOrder.id);
            });
    }

    clearDraftLines(): void {
        const before = this.cart.length;
        const hadDraftLines = this.draftLineCountValue;
        this.cart = this.cart.filter((line) => line.id || !this.canEditLine(line));
        if (before !== this.cart.length) {
            this.orderDirty = true;
        }
        this.recalculateViewState();
        if (before !== this.cart.length) {
            this.notify.info('Unsaved draft lines cleared');
        } else if (hadDraftLines) {
            this.notify.warn('Use the delete button to remove saved draft lines');
        }
    }

    removeLine(index: number): void {
        const line = this.cart[index];
        if (!line?.id) {
            this.cart.splice(index, 1);
            this.orderDirty = true;
            this.recalculateViewState();
            return;
        }

        this.voidLine(line, index);
    }

    lineTotal(line: PosCartLine): number {
        return (
            Number(line.qty || 0) * Number(line.rate || 0) +
            this.modifierTotalForLine(line) -
            Number(line.discountAmount || 0)
        );
    }

    modifierTotalForLine(line: PosCartLine): number {
        const unitTotal = line.modifierUnitTotal ?? this.getExistingModifierUnitTotal(line);
        return Number(line.qty || 0) * Number(unitTotal || 0);
    }

    increaseLine(line: PosCartLine): void {
        if (!this.canEditLine(line)) {
            return;
        }

        line.qty = (line.qty || 0) + 1;
        this.orderDirty = true;
        this.recalculateViewState();
    }

    decreaseLine(line: PosCartLine): void {
        if (!this.canEditLine(line)) {
            return;
        }

        line.qty = Math.max(1, (line.qty || 1) - 1);
        this.orderDirty = true;
        this.recalculateViewState();
    }

    saveOrder(sendToKitchen = false, onSaved?: () => void): void {
        if (this.saving) {
            return;
        }

        if (!this.posOnline) {
            if (sendToKitchen) {
                this.notify.warn('Kitchen dispatch is paused until the server connection returns');
            } else {
                this.persistLocalDraft();
                this.notify.info('Order draft saved on this browser. Reconnect and save it to send it to the server.');
            }
            return;
        }

        if (!this.cart.length) {
            this.notify.warn('Add at least one menu item');
            return;
        }

        if (this.cart.some((line) => !line.qty || line.qty <= 0)) {
            this.notify.warn('Quantity must be greater than zero');
            return;
        }

        if (this.orderType === RestaurantOrderType.DineIn && !this.selectedTable) {
            this.notify.warn('Select a table for dine in order');
            this.activeContext = 'tables';
            return;
        }

        this.saving = true;
        const creatingNewOrder = !this.selectedOrder?.id;
        const shouldSendToKitchen = sendToKitchen && this.hasKitchenSendableLines();
        let kitchenDispatchStarted = false;
        if (sendToKitchen && !shouldSendToKitchen) {
            this.notify.info('Stock items are direct sale; no KOT/BOT ticket needed');
        }

        const orderRequest = new CreateOrEditRestaurantOrderDto({
                    id: this.selectedOrder?.id,
                    orderType: this.selectedTable ? RestaurantOrderType.DineIn : this.orderType,
                    tableId: this.selectedTable?.id || undefined,
                    deviceId: undefined,
                    clientRequestId: undefined,
                    waiterUserId: undefined,
                    guestCount: 0,
                    customerName: this.billForm.customerName,
                    customerPhoneNo: this.billForm.customerPhoneNo,
                    notes: undefined,
                    source: 'Web POS',
                    items: this.cart.map(
                        (line) =>
                            new CreateOrEditRestaurantOrderItemDto({
                                id: line.id || undefined,
                                menuItemId: line.menuItemId || undefined,
                                variantId: line.variantId || undefined,
                                productId: line.productId,
                                unitId: line.unitId || undefined,
                                taxId: line.taxId || undefined,
                                stationId: line.stationId || undefined,
                                qty: Number(line.qty || 0),
                                rate: Number(line.rate || 0),
                                discountAmount: Number(line.discountAmount || 0),
                                notes: line.notes || '',
                                modifiers: this.buildModifierPayload(line),
                            }),
                    ),
                });
        if (creatingNewOrder) orderRequest.clientRequestId = this.getOrderCreationRequestId(orderRequest);

        this.restaurantOrderService
            .createOrEditOrder(orderRequest)
            .pipe(finalize(() => {
                if (!shouldSendToKitchen || !kitchenDispatchStarted) this.saving = false;
            }))
            .subscribe((orderId) => {
                localStorage.removeItem(this.localDraftStorageKey());
                this.localDraftAvailable = false;
                if (creatingNewOrder) localStorage.removeItem(this.orderCreationStorageKey());
                this.notify.success(this.l('SavedSuccessfully'));
                this.orderDirty = false;
                if (shouldSendToKitchen) {
                    kitchenDispatchStarted = true;
                    this.sendToKitchen(orderId, onSaved);
                } else {
                    this.afterOrderChange(orderId, onSaved);
                }
            });
    }

    private orderCreationStorageKey(): string {
        return `restaurant-new-order:${this.appSession.tenantId ?? 'host'}:${this.appSession.userId ?? 'unknown'}`;
    }

    private localDraftStorageKey(): string {
        return `restaurant-pos-draft:${this.appSession.tenantId ?? 'host'}:${this.appSession.userId ?? 'unknown'}`;
    }

    private loadLocalDraftState(): void {
        try {
            const draft = JSON.parse(localStorage.getItem(this.localDraftStorageKey()) || 'null');
            this.localDraftAvailable = Array.isArray(draft?.cart) && draft.cart.length > 0;
            this.localDraftSavedAt = draft?.savedAt ? new Date(draft.savedAt) : null;
        } catch {
            localStorage.removeItem(this.localDraftStorageKey());
            this.localDraftAvailable = false;
        }
    }

    restoreLocalDraft(): void {
        try {
            const draft = JSON.parse(localStorage.getItem(this.localDraftStorageKey()) || 'null');
            if (!Array.isArray(draft?.cart) || !draft.cart.length) {
                this.localDraftAvailable = false;
                return;
            }
            if (draft.orderId) {
                const order = this.openOrders.find((row) => row.id === draft.orderId);
                if (!order) {
                    this.notify.warn('The saved order is no longer open. Refresh orders before restoring this draft.');
                    return;
                }
                this.selectedOrder = order;
                this.selectedTable = this.tables.find((table) => table.id === order.tableId) || null;
            } else {
                this.selectedOrder = null;
                this.selectedTable = this.tables.find((table) => table.id === draft.tableId) || null;
            }
            this.orderType = draft.orderType ?? RestaurantOrderType.TakeAway;
            this.cart = draft.cart;
            this.billForm.customerName = draft.customerName || '';
            this.billForm.customerPhoneNo = draft.customerPhoneNo || '';
            this.orderDirty = true;
            this.activeContext = 'menu';
            this.recalculateViewState();
            this.notify.success('Local order draft restored. Save it to send the latest version to the server.');
        } catch {
            this.notify.error('The local order draft could not be restored');
        }
    }

    private persistLocalDraft(): void {
        if (!this.orderDirty || !this.cart.length) return;
        try {
            const draft = {
                orderId: this.selectedOrder?.id || null,
                orderType: this.orderType,
                tableId: this.selectedTable?.id || null,
                customerName: this.billForm.customerName,
                customerPhoneNo: this.billForm.customerPhoneNo,
                cart: this.cart,
                savedAt: new Date().toISOString(),
            };
            localStorage.setItem(this.localDraftStorageKey(), JSON.stringify(draft));
            this.localDraftAvailable = true;
            this.localDraftSavedAt = new Date(draft.savedAt);
        } catch {
            this.localDraftAvailable = false;
        }
    }

    private getOrderCreationRequestId(request: CreateOrEditRestaurantOrderDto): string {
        const body = request.toJSON();
        delete body.clientRequestId;
        const signature = JSON.stringify(body);
        const key = this.orderCreationStorageKey();
        try {
            const previous = JSON.parse(localStorage.getItem(key) || 'null');
            if (previous?.signature === signature && previous?.id) return previous.id;
            const id = this.newRequestId();
            localStorage.setItem(key, JSON.stringify({ signature, id }));
            return id;
        } catch {
            return this.newRequestId();
        }
    }

    sendToKitchen(orderId?: string, onSaved?: () => void): void {
        const id = orderId || this.selectedOrder?.id;
        if (!id) {
            this.saving = false;
            return;
        }

        if (!this.hasKitchenSendableLines()) {
            this.notify.info('No kitchen/bar draft items to send');
            this.saving = false;
            return;
        }

        this.saving = true;
        const isAddOn = this.hasAddOnDraftLines();
        const stockDraftCount = this.draftStockLineCountValue;
        this.restaurantOrderService
            .sendToKitchen(new EntityDtoOfGuid({ id }))
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(isAddOn ? 'Add-on KOT/BOT sent' : 'KOT/BOT sent');
                if (stockDraftCount) {
                    this.notify.info(`${stockDraftCount} stock item(s) kept for direct billing`);
                }
                this.afterOrderChange(id, onSaved);
            });
    }

    voidLine(line: PosCartLine, index?: number): void {
        const isDraft = line.status === undefined || line.status === 0;
        const reason = isDraft ? 'Draft item removed' : window.prompt('Void reason');
        if (!isDraft && !reason) {
            return;
        }

        if (!line.id) {
            if (index !== undefined) {
                this.cart.splice(index, 1);
            }
            this.recalculateViewState();
            return;
        }

        this.restaurantOrderService
            .voidOrderItem(
                new VoidRestaurantOrderItemDto({
                    orderItemId: line.id,
                    reason: reason || '',
                    approvalPin: undefined,
                    approvalNote: undefined,
                }),
            )
            .subscribe(() => {
                this.notify.success('Item voided');
                this.afterOrderChange(this.selectedOrder?.id);
            });
    }

    validateStock(): void {
        if (!this.selectedOrder?.id) {
            this.saveOrder(false);
            return;
        }

        const billLines = this.billLinesForCurrentMode();
        if (billLines === null) {
            return;
        }

        this.restaurantBillingService
            .validateOrderStock(new ValidateRestaurantBillStockDto({ id: this.selectedOrder.id, billLines }))
            .subscribe((result) => (this.stockValidation = result));
    }

    finalizeBill(): void {
        if (this.saving) {
            return;
        }

        if (!this.posOnline) {
            this.notify.warn('Billing is unavailable while disconnected. Your current order remains saved on the server.');
            return;
        }

        if (!this.cashShift) {
            this.notify.warn('Open a cashier shift before billing');
            return;
        }

        if (!this.selectedOrder?.id) {
            this.notify.warn('Save the order before final billing');
            return;
        }

        if (!this.billForm.ledgerId || !this.billForm.salesAccountId) {
            this.advancedCheckoutOpen = true;
            this.notify.warn('Customer ledger and sales account are required');
            return;
        }

        const billLines = this.billLinesForCurrentMode();
        if (billLines === null) {
            return;
        }

        if (!this.validateTender()) {
            return;
        }

        this.saving = true;
        this.restaurantBillingService
            .validateOrderStock(new ValidateRestaurantBillStockDto({ id: this.selectedOrder.id, billLines }))
            .subscribe(
                (validation) => {
                    this.stockValidation = validation;
                    const shortages = this.getStockShortages(validation);
                    if (shortages.length) {
                        this.saving = false;
                        this.message.confirm(
                            this.formatStockWarnings(shortages),
                            this.l('Insufficient stock'),
                            (isConfirmed) => {
                                if (isConfirmed) {
                                    this.postBill(true, billLines);
                                }
                            },
                        );
                        return;
                    }

                    this.postBill(false, billLines);
                },
                () => (this.saving = false),
            );
    }

    private postBill(confirmNegativeStock: boolean, billLines?: FinalizeRestaurantBillLineDto[]): void {
        const receiptSnapshot = this.buildBillReceiptSnapshot();
        const billRequest = new FinalizeRestaurantBillDto({
            orderId: this.selectedOrder.id,
            dateMiti: this.billForm.dateMiti,
            salesAccountId: this.billForm.salesAccountId,
            ledgerId: this.billForm.ledgerId,
            customerName: this.billForm.customerName,
            customerAddress: this.billForm.customerAddress,
            customerVatNo: this.billForm.customerVatNo,
            customerPhoneNo: this.billForm.customerPhoneNo,
            paymentMethod: this.billForm.paymentMethod,
            paymentMethodLedgerId: this.billForm.paymentMethodLedgerId || undefined,
            tipAmount: Number(this.billForm.tipAmount || 0),
            customerPaidAmount:
                this.billForm.customerPaidAmount === null ? undefined : Number(this.billForm.customerPaidAmount || 0),
            isPrint: this.billForm.isPrint,
            confirmNegativeStock,
            cashShiftId: this.cashShift?.id,
            tenders: this.billForm.paymentMethod === PaymentMethod.Credit ? [] : [{
                paymentMethod: this.billForm.paymentMethod,
                paymentLedgerId: this.billForm.paymentMethodLedgerId || undefined,
                amount: this.billPayableAmount,
                receivedAmount: this.billForm.customerPaidAmount === null
                    ? this.billPayableAmount
                    : Number(this.billForm.customerPaidAmount || 0),
            }],
            billLines,
        });
        billRequest.clientRequestId = this.getBillingRequestId(billRequest);

        this.saving = true;
        this.restaurantBillingService
            .finalizeBill(billRequest)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe((result) => {
                localStorage.removeItem(`${this.orderCreationStorageKey()}:restaurant-bill:${billRequest.orderId}`);
                this.loadCashShift();
                const stockWarnings =
                    result.stockWarnings || (confirmNegativeStock ? this.getStockShortages(this.stockValidation) : []);
                this.notify.success(
                    `${result.isFullyBilled ? 'Bill posted' : 'Split bill posted'}: ${result.salesMasterId}`,
                );
                if (stockWarnings.length) {
                    this.message.warn(this.formatStockWarnings(stockWarnings), this.l('Negative stock posted'));
                }
                if (this.billForm.isPrint) {
                    this.printOrDownloadPosBill(receiptSnapshot, result);
                }
                this.closeCheckout(true);
                this.stockValidation = null;
                this.resetBillSelection();
                this.resetTenderFields();
                if (result.isFullyBilled) {
                    this.performStartNewOrder(this.orderType);
                    this.refresh();
                    return;
                }

                this.afterOrderChange(result.orderId);
                this.restaurantMenuService.getPosMenu(null, null).subscribe((menuItems) => {
                    this.menuItems = menuItems || [];
                    this.recalculateViewState();
                });
            });
    }

    private getStockShortages(
        validation: RestaurantStockValidationDto | null | undefined,
    ): RestaurantMaterialRequirementDto[] {
        return (validation?.requirements || []).filter((row) => !row.isAvailable);
    }

    private formatStockWarnings(rows: RestaurantMaterialRequirementDto[]): string {
        return rows
            .map(
                (row) =>
                    `${row.productName}: ${row.requiredQty} ${row.unitName} required, ${row.availableQty} available`,
            )
            .join('\n');
    }

    loadTickets(orderId?: string): void {
        const id = orderId || this.selectedOrder?.id;
        if (!id) {
            this.tickets = [];
            this.recalculateViewState();
            return;
        }

        this.restaurantOrderService.getTicketsForOrder(id).subscribe((result) => {
            this.tickets = result || [];
            this.recalculateViewState();
        });
    }

    printTicket(ticket: RestaurantTicketDto, reprint = false): void {
        if (!reprint) {
            this.notify.info('This ticket was sent to the configured Print Agent route. Check the print station if it is waiting or failed.');
            return;
        }
        const reason = reprint ? (window.prompt('Reason for reprinting this KOT/BOT') || '').trim() : '';
        if (reprint && !reason) return;
        const approvalPin = reprint ? (window.prompt('Manager PIN (if required)') || '') : '';
        this.restaurantOrderService.reprintTicket(
            new ReprintRestaurantTicketDto({ ticketId: ticket.id, approvalPin: approvalPin || undefined, approvalNote: reason }),
        ).subscribe((printTicket) => {
            this.notify.success('Audited reprint queued for the configured Print Agent route.');
            this.loadTickets(printTicket.orderId);
        });
    }

    private getBillingRequestId(request: FinalizeRestaurantBillDto): string {
        const signatureData = request.toJSON();
        delete signatureData.clientRequestId;
        const signature = JSON.stringify(signatureData);
        const storageKey = `restaurant-bill:${request.orderId}`;
        try {
            const scopedKey = `${this.orderCreationStorageKey()}:${storageKey}`;
            const prior = JSON.parse(localStorage.getItem(scopedKey) || 'null');
            if (prior?.signature === signature && prior?.id) return prior.id;
            const id = this.newRequestId();
            localStorage.setItem(scopedKey, JSON.stringify({ signature, id }));
            return id;
        } catch {
            return this.newRequestId();
        }
    }

    private newRequestId(): string {
        return typeof crypto !== 'undefined' && 'randomUUID' in crypto
            ? crypto.randomUUID()
            : `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    }

    cancelTicket(ticket: RestaurantTicketDto): void {
        if (!ticket?.id || !this.canCancelTicket(ticket)) {
            return;
        }

        const reason = (window.prompt('Cancel KOT/BOT reason') || '').trim();
        if (!reason) {
            return;
        }

        this.saving = true;
        this.restaurantOrderService
            .cancelTicket(
                new CancelRestaurantTicketDto({
                    ticketId: ticket.id,
                    reason,
                    approvalPin: undefined,
                    approvalNote: undefined,
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success('KOT/BOT cancelled');
                this.stockValidation = null;
                this.afterOrderChange(ticket.orderId || this.selectedOrder?.id);
            });
    }

    toggleModifier(group: PosModifierGroup, modifier: RestaurantModifierDto, checked: boolean): void {
        const current = this.pendingModifierIds[group.id] || [];
        if (checked) {
            if (group.maxSelect > 0 && current.length >= group.maxSelect) {
                this.notify.warn(`${group.name}: maximum ${group.maxSelect}`);
                return;
            }

            this.pendingModifierIds[group.id] = [...current, modifier.id];
            this.recalculatePendingConfigurator();
            return;
        }

        this.pendingModifierIds[group.id] = current.filter((id) => id !== modifier.id);
        this.recalculatePendingConfigurator();
    }

    isModifierSelected(groupId: string, modifierId: string): boolean {
        return (this.pendingModifierIds[groupId] || []).includes(modifierId);
    }

    onMergeOrderChanged(orderId: string, event: Event): void {
        this.toggleMergeOrder(orderId, (event.target as HTMLInputElement).checked);
    }

    onModifierChanged(group: PosModifierGroup, modifier: RestaurantModifierDto, event: Event): void {
        this.toggleModifier(group, modifier, (event.target as HTMLInputElement).checked);
    }

    selectedModifierSummary(): string {
        return this.selectedModifiers()
            .map((modifier) => modifier.name)
            .filter(Boolean)
            .join(', ');
    }

    lineName(line: PosCartLine): string {
        return line.itemNameSnapshot || line.productName || line.displayName || '';
    }

    lineVariantText(line: PosCartLine): string {
        return line.variantNameSnapshot || line.variantName || '';
    }

    lineModifierText(line: PosCartLine): string {
        return (
            line.modifierSummary ||
            this.getLineModifiers(line)
                .map((modifier) => modifier.name || modifier.modifierNameSnapshot)
                .join(', ')
        );
    }

    statusText(status: number): string {
        return ['Draft', 'Sent', 'Preparing', 'Ready', 'Served', 'Cancelled'][status] || 'Unknown';
    }

    ticketStatusText(status: number): string {
        return ['Pending', 'In Progress', 'Ready', 'Served', 'Cancelled'][status] || 'Unknown';
    }

    ticketPurposeText(purpose: number): string {
        return purpose === RestaurantTicketPurpose.Cancellation
            ? 'Cancellation'
            : purpose === RestaurantTicketPurpose.AddOn
              ? 'AddOn'
              : 'New Order';
    }

    orderStatusText(status: number): string {
        return ['Draft', 'Sent', 'In Progress', 'Ready', 'Served', 'Billed', 'Closed', 'Cancelled'][status] || 'Open';
    }

    indexOrderNo(orderNo: string): string {
        const value = (orderNo || '').toString().trim();
        if (!value) {
            return '#';
        }

        const parts = value.split(/[-/]/).filter(Boolean);
        return parts.length ? parts[parts.length - 1].slice(-3) : value.slice(-3);
    }

    canEditLine(line: PosCartLine): boolean {
        return !line.id || line.status === 0;
    }

    lineStatusClass(status: number): string {
        if (status === 2) {
            return 'bg-light-warning text-warning';
        }
        if (status === 3 || status === 4) {
            return 'bg-light-success text-success';
        }
        if (status === 5) {
            return 'bg-light-danger text-danger';
        }
        return status === 1 ? 'bg-light-primary text-primary' : 'bg-light text-gray-600';
    }

    ticketStatusClass(status: number): string {
        if (status === 1) {
            return 'bg-light-warning text-warning';
        }
        if (status === 2 || status === 3) {
            return 'bg-light-success text-success';
        }
        if (status === 4) {
            return 'bg-light-danger text-danger';
        }
        return 'bg-light text-gray-600';
    }

    tableStatusClass(status: number): string {
        return status === 1
            ? 'bg-light-warning text-warning'
            : status === 3
              ? 'bg-light-danger text-danger'
              : 'bg-light-success text-success';
    }

    tableStatusText(status: number): string {
        return ['Available', 'Occupied', 'Reserved', 'Maintenance'][status] || 'Available';
    }

    paymentMethodText(method: number): string {
        if (method === PaymentMethod.Card_Swipe) {
            return 'Card (manually recorded)';
        }
        if (method === PaymentMethod.QR) {
            return 'QR (manually recorded)';
        }
        if (method === PaymentMethod.Credit) {
            return 'Credit';
        }
        if (method === PaymentMethod.Cheque) {
            return 'Cheque';
        }
        return 'Cash';
    }

    onTipChanged(): void {
        if (Number(this.billForm.tipAmount || 0) < 0) {
            this.billForm.tipAmount = 0;
        }
        this.recalculateViewState();
    }

    onCustomerPaidChanged(): void {
        if (Number(this.billForm.customerPaidAmount || 0) < 0) {
            this.billForm.customerPaidAmount = 0;
        }
        this.recalculateTenderState();
    }

    canCancelTicket(ticket: RestaurantTicketDto): boolean {
        return (
            !!ticket?.id &&
            ticket.purpose !== RestaurantTicketPurpose.Cancellation &&
            ticket.status !== RestaurantTicketStatus.Cancelled &&
            ticket.status !== RestaurantTicketStatus.Served
        );
    }

    orderForTable(table: RestaurantTableDto): RestaurantOrderDto | undefined {
        return this.openOrders.find((order) => order.tableId === table?.id);
    }

    showTableTimer(order: RestaurantOrderDto | null = this.selectedOrder): boolean {
        return !!order?.sentAt && !!this.tableTimerStart(order);
    }

    tableTimerText(order: RestaurantOrderDto | null = this.selectedOrder): string {
        return this.formatElapsedFrom(this.tableTimerStart(order));
    }

    ticketElapsedText(ticket: RestaurantTicketDto): string {
        return this.formatElapsedFrom(ticket?.sentAt);
    }

    formatClock(value: DateTimeInput): string {
        const dateTime = this.toDateTime(value);
        return dateTime ? dateTime.toFormat('HH:mm') : '-';
    }

    formatNepaliDateTime(value: DateTimeInput): string {
        const dateTime = this.toDateTime(value);
        if (!dateTime) {
            return '';
        }

        try {
            const nepaliDate = this.nepaliDateService.engToNepDate(dateTime.day, dateTime.month - 1, dateTime.year);
            const dateText = `${nepaliDate.year}/${this.padDatePart(nepaliDate.month)}/${this.padDatePart(nepaliDate.day)}`;
            return `${dateText} ${dateTime.toFormat('HH:mm')}`;
        } catch {
            return dateTime.toFormat('yyyy-LL-dd HH:mm');
        }
    }

    orderTypeText(orderType: number): string {
        return ['Dine In', 'Take Away', 'Delivery', 'Mobile', 'QR'][orderType] || 'Order';
    }

    setBillMode(mode: 'full' | 'split'): void {
        if (this.billMode === mode) {
            return;
        }

        this.billMode = mode;
        this.stockValidation = null;
        if (mode === 'full') {
            this.splitBillQuantities = {};
        }
        this.recalculateViewState();
    }

    getLineBilledQty(line: PosCartLine): number {
        return Math.max(0, Number(line?.billedQty || 0));
    }

    getLineUnbilledQty(line: PosCartLine): number {
        const qty = Math.max(0, Number(line?.qty || 0));
        if (line?.unbilledQty !== undefined && line?.unbilledQty !== null) {
            return Math.min(qty, Math.max(0, Number(line.unbilledQty || 0)));
        }

        return Math.max(0, qty - this.getLineBilledQty(line));
    }

    lineAmountForQty(line: PosCartLine, qty: number): number {
        const lineQty = Math.max(0, Number(line?.qty || 0));
        const selectedQty = Math.max(0, Math.min(Number(qty || 0), this.getLineUnbilledQty(line)));
        if (!lineQty || !selectedQty) {
            return 0;
        }

        const lineAmount =
            line?.amount !== undefined && line?.amount !== null ? Number(line.amount || 0) : this.lineTotal(line);
        return (lineAmount / lineQty) * selectedQty;
    }

    onSplitBillQtyChanged(line: PosCartLine): void {
        this.setSplitBillQty(line, Number(this.splitBillQuantities[line.id] || 0));
    }

    setSplitBillQty(line: PosCartLine, qty: number): void {
        if (!line?.id) {
            return;
        }

        const maxQty = this.getLineUnbilledQty(line);
        const nextQty = Math.max(0, Math.min(Number(qty || 0), maxQty));
        if (nextQty > 0) {
            this.splitBillQuantities[line.id] = nextQty;
        } else {
            delete this.splitBillQuantities[line.id];
        }
        this.stockValidation = null;
        this.recalculateViewState();
    }

    selectedBillLines(): FinalizeRestaurantBillLineDto[] {
        return this.unbilledLinesList
            .map((line) => ({
                line,
                qty: Math.min(Number(this.splitBillQuantities[line.id] || 0), this.getLineUnbilledQty(line)),
            }))
            .filter((entry) => entry.qty > 0)
            .map(
                (entry) =>
                    new FinalizeRestaurantBillLineDto({
                        orderItemId: entry.line.id,
                        qty: entry.qty,
                    }),
            );
    }

    billLinesForCurrentMode(): FinalizeRestaurantBillLineDto[] | undefined | null {
        if (this.billMode === 'full') {
            return undefined;
        }

        if (!this.unbilledLinesList.length) {
            this.notify.warn('No unbilled items left to split');
            return null;
        }

        const billLines = this.selectedBillLines();
        if (!billLines.length) {
            this.notify.warn('Select at least one unbilled quantity');
            return null;
        }

        return billLines;
    }

    private billBaseAmount(): number {
        if (this.billMode === 'split') {
            return Number(this.selectedBillTotalAmount || 0);
        }

        if (this.selectedOrder?.remainingGrandTotal !== undefined && this.selectedOrder?.remainingGrandTotal !== null) {
            return Number(this.selectedOrder.remainingGrandTotal || 0);
        }

        return Number(this.cartTotalAmount || 0);
    }

    private recalculateTenderState(): void {
        const previousPayableAmount = Number(this.billPayableAmount || 0);
        const tipAmount = Math.max(0, Number(this.billForm.tipAmount || 0));
        const payableAmount = this.billBaseAmount() + tipAmount;
        const method = Number(this.billForm.paymentMethod);
        const paidWasBlank =
            this.billForm.customerPaidAmount === null ||
            this.billForm.customerPaidAmount === undefined ||
            this.billForm.customerPaidAmount === '';

        this.billPayableAmount = payableAmount;

        if (method === PaymentMethod.Credit) {
            this.billForm.customerPaidAmount = 0;
            this.billReturnAmount = 0;
            return;
        }

        if (
            this.shouldAutoTenderPayment(method) &&
            (paidWasBlank || Number(this.billForm.customerPaidAmount || 0) === previousPayableAmount)
        ) {
            this.billForm.customerPaidAmount = payableAmount;
        }

        const paidAmount = Math.max(0, Number(this.billForm.customerPaidAmount || 0));
        this.billReturnAmount = Math.max(0, paidAmount - payableAmount);
    }

    private applyPaymentDefaults(force = false): void {
        const method = Number(this.billForm.paymentMethod);
        const paidWasBlank =
            this.billForm.customerPaidAmount === null ||
            this.billForm.customerPaidAmount === undefined ||
            this.billForm.customerPaidAmount === '';

        if (method === PaymentMethod.Credit) {
            this.billForm.customerPaidAmount = 0;
            return;
        }

        if (this.shouldAutoTenderPayment(method) && (force || paidWasBlank)) {
            this.billForm.customerPaidAmount = this.billPayableAmount;
        }
    }

    private shouldAutoTenderPayment(method: number): boolean {
        return method === PaymentMethod.Cash || method === PaymentMethod.Card_Swipe;
    }

    private validateTender(): boolean {
        this.recalculateTenderState();

        if (Number(this.billForm.tipAmount || 0) < 0) {
            this.notify.warn('Tip cannot be negative');
            return false;
        }

        if (Number(this.billForm.customerPaidAmount || 0) < 0) {
            this.notify.warn('Customer paid cannot be negative');
            return false;
        }

        if (
            Number(this.billForm.paymentMethod) !== PaymentMethod.Credit &&
            Number(this.billForm.customerPaidAmount || 0) + 0.0001 < this.billPayableAmount
        ) {
            this.notify.warn('Customer paid amount is less than payable total');
            return false;
        }

        return true;
    }

    private tableTimerStart(order: RestaurantOrderDto | null | undefined): DateTime | undefined {
        return order?.tableSessionOpenedAt || order?.createdAt;
    }

    private formatElapsedFrom(value: DateTimeInput): string {
        const start = this.toDateTime(value);
        if (!start) {
            return '';
        }

        const totalMinutes = Math.max(0, Math.floor(this.currentClock.diff(start, 'minutes').minutes));
        const days = Math.floor(totalMinutes / 1440);
        const hours = Math.floor((totalMinutes % 1440) / 60);
        const minutes = totalMinutes % 60;

        if (days > 0) {
            return `${days}d ${hours}h`;
        }

        if (hours > 0) {
            return `${hours}h ${minutes}m`;
        }

        return `${minutes}m`;
    }

    private toDateTime(value: DateTimeInput): DateTime | null {
        if (!value) {
            return null;
        }

        if (DateTime.isDateTime(value)) {
            return value;
        }

        const parsed = DateTime.fromISO(value.toString());
        return parsed.isValid ? parsed : null;
    }

    hasAddOnDraftLines(): boolean {
        return (
            !!this.selectedOrder?.id &&
            this.draftKitchenLineCountValue > 0 &&
            this.tickets.some((ticket) => ticket.purpose !== RestaurantTicketPurpose.Cancellation)
        );
    }

    sendButtonLabel(): string {
        return this.hasAddOnDraftLines() ? 'Send Add-on KOT/BOT' : 'Send KOT/BOT';
    }

    trackByEntityId(index: number, item: { id?: string } | null | undefined): string | number {
        return item?.id || index;
    }

    trackByCartLine(index: number, line: PosCartLine): string | number {
        return line.id || line.configKey || index;
    }

    isSoldOut(item: RestaurantMenuItemDto): boolean {
        return item?.isAvailable === false;
    }

    isKitchenItem(item: RestaurantMenuItemDto): boolean {
        return !!item?.stationId;
    }

    itemRouteText(item: RestaurantMenuItemDto): string {
        return this.isKitchenItem(item) ? 'Kitchen Item' : 'Stock Item';
    }

    itemRouteClass(item: RestaurantMenuItemDto): string {
        return this.isKitchenItem(item) ? 'bg-light-warning text-warning' : 'bg-light-success text-success';
    }

    isKitchenLine(line: PosCartLine): boolean {
        return !!line?.stationId;
    }

    lineRouteText(line: PosCartLine): string {
        return this.isKitchenLine(line) ? 'Kitchen Item' : 'Stock Item';
    }

    lineRouteClass(line: PosCartLine): string {
        return this.isKitchenLine(line) ? 'bg-light-warning text-warning' : 'bg-light-success text-success';
    }

    hasKitchenSendableLines(): boolean {
        return this.cart.some((line) => this.canEditLine(line) && this.isKitchenLine(line));
    }

    itemInitial(item: RestaurantMenuItemDto): string {
        return (item?.displayName || '?').trim().charAt(0).toUpperCase() || '?';
    }

    getActiveVariants(item: RestaurantMenuItemDto): RestaurantMenuVariantDto[] {
        return (item?.variants || []).filter((variant) => variant.isActive !== false);
    }

    getActiveModifierGroups(item: RestaurantMenuItemDto): PosModifierGroup[] {
        return (item?.modifierGroups || []).filter((group) => group.isActive !== false);
    }

    getActiveModifiers(group: PosModifierGroup): RestaurantModifierDto[] {
        return (group?.modifiers || []).filter((modifier) => modifier.isActive !== false);
    }

    private recalculateViewState(): void {
        const tableSearch = (this.tableSearch || '').toLowerCase().trim();
        this.tableStatusCounts = {};
        this.matchingTableCount = 0;
        this.filteredTablesList = this.tables.filter((table) => {
            const areaMatch = !this.selectedAreaId || table.areaId === this.selectedAreaId;
            const statusMatch =
                this.tableStatusFilter === '' || Number(table.status) === Number(this.tableStatusFilter);
            const searchMatch =
                !tableSearch ||
                (table.name || '').toLowerCase().includes(tableSearch) ||
                (table.code || '').toLowerCase().includes(tableSearch) ||
                (table.areaName || '').toLowerCase().includes(tableSearch);
            const matches = table.isActive !== false && areaMatch && searchMatch;
            if (matches) {
                this.matchingTableCount++;
                const status = String(table.status);
                this.tableStatusCounts[status] = (this.tableStatusCounts[status] || 0) + 1;
            }
            return matches && statusMatch;
        });

        const orderSearch = (this.orderSearch || '').toLowerCase().trim();
        this.filteredOrdersList = this.openOrders.filter((order) => {
            const typeMatch =
                this.orderTypeFilter === '' || Number(order.orderType) === Number(this.orderTypeFilter);
            const statusMatch =
                this.orderStatusFilter === '' || Number(order.status) === Number(this.orderStatusFilter);
            const searchMatch =
                !orderSearch ||
                (order.orderNo || '').toLowerCase().includes(orderSearch) ||
                (order.tableName || '').toLowerCase().includes(orderSearch) ||
                (order.customerName || '').toLowerCase().includes(orderSearch) ||
                (order.customerPhoneNo || '').toLowerCase().includes(orderSearch);
            return typeMatch && statusMatch && searchMatch;
        });

        const search = (this.menuSearch || '').toLowerCase().trim();
        this.filteredMenuItemsList = this.menuItems.filter((item) => {
            const categoryMatch = !this.selectedCategoryId || item.categoryId === this.selectedCategoryId;
            const routeMatch =
                !this.selectedRouteFilter ||
                (this.selectedRouteFilter === 'kitchen' ? this.isKitchenItem(item) : !this.isKitchenItem(item));
            const tagText = (item.tags || []).map((tag) => tag.name).join(' ');
            const searchMatch =
                !search ||
                (item.displayName || '').toLowerCase().includes(search) ||
                (item.categoryName || '').toLowerCase().includes(search) ||
                (item.shortCode || '').toLowerCase().includes(search) ||
                tagText.toLowerCase().includes(search);
            return item.isActive !== false && categoryMatch && routeMatch && searchMatch;
        });

        this.draftLineCountValue = this.cart.filter((line) => this.canEditLine(line)).length;
        this.cartItemCountValue = this.cart.reduce((sum, line) => sum + Number(line.qty || 0), 0);
        this.cartTotalAmount = this.cart.reduce((sum, line) => sum + this.lineTotal(line), 0);
        this.kitchenItemCountValue = this.menuItems.filter((item) => this.isKitchenItem(item)).length;
        this.stockItemCountValue = this.menuItems.filter((item) => !this.isKitchenItem(item)).length;
        this.kitchenLineCountValue = this.cart.filter((line) => this.isKitchenLine(line)).length;
        this.stockLineCountValue = this.cart.filter((line) => !this.isKitchenLine(line)).length;
        this.draftKitchenLineCountValue = this.cart.filter(
            (line) => this.canEditLine(line) && this.isKitchenLine(line),
        ).length;
        this.draftStockLineCountValue = this.cart.filter(
            (line) => this.canEditLine(line) && !this.isKitchenLine(line),
        ).length;
        this.activeTicketCountValue = this.tickets.filter(
            (ticket) =>
                ticket.status !== RestaurantTicketStatus.Served && ticket.status !== RestaurantTicketStatus.Cancelled,
        ).length;
        this.currentOrderTitleText = this.buildCurrentOrderTitle();
        this.currentOrderSubtitleText = this.buildCurrentOrderSubtitle();
        this.operationTablesList = this.tables.filter(
            (table) => table.isActive !== false && table.id !== this.selectedTable?.id,
        );
        this.mergeCandidatesList = this.openOrders.filter((order) => order.id !== this.selectedOrder?.id);
        this.splittableLinesList = this.cart.filter((line) => line.id && line.status !== 5);
        this.unbilledLinesList = this.cart.filter(
            (line) => line.id && line.status !== 5 && this.getLineUnbilledQty(line) > 0,
        );
        this.clampSplitBillQuantities();
        const selectedBillLines = this.selectedBillLines();
        this.selectedBillLineCountValue = selectedBillLines.length;
        this.selectedBillTotalAmount = this.unbilledLinesList.reduce(
            (sum, line) => sum + this.lineAmountForQty(line, this.splitBillQuantities[line.id] || 0),
            0,
        );
        this.recalculateTenderState();
        this.recalculatePendingConfigurator();
        this.persistLocalDraft();
        this.cdr.markForCheck();
    }

    private recalculatePendingConfigurator(): void {
        if (!this.pendingMenuItem) {
            this.pendingVariants = [];
            this.pendingModifierGroups = [];
            this.selectedVariantItem = null;
            this.configuratorTotalAmount = 0;
            return;
        }

        this.pendingVariants = this.getActiveVariants(this.pendingMenuItem);
        this.pendingModifierGroups = this.getActiveModifierGroups(this.pendingMenuItem).map((group) => ({
            ...group,
            modifiers: this.getActiveModifiers(group),
        }));
        this.selectedVariantItem =
            this.pendingVariants.find((variant) => variant.id === this.pendingVariantId) ||
            this.pendingVariants.find((variant) => variant.isDefault) ||
            this.pendingVariants[0] ||
            null;

        if (this.selectedVariantItem && this.pendingVariantId !== this.selectedVariantItem.id) {
            this.pendingVariantId = this.selectedVariantItem.id;
        }

        this.configuratorTotalAmount =
            this.variantPrice(this.pendingMenuItem, this.selectedVariantItem) + this.selectedModifierUnitTotal();
    }

    private applyDefaultLedgerSelection(): void {
        const cashLedgerId =
            this.findLedgerIdByName(this.ledgers, ['cash']) ||
            this.findLedgerIdByName(this.ledgers, ['cash in hand', 'cash-in hand'], ['cash']);
        const salesAccountId =
            this.findLedgerIdByName(this.salesLedgerList, ['sales account']) ||
            this.findLedgerIdByName(this.salesLedgerList, [], ['sales']) ||
            this.salesLedgerList[0]?.id ||
            '';

        if (!this.billForm.ledgerId && cashLedgerId) {
            this.billForm.ledgerId = cashLedgerId;
        }

        if (
            !this.billForm.paymentMethodLedgerId &&
            Number(this.billForm.paymentMethod) === PaymentMethod.Cash &&
            cashLedgerId
        ) {
            this.billForm.paymentMethodLedgerId = cashLedgerId;
        }

        if (!this.billForm.salesAccountId && salesAccountId) {
            this.billForm.salesAccountId = salesAccountId;
        }

        this.cdr.markForCheck();
    }

    private findLedgerIdByName(
        ledgers: SalesMasterAccountLedgerTableDto[],
        exactNames: string[],
        containsNames: string[] = [],
    ): string {
        const exactMatches = exactNames.map((name) => this.normalizeLedgerName(name));
        const containsMatches = containsNames.map((name) => this.normalizeLedgerName(name));
        const exact = ledgers.find((ledger) => exactMatches.includes(this.normalizeLedgerName(ledger.displayName)));
        if (exact?.id) {
            return exact.id;
        }

        return (
            ledgers.find((ledger) =>
                containsMatches.some((name) => this.normalizeLedgerName(ledger.displayName).includes(name)),
            )?.id || ''
        );
    }

    private normalizeLedgerName(value: string | undefined): string {
        return (value || '').toLowerCase().replace(/[-_]+/g, ' ').replace(/\s+/g, ' ').trim();
    }

    private buildCurrentOrderTitle(): string {
        if (this.selectedOrder?.orderNo) {
            return this.selectedOrder.orderNo;
        }

        if (this.selectedTable?.name) {
            return this.selectedTable.name;
        }

        return this.orderTypeText(this.orderType);
    }

    private buildCurrentOrderSubtitle(): string {
        const mode = this.selectedTable
            ? this.orderTypeText(RestaurantOrderType.DineIn)
            : this.orderTypeText(this.orderType);
        const customer = this.billForm.customerName ? ` / ${this.billForm.customerName}` : '';
        return `${mode}${customer}`;
    }

    variantPrice(item: RestaurantMenuItemDto, variant: RestaurantMenuVariantDto | null): number {
        if (!variant) {
            return Number(item?.price || 0);
        }

        return variant.isAbsolutePrice
            ? Number(variant.priceDelta || 0)
            : Number(item?.price || 0) + Number(variant.priceDelta || 0);
    }

    private pushConfiguredLine(
        item: RestaurantMenuItemDto,
        variant: RestaurantMenuVariantDto | null,
        modifiers: RestaurantModifierDto[],
    ): void {
        const modifierUnitTotal = modifiers.reduce((sum, modifier) => sum + Number(modifier.priceDelta || 0), 0);
        const modifierSummary = modifiers
            .map((modifier) => modifier.name)
            .filter(Boolean)
            .join(', ');
        const key = this.cartKey(
            item.id,
            variant?.id || '',
            modifiers.map((modifier) => modifier.id),
        );
        const existing = this.cart.find((line) => !line.id && line.configKey === key);
        if (existing) {
            existing.qty += 1;
            this.orderDirty = true;
            this.recalculateViewState();
            return;
        }

        this.cart.push({
            configKey: key,
            menuItemId: item.id,
            productId: item.productId,
            productName: item.displayName,
            itemNameSnapshot: item.displayName,
            variantId: variant?.id || undefined,
            variantNameSnapshot: variant?.name || '',
            stationId: item.stationId || null,
            qty: 1,
            rate: this.variantPrice(item, variant),
            modifierUnitTotal,
            modifierTotal: modifierUnitTotal,
            modifierSummary,
            modifiers: modifiers.map((modifier) => ({
                modifierId: modifier.id,
                name: modifier.name,
                priceDelta: Number(modifier.priceDelta || 0),
            })),
            discountAmount: 0,
            notes: '',
        });
        this.orderDirty = true;
        this.recalculateViewState();
    }

    private selectedModifiers(): RestaurantModifierDto[] {
        if (!this.pendingMenuItem) {
            return [];
        }

        const selected: RestaurantModifierDto[] = [];
        for (const group of this.pendingModifierGroups) {
            const ids = this.pendingModifierIds[group.id] || [];
            selected.push(...this.getActiveModifiers(group).filter((modifier) => ids.includes(modifier.id)));
        }
        return selected;
    }

    private selectedModifierUnitTotal(): number {
        return this.selectedModifiers().reduce((sum, modifier) => sum + Number(modifier.priceDelta || 0), 0);
    }

    private hydrateCartLines(items: RestaurantOrderItemDto[]): PosCartLine[] {
        return items
            .filter((x) => x.status !== 5)
            .map((line) => ({
                ...line,
                productName: line.itemNameSnapshot || line.productName,
                modifierUnitTotal: this.getExistingModifierUnitTotal(line),
                modifierSummary:
                    line.modifierSummary ||
                    this.getLineModifiers(line)
                        .map((modifier) => modifier.modifierNameSnapshot)
                        .join(', '),
            }));
    }

    private getExistingModifierUnitTotal(line: PosCartLine): number {
        const modifiers = this.getLineModifiers(line);
        if (modifiers.length) {
            return modifiers.reduce((sum, modifier) => sum + Number(modifier.priceDelta || 0), 0);
        }

        return line.qty ? Number(line.modifierTotal || 0) / Number(line.qty || 1) : Number(line.modifierTotal || 0);
    }

    private getLineModifiers(line: PosCartLine): PosLineModifier[] {
        return line.modifiers || [];
    }

    private buildModifierPayload(line: PosCartLine): CreateOrEditRestaurantOrderItemModifierDto[] {
        return this.getLineModifiers(line)
            .map((modifier) => modifier.modifierId || modifier.id)
            .filter(Boolean)
            .map(
                (modifierId) =>
                    new CreateOrEditRestaurantOrderItemModifierDto({
                        id: undefined,
                        modifierId,
                        qty: 1,
                    }),
            );
    }

    private cartKey(menuItemId: string, variantId: string, modifierIds: string[]): string {
        return [menuItemId, variantId, [...modifierIds].sort().join('|')].join(':');
    }

    private resetOperationForm(recalculate = true): void {
        this.activeOperation = '';
        this.operationForm = {
            discountAmount: this.selectedOrder?.discountAmount || 0,
            transferTableId: '',
            splitTableId: '',
            splitItemIds: [],
            mergeSourceOrderIds: [],
        };
        if (recalculate) {
            this.recalculateViewState();
        }
    }

    private resetBillSelection(): void {
        this.billMode = 'full';
        this.splitBillQuantities = {};
        this.selectedBillTotalAmount = 0;
        this.selectedBillLineCountValue = 0;
    }

    private resetTenderFields(): void {
        this.billForm.tipAmount = 0;
        this.billForm.customerPaidAmount = null;
        this.billPayableAmount = 0;
        this.billReturnAmount = 0;
    }

    private clampSplitBillQuantities(): void {
        const nextQuantities: Record<string, number> = {};
        for (const line of this.unbilledLinesList) {
            const qty = Math.min(Number(this.splitBillQuantities[line.id] || 0), this.getLineUnbilledQty(line));
            if (qty > 0) {
                nextQuantities[line.id] = qty;
            }
        }
        this.splitBillQuantities = nextQuantities;
    }

    private afterOrderChange(orderId?: string, afterRefresh?: () => void): void {
        this.restaurantOrderService.getOpenOrdersForPos().subscribe((orders) => {
            this.openOrders = orders || [];
            this.selectedOrder = orderId ? this.openOrders.find((order) => order.id === orderId) || null : null;
            if (this.selectedOrder) {
                this.selectedTable = this.tables.find((table) => table.id === this.selectedOrder.tableId) || null;
                this.orderType = this.selectedTable
                    ? RestaurantOrderType.DineIn
                    : (this.selectedOrder.orderType ?? RestaurantOrderType.TakeAway);
                this.cart = this.hydrateCartLines(this.selectedOrder.items || []);
                this.syncCustomerFromOrder();
                this.loadTickets(this.selectedOrder.id);
            } else {
                this.cart = [];
                this.tickets = [];
            }
            this.orderDirty = false;
            this.recalculateViewState();
            afterRefresh?.();
            this.restaurantSetupService.getTables(null).subscribe((tables) => {
                this.tables = tables || [];
                this.recalculateViewState();
            });
        });
    }

    private buildBillReceiptSnapshot(): PosBillReceiptSnapshot {
        const lines =
            this.billMode === 'split'
                ? this.unbilledLinesList
                      .map((line) => ({
                          line,
                          qty: Math.min(Number(this.splitBillQuantities[line.id] || 0), this.getLineUnbilledQty(line)),
                      }))
                      .filter((entry) => entry.qty > 0)
                      .map((entry) => this.mapReceiptLine(entry.line, entry.qty))
                : (this.unbilledLinesList.length ? this.unbilledLinesList : this.cart).map((line) =>
                      this.mapReceiptLine(line, this.getLineUnbilledQty(line) || Number(line.qty || 0)),
                  );

        return {
            orderNo: this.selectedOrder?.orderNo || '',
            orderType: this.selectedTable ? this.orderTypeText(RestaurantOrderType.DineIn) : this.orderTypeText(this.orderType),
            tableName: this.selectedTable?.name || this.selectedOrder?.tableName || '',
            customerName: this.billForm.customerName || this.selectedOrder?.customerName || 'Walk-in customer',
            customerPhoneNo: this.billForm.customerPhoneNo || this.selectedOrder?.customerPhoneNo || '',
            paymentMethod: this.paymentMethodText(this.billForm.paymentMethod),
            printedAt: DateTime.now(),
            lines,
        };
    }

    private mapReceiptLine(line: PosCartLine, qty: number): PosBillReceiptLine {
        const billedQty = Math.max(0, Number(qty || 0));
        const amount = this.lineAmountForQty(line, billedQty) || this.lineTotal(line);
        const unitRate = billedQty > 0 ? amount / billedQty : Number(line.rate || 0);
        const details = [this.lineVariantText(line), this.lineModifierText(line)].filter(Boolean).join(' / ');

        return {
            name: this.lineName(line),
            details,
            qty: billedQty,
            rate: unitRate,
            amount,
        };
    }

    private printOrDownloadPosBill(
        receipt: PosBillReceiptSnapshot,
        result: FinalizeRestaurantBillResultDto,
    ): void {
        const html = this.buildPosBillHtml(receipt, result);
        const popup = window.open('', '_blank', 'width=380,height=720');
        if (!popup) {
            this.downloadPosBillHtml(html, result.orderNo || receipt.orderNo || result.salesMasterId);
            this.notify.warn('Print popup blocked. POS bill downloaded instead.');
            return;
        }

        popup.document.write(html);
        popup.document.close();
    }

    private buildPosBillHtml(receipt: PosBillReceiptSnapshot, result: FinalizeRestaurantBillResultDto): string {
        const restaurantName = this.appSession.tenant?.name || this.appSession.tenancyName || 'Restaurant';
        const billNo = result.orderNo || receipt.orderNo || '-';
        const itemCount = receipt.lines.reduce((total, line) => total + Number(line.qty || 0), 0);
        const rows = receipt.lines
            .map(
                (line) => `
                    <tr>
                        <td class="item-cell">
                            <span class="item-name">${this.escapeHtml(line.name)}</span>
                            ${line.details ? `<span class="item-details">${this.escapeHtml(line.details)}</span>` : ''}
                        </td>
                        <td class="qty-cell">${this.formatQty(line.qty)}</td>
                        <td class="money-cell">${this.formatMoney(line.rate)}</td>
                        <td class="money-cell amount-cell">${this.formatMoney(line.amount)}</td>
                    </tr>`,
            )
            .join('');

        return `
            <html>
                <head>
                    <title>POS Bill ${this.escapeHtml(billNo)}</title>
                    <style>
                        * { box-sizing: border-box; }
                        html, body { margin: 0; padding: 0; }
                        body {
                            background: #f4f4f4;
                            color: #111;
                            font-family: Arial, Helvetica, sans-serif;
                            font-size: 11px;
                            line-height: 1.25;
                        }
                        .receipt {
                            width: 80mm;
                            max-width: 302px;
                            min-height: 100%;
                            margin: 0 auto;
                            padding: 12px 10px 14px;
                            background: #fff;
                        }
                        .header { text-align: center; }
                        .brand {
                            display: block;
                            font-size: 20px;
                            font-weight: 800;
                            line-height: 1.05;
                            word-break: break-word;
                        }
                        .document-title {
                            display: inline-block;
                            margin-top: 6px;
                            padding: 3px 10px;
                            border: 1px solid #111;
                            font-size: 11px;
                            font-weight: 800;
                            text-transform: uppercase;
                        }
                        .bill-number {
                            margin-top: 6px;
                            font-family: "Courier New", monospace;
                            font-size: 12px;
                            font-weight: 700;
                            text-align: center;
                        }
                        .rule {
                            border: 0;
                            border-top: 1px dashed #777;
                            margin: 10px 0;
                        }
                        .meta {
                            display: grid;
                            gap: 4px;
                            margin-bottom: 10px;
                        }
                        .meta-row,
                        .total-row {
                            display: grid;
                            grid-template-columns: minmax(72px, 34%) minmax(0, 1fr);
                            column-gap: 8px;
                            align-items: baseline;
                        }
                        .label {
                            color: #555;
                            font-size: 9px;
                            font-weight: 700;
                            text-transform: uppercase;
                        }
                        .value {
                            min-width: 0;
                            font-weight: 700;
                            text-align: right;
                            word-break: break-word;
                        }
                        table {
                            width: 100%;
                            border-collapse: collapse;
                            table-layout: fixed;
                            font-size: 11px;
                        }
                        th {
                            padding: 4px 0 5px;
                            border-bottom: 1px solid #111;
                            font-size: 9px;
                            text-align: right;
                            text-transform: uppercase;
                        }
                        th:first-child { text-align: left; }
                        td {
                            padding: 6px 0;
                            border-bottom: 1px dotted #ccc;
                            vertical-align: top;
                        }
                        .item-cell {
                            padding-right: 6px;
                            text-align: left;
                            word-break: break-word;
                        }
                        .item-name {
                            display: block;
                            font-size: 11px;
                            font-weight: 800;
                        }
                        .item-details {
                            display: block;
                            margin-top: 2px;
                            color: #555;
                            font-size: 9px;
                        }
                        .qty-cell,
                        .money-cell {
                            font-family: "Courier New", monospace;
                            font-size: 11px;
                            text-align: right;
                            white-space: nowrap;
                        }
                        .amount-cell { font-weight: 700; }
                        .totals {
                            margin-top: 10px;
                            padding-top: 7px;
                            border-top: 1px dashed #777;
                            font-size: 11px;
                        }
                        .total-row { margin-top: 4px; }
                        .total-row strong {
                            font-family: "Courier New", monospace;
                            font-size: 12px;
                        }
                        .payable {
                            margin: 7px 0 5px;
                            padding: 5px 6px;
                            border: 2px solid #111;
                        }
                        .payable .label,
                        .payable strong {
                            color: #111;
                            font-size: 15px;
                            font-weight: 900;
                        }
                        .footer {
                            margin-top: 12px;
                            text-align: center;
                        }
                        .thanks {
                            margin: 0;
                            font-size: 11px;
                            font-weight: 700;
                        }
                        .footer-line {
                            margin-top: 6px;
                            color: #555;
                            font-size: 9px;
                        }
                        @media print {
                            @page { size: 80mm auto; margin: 0; }
                            body { width: 80mm; background: #fff; }
                            .receipt { width: 80mm; max-width: none; margin: 0; padding: 4mm 3mm; }
                        }
                    </style>
                </head>
                <body>
                    <main class="receipt">
                        <header class="header">
                            <span class="brand">${this.escapeHtml(restaurantName)}</span>
                            <span class="document-title">POS Bill</span>
                            <div class="bill-number">${this.escapeHtml(billNo)}</div>
                        </header>
                        <hr class="rule">
                        <section class="meta">
                            <div class="meta-row"><span class="label">Mode</span><span class="value">${this.escapeHtml(receipt.orderType || '-')}</span></div>
                            <div class="meta-row"><span class="label">Table</span><span class="value">${this.escapeHtml(receipt.tableName || '-')}</span></div>
                            <div class="meta-row"><span class="label">Customer</span><span class="value">${this.escapeHtml(receipt.customerName || '-')}</span></div>
                        ${
                            receipt.customerPhoneNo
                                ? `<div class="meta-row"><span class="label">Phone</span><span class="value">${this.escapeHtml(receipt.customerPhoneNo)}</span></div>`
                                : ''
                        }
                            <div class="meta-row"><span class="label">Payment</span><span class="value">${this.escapeHtml(receipt.paymentMethod)}</span></div>
                            <div class="meta-row"><span class="label">Date</span><span class="value">${receipt.printedAt.toFormat('yyyy-LL-dd HH:mm')}</span></div>
                            <div class="meta-row"><span class="label">Items</span><span class="value">${this.formatQty(itemCount)}</span></div>
                        </section>
                        <table>
                            <colgroup>
                                <col style="width: 46%">
                                <col style="width: 12%">
                                <col style="width: 20%">
                                <col style="width: 22%">
                            </colgroup>
                            <thead>
                                <tr>
                                    <th>Item</th>
                                    <th>Qty</th>
                                    <th>Rate</th>
                                    <th>Amt</th>
                                </tr>
                            </thead>
                            <tbody>${rows}</tbody>
                        </table>
                        <section class="totals">
                            <div class="total-row"><span class="label">Bill</span><strong class="value">${this.formatMoney(result.billAmount)}</strong></div>
                            <div class="total-row"><span class="label">Tip</span><strong class="value">${this.formatMoney(result.tipAmount)}</strong></div>
                            <div class="total-row payable"><span class="label">Payable</span><strong class="value">${this.formatMoney(result.payableAmount)}</strong></div>
                            <div class="total-row"><span class="label">Paid</span><strong class="value">${this.formatMoney(result.customerPaidAmount)}</strong></div>
                            <div class="total-row"><span class="label">Return</span><strong class="value">${this.formatMoney(result.returnAmount)}</strong></div>
                        </section>
                        <footer class="footer">
                            <p class="thanks">Thank you. Please visit again.</p>
                            <div class="footer-line">Keep this bill for your records</div>
                        </footer>
                    </main>
                    <script>window.onload = function () { window.focus(); window.print(); };</script>
                </body>
            </html>`;
    }

    private downloadPosBillHtml(html: string, name: string): void {
        const blob = new Blob([html], { type: 'text/html;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `POS-Bill-${this.safeFileName(name || DateTime.now().toFormat('yyyyLLddHHmmss'))}.html`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    private safeFileName(value: string): string {
        return String(value).replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'receipt';
    }

    private formatMoney(value: number | string | undefined | null): string {
        return Number(value || 0).toFixed(2);
    }

    private formatQty(value: number | string | undefined | null): string {
        const qty = Number(value || 0);
        return Number.isInteger(qty) ? qty.toString() : qty.toFixed(2);
    }

    private padDatePart(value: number | string | undefined | null): string {
        return String(Number(value || 0)).padStart(2, '0');
    }

    private openTicketPrint(ticket: RestaurantTicketDto): boolean {
        const popup = window.open('', '_blank', 'width=420,height=640');
        if (!popup) {
            this.notify.warn('Allow popups to print KOT/BOT');
            return false;
        }

        const rows = (ticket.items || [])
            .map((item) => {
                const name = item.itemNameSnapshot || item.productName || '';
                const variant = item.variantNameSnapshot
                    ? `<div class="muted">${this.escapeHtml(item.variantNameSnapshot)}</div>`
                    : '';
                const modifiers = item.modifierSummary
                    ? `<div class="muted">${this.escapeHtml(item.modifierSummary)}</div>`
                    : '';
                const notes =
                    item.notes || item.cancelReason
                        ? `<div class="muted">${this.escapeHtml(item.notes || item.cancelReason || '')}</div>`
                        : '';
                return `
                    <tr>
                        <td>
                            <strong>${this.escapeHtml(name)}</strong>
                            ${variant}
                            ${modifiers}
                            ${notes}
                        </td>
                        <td>${item.qty}</td>
                        <td>${this.escapeHtml(item.unitName || '')}</td>
                    </tr>`;
            })
            .join('');
        const ticketType = ticket.ticketType === 1 ? 'BOT' : 'KOT';
        const purpose = this.ticketPurposeText(ticket.purpose).toUpperCase();
        const reprint = ticket.isReprint ? '<div class="stamp">REPRINT</div>' : '';

        popup.document.write(`
            <html>
                <head>
                    <title>${ticket.ticketNo}</title>
                    <style>
                        body { font-family: Arial, sans-serif; width: 280px; margin: 0 auto; padding: 12px; color: #111; }
                        h1, h2, p { margin: 0; text-align: center; }
                        h1 { font-size: 18px; }
                        h2 { font-size: 16px; margin-top: 6px; }
                        .meta { margin: 12px 0; font-size: 12px; }
                        .meta div { display: flex; justify-content: space-between; gap: 10px; }
                        table { width: 100%; border-collapse: collapse; font-size: 12px; }
                        td { border-top: 1px dashed #999; padding: 6px 0; vertical-align: top; }
                        td:nth-child(2), td:nth-child(3) { text-align: right; white-space: nowrap; }
                        .muted { color: #555; font-size: 11px; margin-top: 2px; }
                        .stamp { border: 1px solid #111; display: inline-block; padding: 2px 8px; margin-top: 6px; font-weight: bold; }
                        @media print { body { width: auto; } }
                    </style>
                </head>
                <body>
                    <h1>${this.escapeHtml(ticket.restaurantName || 'Restaurant')}</h1>
                    <h2>${ticketType} - ${purpose}</h2>
                    ${reprint}
                    <div class="meta">
                        <div><span>No</span><strong>${this.escapeHtml(ticket.ticketNo || '')}</strong></div>
                        <div><span>Order</span><strong>${this.escapeHtml(ticket.orderNo || '')}</strong></div>
                        <div><span>Table</span><strong>${this.escapeHtml(ticket.tableName || '-')}</strong></div>
                        <div><span>Station</span><strong>${this.escapeHtml(ticket.stationName || '')}</strong></div>
                        <div><span>Waiter</span><strong>${this.escapeHtml(ticket.waiterName || '-')}</strong></div>
                        <div><span>Sent</span><strong>${this.escapeHtml(this.formatNepaliDateTime(ticket.sentAt))}</strong></div>
                    </div>
                    <table>${rows}</table>
                    <script>window.onload = function () { window.print(); };</script>
                </body>
            </html>`);
        popup.document.close();
        return true;
    }

    private escapeHtml(value: string): string {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
}
