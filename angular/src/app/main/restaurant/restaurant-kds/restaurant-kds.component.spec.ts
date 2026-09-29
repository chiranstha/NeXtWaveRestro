import { of } from 'rxjs';
import {
    BulkUpdateRestaurantTicketItemStatusDto,
    BulkUpdateRestaurantTicketItemStatusResultDto,
    RestaurantTicketDto,
    RestaurantTicketItemDto,
} from '@shared/service-proxies/service-proxies';
import { RestaurantKdsComponent } from './restaurant-kds.component';

describe('RestaurantKdsComponent item totals', () => {
    function createComponent(): RestaurantKdsComponent {
        const component = Object.create(RestaurantKdsComponent.prototype) as RestaurantKdsComponent;
        Object.assign(component, {
            tickets: [],
            filteredTickets: [],
            itemTotals: [],
            expandedItemKeys: new Set<string>(),
            activeView: 'tickets',
            selectedStationId: '',
            selectedTicketType: '',
            selectedStatus: '',
            cdr: { markForCheck: jasmine.createSpy('markForCheck') },
            restaurantKdsService: {
                updateTicketItemStatuses: jasmine.createSpy('updateTicketItemStatuses').and.returnValue(
                    of(new BulkUpdateRestaurantTicketItemStatusResultDto({ updatedCount: 2, skippedCount: 1 })),
                ),
            },
            releaseApi: { kdsOperationStatus: jasmine.createSpy('kdsOperationStatus').and.returnValue(of({ status: 'NotFound' })) },
            notify: { success: jasmine.createSpy('success'), error: jasmine.createSpy('error') },
        });
        return component;
    }

    function ticket(
        id: string,
        items: Array<Partial<RestaurantTicketItemDto>>,
        options: { purpose?: number; status?: number; ticketType?: number } = {},
    ): RestaurantTicketDto {
        return {
            id,
            purpose: options.purpose ?? 0,
            status: options.status ?? 0,
            ticketType: options.ticketType ?? 0,
            ticketNo: `KOT-${id}`,
            orderNo: `ORD-${id}`,
            orderId: `order-${id}`,
            orderRowVersion: `version-${id}`,
            tableName: `Table ${id}`,
            items: items.map((item, index) => ({
                id: `${id}-item-${index}`,
                itemNameSnapshot: 'Masala Tea',
                productName: 'Masala Tea',
                unitName: 'Cup',
                qty: 1,
                status: 1,
                variantNameSnapshot: '',
                modifierSummary: '',
                notes: '',
                ...item,
            })),
        } as unknown as RestaurantTicketDto;
    }

    it('sums Sent, Preparing, and Ready quantities across tickets and excludes served or cancelled work', () => {
        const component = createComponent();
        component.tickets = [
            ticket('1', [{ qty: 3 }]),
            ticket('2', [{ qty: 4, status: 2 }]),
            ticket('3', [{ qty: 9, status: 3 }]),
            ticket('4', [{ qty: 7, status: 5 }]),
            ticket('5', [{ qty: 6 }], { purpose: 1 }),
        ];

        component.updateFilteredTickets();

        expect(component.itemTotals.length).toBe(1);
        expect(component.itemTotals[0].qty).toBe(16);
        expect(component.itemTotals[0].entries.map((entry) => entry.ticket.id)).toEqual(['1', '2', '3']);
        expect(component.itemTotals[0].sentCount).toBe(1);
        expect(component.itemTotals[0].preparingCount).toBe(1);
        expect(component.itemTotals[0].readyCount).toBe(1);
    });

    it('keeps units, variants, modifiers, and preparation notes in separate totals', () => {
        const component = createComponent();
        component.tickets = [ticket('1', [
            { qty: 5 },
            { qty: 2, variantNameSnapshot: 'Large' },
            { qty: 3, modifierSummary: 'Ginger' },
            { qty: 4, notes: 'Less sugar' },
            { qty: 6, unitName: 'Jug' },
        ])];

        component.updateFilteredTickets();

        expect(component.itemTotals.length).toBe(5);
        expect(component.itemTotals.map((total) => total.qty)).toEqual([6, 5, 4, 3, 2]);
    });

    it('sends only currently eligible item lines with order versions and a stable request ID', async () => {
        localStorage.clear();
        const component = createComponent();
        component.tickets = [
            ticket('sent', [{ id: 'sent-item', qty: 2, status: 1 }]),
            ticket('preparing', [{ id: 'preparing-item', qty: 1, status: 2 }]),
            ticket('ready', [{ id: 'ready-item', qty: 1, status: 3 }]),
        ];
        component.refresh = jasmine.createSpy('refresh');
        component.updateFilteredTickets();
        const total = component.itemTotals[0];

        expect(component.eligibleItemEntries(total, 2).map((entry) => entry.item.id)).toEqual(['sent-item']);
        expect(component.eligibleItemEntries(total, 3).map((entry) => entry.item.id)).toEqual(['sent-item', 'preparing-item']);
        expect(component.eligibleItemEntries(total, 4).map((entry) => entry.item.id)).toEqual(['ready-item']);

        await component.bulkUpdateItemStatus(total, 3);

        const service = (component as any).restaurantKdsService;
        const request = service.updateTicketItemStatuses.calls.mostRecent().args[0] as BulkUpdateRestaurantTicketItemStatusDto;
        expect(request.ticketItemIds).toEqual(['sent-item', 'preparing-item']);
        expect(request.status).toBe(3);
        expect(request.expectedOrderVersions).toEqual({ 'order-preparing': 'version-preparing', 'order-sent': 'version-sent' });
        expect(request.clientRequestId).toBeTruthy();
        expect(component.refresh).toHaveBeenCalled();
        expect((component as any).notify.success).toHaveBeenCalledWith('Ready: 2 line(s) updated (1 already changed)');
    });

    it('does not issue a bulk update when all matching lines are ineligible for that step', () => {
        const component = createComponent();
        component.tickets = [ticket('1', [{ id: 'ready-item', status: 3 }])];
        component.updateFilteredTickets();

        component.bulkUpdateItemStatus(component.itemTotals[0], 2);

        expect((component as any).restaurantKdsService.updateTicketItemStatuses).not.toHaveBeenCalled();
    });

    it('rebuilds totals from the selected ticket type and status filters', () => {
        const component = createComponent();
        component.tickets = [
            ticket('1', [{ qty: 3 }], { status: 0, ticketType: 0 }),
            ticket('2', [{ qty: 8 }], { status: 1, ticketType: 0 }),
            ticket('3', [{ qty: 10 }], { status: 0, ticketType: 1 }),
        ];
        component.selectedTicketType = '0';
        component.selectedStatus = '0';

        component.updateFilteredTickets();

        expect(component.filteredTickets.map((item) => item.id)).toEqual(['1']);
        expect(component.itemTotals.map((total) => total.qty)).toEqual([3]);
    });
});
