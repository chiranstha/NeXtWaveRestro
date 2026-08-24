import {
    PaymentMethod,
    RestaurantOrderType,
    RestaurantTableDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { RestaurantPosComponent } from './restaurant-pos.component';

describe('RestaurantPosComponent cashier workflow', () => {
    function createComponent(): RestaurantPosComponent {
        const component = Object.create(RestaurantPosComponent.prototype) as RestaurantPosComponent & Record<string, any>;
        Object.assign(component, {
            areas: [],
            categories: [],
            tables: [],
            menuItems: [],
            openOrders: [],
            ledgers: [],
            salesLedgerList: [],
            selectedTable: null,
            selectedOrder: null,
            cart: [],
            tickets: [],
            selectedAreaId: '',
            selectedCategoryId: '',
            selectedRouteFilter: '',
            menuSearch: '',
            tableSearch: '',
            tableStatusFilter: '',
            orderSearch: '',
            orderTypeFilter: '',
            orderStatusFilter: '',
            filteredTablesList: [],
            filteredMenuItemsList: [],
            filteredOrdersList: [],
            operationTablesList: [],
            mergeCandidatesList: [],
            splittableLinesList: [],
            unbilledLinesList: [],
            splitBillQuantities: {},
            pendingMenuItem: null,
            pendingVariants: [],
            pendingModifierGroups: [],
            selectedVariantItem: null,
            orderType: RestaurantOrderType.DineIn,
            billMode: 'full',
            activeContext: 'tables',
            activeOperation: '',
            orderDirty: false,
            pendingOrderSwitch: null,
            currentClock: DateTime.now(),
            billForm: {
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
            },
            cdr: { markForCheck: jasmine.createSpy('markForCheck') },
        });
        return component;
    }

    it('starts dine-in on Floor and takeaway or delivery on Menu', () => {
        const component = createComponent() as any;

        component.performStartNewOrder(RestaurantOrderType.DineIn);
        expect(component.activeContext).toBe('tables');

        component.performStartNewOrder(RestaurantOrderType.TakeAway);
        expect(component.activeContext).toBe('menu');

        component.performStartNewOrder(RestaurantOrderType.Delivery);
        expect(component.activeContext).toBe('menu');
    });

    it('protects an unsaved cart before selecting another order', () => {
        const component = createComponent();
        component.orderDirty = true;
        component.cart = [{ productId: 'product-1', qty: 1, rate: 100 }];
        component.selectedOrder = { id: 'order-1' } as any;

        component.selectOrder({ id: 'order-2' } as any);

        expect(component.pendingOrderSwitch).toEqual(jasmine.objectContaining({ kind: 'order' }));
        expect(component.selectedOrder.id).toBe('order-1');
    });

    it('filters large table and order lists locally', () => {
        const component = createComponent() as any;
        component.tables = [
            new RestaurantTableDto({
                id: 'table-1',
                name: 'Banquet 08',
                code: 'BAN-08',
                capacity: 8,
                sortOrder: 1,
                status: 1,
                isActive: true,
                areaId: 'banquet',
                areaName: 'Banquet Hall',
            }),
            new RestaurantTableDto({
                id: 'table-2',
                name: 'Patio 02',
                code: 'PAT-02',
                capacity: 4,
                sortOrder: 2,
                status: 0,
                isActive: true,
                areaId: 'patio',
                areaName: 'Patio',
            }),
        ];
        component.openOrders = [
            { id: 'order-1', orderNo: 'POS-1001', orderType: RestaurantOrderType.DineIn, status: 2, tableName: 'Banquet 08' },
            { id: 'order-2', orderNo: 'POS-1002', orderType: RestaurantOrderType.Delivery, status: 1, customerName: 'Aarav Shah' },
        ];
        component.tableSearch = 'ban';
        component.tableStatusFilter = '1';
        component.orderSearch = 'aarav';
        component.orderTypeFilter = RestaurantOrderType.Delivery.toString();

        component.recalculateViewState();

        expect(component.filteredTablesList.map((table: RestaurantTableDto) => table.id)).toEqual(['table-1']);
        expect(component.filteredOrdersList.map((order: any) => order.id)).toEqual(['order-2']);
    });

    it('requires accounting defaults before enabling payment', () => {
        const component = createComponent();
        component.selectedOrder = { id: 'order-1' } as any;
        component.billPayableAmount = 500;

        expect(component.canFinalizePayment).toBeFalse();
        expect(component.checkoutBlockReason).toContain('Advanced');

        component.billForm.ledgerId = 'cash-ledger';
        component.billForm.salesAccountId = 'sales-account';
        expect(component.canFinalizePayment).toBeTrue();
    });
});
