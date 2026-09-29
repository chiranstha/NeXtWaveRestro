import {
    AfterViewInit,
    ChangeDetectorRef,
    Component,
    Injector,
    OnDestroy,
    OnInit,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    RestaurantKdsServiceProxy,
    RestaurantSetupServiceProxy,
    RestaurantStationDto,
    RestaurantTicketDto,
    RestaurantTicketItemDto,
    BulkUpdateRestaurantTicketItemStatusDto,
    UpdateRestaurantTicketItemStatusDto,
    UpdateRestaurantTicketStatusDto,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';

interface RestaurantKdsItemEntry {
    ticket: RestaurantTicketDto;
    item: RestaurantTicketItemDto;
}

interface RestaurantKdsItemTotal {
    key: string;
    name: string;
    unitName: string;
    variantName: string;
    modifierSummary: string;
    notes: string;
    qty: number;
    sentCount: number;
    preparingCount: number;
    readyCount: number;
    sentQty: number;
    preparingQty: number;
    readyQty: number;
    entries: RestaurantKdsItemEntry[];
}

@Component({
    selector: 'restaurant-kds',
    templateUrl: './restaurant-kds.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantKdsComponent extends AppComponentBase implements OnInit, AfterViewInit, OnDestroy {
    stations: RestaurantStationDto[] = [];
    tickets: RestaurantTicketDto[] = [];
    filteredTickets: RestaurantTicketDto[] = [];
    itemTotals: RestaurantKdsItemTotal[] = [];
    expandedItemKeys = new Set<string>();
    activeView: 'tickets' | 'items' = 'tickets';
    selectedStationId = '';
    selectedTicketType = '';
    selectedStatus = '';
    loading = false;
    refreshFailed = false;
    online = navigator.onLine;
    lastSuccessfulRefresh: Date | null = null;
    bulkActionKey = '';
    private refreshTimer: number | undefined;
    private readonly handleOnline = (): void => {
        this.online = true;
        this.refresh();
    };
    private readonly handleOffline = (): void => {
        this.online = false;
        this.cdr.markForCheck();
    };
    private readonly handleVisibility = (): void => {
        if (document.visibilityState === 'visible') this.refresh();
    };

    private restaurantSetupService = inject(RestaurantSetupServiceProxy);
    private restaurantKdsService = inject(RestaurantKdsServiceProxy);
    private cdr = inject(ChangeDetectorRef);

    constructor() {
        super(inject(Injector));
    }

    ngOnInit(): void {
        this.restaurantSetupService.getStations().subscribe((result) => {
            this.stations = result || [];
            this.cdr.markForCheck();
        });
        this.refresh();
        this.refreshTimer = window.setInterval(() => this.refresh(), 15000);
        window.addEventListener('online', this.handleOnline);
        window.addEventListener('offline', this.handleOffline);
        document.addEventListener('visibilitychange', this.handleVisibility);
    }

    ngAfterViewInit(): void {
        this.cdr.detectChanges();
    }

    ngOnDestroy(): void {
        if (this.refreshTimer) {
            window.clearInterval(this.refreshTimer);
        }
        window.removeEventListener('online', this.handleOnline);
        window.removeEventListener('offline', this.handleOffline);
        document.removeEventListener('visibilitychange', this.handleVisibility);
    }

    refresh(): void {
        if (this.loading || !this.online || document.visibilityState === 'hidden') return;
        this.loading = true;
        this.refreshFailed = false;
        this.cdr.markForCheck();
        this.restaurantKdsService
            .getOpenTickets(this.selectedStationId || undefined)
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((result) => {
                this.lastSuccessfulRefresh = new Date();
                this.tickets = result || [];
                this.updateFilteredTickets();
            }, () => {
                this.refreshFailed = true;
                this.cdr.markForCheck();
            });
    }

    updateFilteredTickets(): void {
        this.filteredTickets = this.tickets.filter((ticket) => {
            const typeMatch = this.selectedTicketType === '' || ticket.ticketType === Number(this.selectedTicketType);
            const statusMatch = this.selectedStatus === '' || ticket.status === Number(this.selectedStatus);
            return typeMatch && statusMatch;
        });
        this.itemTotals = this.buildItemTotals(this.filteredTickets);
        this.cdr.markForCheck();
    }

    setActiveView(view: 'tickets' | 'items'): void {
        this.activeView = view;
    }

    toggleItemExpanded(key: string): void {
        if (this.expandedItemKeys.has(key)) {
            this.expandedItemKeys.delete(key);
        } else {
            this.expandedItemKeys.add(key);
        }
    }

    isItemExpanded(key: string): boolean {
        return this.expandedItemKeys.has(key);
    }

    trackItemTotal(_: number, total: RestaurantKdsItemTotal): string {
        return total.key;
    }

    trackItemEntry(_: number, entry: RestaurantKdsItemEntry): string {
        return entry.item.id;
    }

    eligibleItemEntries(total: RestaurantKdsItemTotal, status: number): RestaurantKdsItemEntry[] {
        const allowedStatuses = status === 2 ? [1] : status === 3 ? [1, 2] : status === 4 ? [3] : [];
        return total.entries.filter((entry) => allowedStatuses.includes(entry.item.status));
    }

    bulkUpdateItemStatus(total: RestaurantKdsItemTotal, status: number): void {
        const entries = this.eligibleItemEntries(total, status);
        if (!entries.length || this.bulkActionKey) {
            return;
        }

        this.bulkActionKey = `${total.key}:${status}`;
        this.restaurantKdsService
            .updateTicketItemStatuses(
                new BulkUpdateRestaurantTicketItemStatusDto({
                    ticketItemIds: entries.map((entry) => entry.item.id),
                    status,
                }),
            )
            .pipe(finalize(() => {
                this.bulkActionKey = '';
                this.cdr.markForCheck();
            }))
            .subscribe((result) => {
                const skippedText = result.skippedCount ? ` (${result.skippedCount} already changed)` : '';
                this.notify.success(`${this.itemStatusText(status)}: ${result.updatedCount} line(s) updated${skippedText}`);
                this.refresh();
            }, () => this.notify.error('Items could not be updated. Refresh the KDS and retry.'));
    }

    private buildItemTotals(tickets: RestaurantTicketDto[]): RestaurantKdsItemTotal[] {
        const totals = new Map<string, RestaurantKdsItemTotal>();

        for (const ticket of tickets) {
            if (ticket.purpose === 1) {
                continue;
            }

            for (const item of ticket.items || []) {
                if (item.status !== 1 && item.status !== 2 && item.status !== 3) {
                    continue;
                }

                const name = item.itemNameSnapshot || item.productName || 'Unnamed item';
                const unitName = item.unitName || '';
                const variantName = item.variantNameSnapshot || '';
                const modifierSummary = item.modifierSummary || '';
                const notes = item.notes || '';
                const key = JSON.stringify(
                    [name, unitName, variantName, modifierSummary, notes].map((value) => value.trim().toLocaleLowerCase()),
                );
                let total = totals.get(key);

                if (!total) {
                    total = {
                        key,
                        name,
                        unitName,
                        variantName,
                        modifierSummary,
                        notes,
                        qty: 0,
                        sentCount: 0,
                        preparingCount: 0,
                        readyCount: 0,
                        sentQty: 0,
                        preparingQty: 0,
                        readyQty: 0,
                        entries: [],
                    };
                    totals.set(key, total);
                }

                total.qty += item.qty || 0;
                if (item.status === 1) {
                    total.sentCount++;
                    total.sentQty += item.qty || 0;
                } else if (item.status === 2) {
                    total.preparingCount++;
                    total.preparingQty += item.qty || 0;
                } else {
                    total.readyCount++;
                    total.readyQty += item.qty || 0;
                }
                total.entries.push({ ticket, item });
            }
        }

        return Array.from(totals.values()).sort((left, right) => right.qty - left.qty || left.name.localeCompare(right.name));
    }

    selectedStationName(): string {
        if (!this.selectedStationId) {
            return 'All stations';
        }

        const station = this.stations.find((item) => String(item.id) === String(this.selectedStationId));
        return station?.name ?? 'Selected station';
    }

    pendingTicketCount(): number {
        return this.filteredTickets.filter((ticket) => ticket.status === 0).length;
    }

    inProgressTicketCount(): number {
        return this.filteredTickets.filter((ticket) => ticket.status === 1).length;
    }

    readyTicketCount(): number {
        return this.filteredTickets.filter((ticket) => ticket.status === 2).length;
    }

    rushTicketCount(): number {
        return this.filteredTickets.filter((ticket) => ticket.status < 2 && this.elapsedMinutes(ticket) >= 20).length;
    }

    ticketUrgencyClass(ticket: RestaurantTicketDto): string {
        if (ticket.status >= 2) {
            return 'restaurant-kds-card-ready';
        }

        const minutes = this.elapsedMinutes(ticket);
        if (minutes >= 20) {
            return 'restaurant-kds-card-rush';
        }

        if (minutes >= 10) {
            return 'restaurant-kds-card-warn';
        }

        return 'restaurant-kds-card-fresh';
    }

    trackTicket(_: number, ticket: RestaurantTicketDto): string {
        return ticket.id;
    }

    trackTicketItem(_: number, item: RestaurantTicketItemDto): string {
        return item.id;
    }

    setTicketStatus(ticket: RestaurantTicketDto, status: number): void {
        const cancelReason = status === 4 && ticket.purpose !== 1 ? window.prompt('Cancel reason') : '';
        if (status === 4 && ticket.purpose !== 1 && !cancelReason) {
            return;
        }

        this.restaurantKdsService
            .updateTicketStatus(
                new UpdateRestaurantTicketStatusDto({ ticketId: ticket.id, status, cancelReason: cancelReason || '' }),
            )
            .subscribe(() => {
                this.refresh();
                this.cdr.markForCheck();
            });
    }

    setItemStatus(item: RestaurantTicketItemDto, status: number): void {
        const cancelReason = status === 5 ? window.prompt('Cancel reason') : '';
        if (status === 5 && !cancelReason) {
            return;
        }

        this.restaurantKdsService
            .updateTicketItemStatus(
                new UpdateRestaurantTicketItemStatusDto({
                    ticketItemId: item.id,
                    status,
                    cancelReason: cancelReason || '',
                }),
            )
            .subscribe(() => {
                this.refresh();
                this.cdr.markForCheck();
            });
    }

    ticketTypeText(ticket: RestaurantTicketDto): string {
        return ticket.ticketType === 1 ? 'BOT' : 'KOT';
    }

    ticketRouteText(ticket: RestaurantTicketDto): string {
        return ticket.ticketType === 1 ? 'Stock Item' : 'Kitchen Item';
    }

    ticketRouteClass(ticket: RestaurantTicketDto): string {
        return ticket.ticketType === 1 ? 'bg-light-primary text-primary' : 'bg-light-warning text-warning';
    }

    purposeText(ticket: RestaurantTicketDto): string {
        return ticket.purpose === 1 ? 'Cancellation' : ticket.purpose === 2 ? 'AddOn' : 'New Order';
    }

    ticketStatusText(status: number): string {
        return ['Pending', 'In Progress', 'Ready', 'Served', 'Cancelled'][status] || 'Unknown';
    }

    itemStatusText(status: number): string {
        return ['Draft', 'Sent', 'Preparing', 'Ready', 'Served', 'Cancelled'][status] || 'Unknown';
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

    itemStatusClass(status: number): string {
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

    elapsed(ticket: RestaurantTicketDto): string {
        if (!ticket.sentAt) {
            return '';
        }

        const minutes = this.elapsedMinutes(ticket);
        if (minutes < 60) {
            return `${minutes}m`;
        }

        const hours = Math.floor(minutes / 60);
        return `${hours}h ${minutes % 60}m`;
    }

    private elapsedMinutes(ticket: RestaurantTicketDto): number {
        if (!ticket.sentAt) {
            return 0;
        }

        return Math.max(0, Math.floor((Date.now() - ticket.sentAt.toMillis()) / 60000));
    }

    private finishLoading(): void {
        this.loading = false;
        this.cdr.markForCheck();
    }
}
