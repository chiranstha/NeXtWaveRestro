import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RestaurantGuestApiService } from './restaurant-guest-api.service';
import { Subscription, timer } from 'rxjs';

@Component({
    selector: 'restaurant-guest-orders',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './restaurant-guest-orders.component.html',
    styleUrl: './restaurant-staff.component.scss',
})
export class RestaurantGuestOrdersComponent implements OnInit, OnDestroy {
    private readonly api = inject(RestaurantGuestApiService);
    private readonly subscriptions = new Subscription();
    orders: any[] = [];
    busyId = '';
    error = '';

    ngOnInit(): void {
        this.load();
        this.subscriptions.add(timer(10000, 10000).subscribe(() => this.load()));
    }
    load(): void {
        this.api.pendingGuestOrders().subscribe({ next: (rows) => this.orders = rows || [], error: (error) => this.error = this.message(error) });
    }
    approve(order: any): void { this.review(order, true); }
    reject(order: any): void {
        const reason = (window.prompt('Reason for rejecting this order') || '').trim();
        if (!reason) return;
        this.review(order, false, reason);
    }
    private review(order: any, approve: boolean, rejectionReason?: string): void {
        this.busyId = order.orderId;
        this.error = '';
        this.api.reviewGuestOrder({ orderId: order.orderId, approve, rejectionReason }).subscribe({
            next: () => { this.busyId = ''; this.load(); },
            error: (error) => { this.busyId = ''; this.error = this.message(error); },
        });
    }
    private message(error: any): string { return error?.error?.error?.message || error?.error?.message || 'Could not update guest orders.'; }
    ngOnDestroy(): void { this.subscriptions.unsubscribe(); }
}
