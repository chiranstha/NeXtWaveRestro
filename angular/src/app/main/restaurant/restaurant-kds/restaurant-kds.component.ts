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
    UpdateRestaurantTicketItemStatusDto,
    UpdateRestaurantTicketStatusDto,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';

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
    selectedStationId = '';
    selectedTicketType = '';
    selectedStatus = '';
    loading = false;
    private refreshTimer: number | undefined;

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
    }

    ngAfterViewInit(): void {
        this.cdr.detectChanges();
    }

    ngOnDestroy(): void {
        if (this.refreshTimer) {
            window.clearInterval(this.refreshTimer);
        }
    }

    refresh(): void {
        this.loading = true;
        this.cdr.markForCheck();
        this.restaurantKdsService
            .getOpenTickets(this.selectedStationId || undefined)
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((result) => {
                this.tickets = result || [];
                this.updateFilteredTickets();
            });
    }

    updateFilteredTickets(): void {
        this.filteredTickets = this.tickets.filter((ticket) => {
            const typeMatch = this.selectedTicketType === '' || ticket.ticketType === Number(this.selectedTicketType);
            const statusMatch = this.selectedStatus === '' || ticket.status === Number(this.selectedStatus);
            return typeMatch && statusMatch;
        });
        this.cdr.markForCheck();
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
