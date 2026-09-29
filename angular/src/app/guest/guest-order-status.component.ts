import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { timer, Subscription, switchMap } from 'rxjs';
import { RestaurantGuestApiService } from '@app/main/restaurant/restaurant-guest-api.service';

@Component({
    selector: 'guest-order-status',
    standalone: true,
    imports: [CommonModule],
    template: `
        <main class="guest-shell">
            <header class="guest-header"><span class="guest-eyebrow">ORDER STATUS</span><h1>Your order</h1><p>Staff review the request before sending it to the kitchen.</p></header>
            <div class="guest-alert guest-alert-error" *ngIf="error">{{ error }}</div>
            <section class="guest-panel" *ngIf="status">
                <div class="guest-status-row"><span>Order</span><strong>{{ status.orderNo }}</strong></div>
                <div class="guest-status-row"><span>Staff review</span><span class="guest-status-badge">{{ approvalLabel }}</span></div>
                <div class="guest-status-row"><span>Kitchen</span><strong>{{ orderLabel }}</strong></div>
                <div class="guest-status-row"><span>Estimated total</span><strong>{{ status.grandTotal | number:'1.2-2' }}</strong></div>
                <p *ngIf="status.guestRejectionReason" class="guest-alert guest-alert-error">{{ status.guestRejectionReason }}</p>
                <button class="guest-button guest-button-light" (click)="refresh()">Refresh status</button>
            </section>
        </main>`,
    styleUrl: './guest-pages.component.scss',
})
export class GuestOrderStatusComponent implements OnInit, OnDestroy {
    private readonly route = inject(ActivatedRoute);
    private readonly api = inject(RestaurantGuestApiService);
    private readonly subscriptions = new Subscription();
    orderId = '';
    access = '';
    status: any;
    error = '';

    get approvalLabel(): string { return ['Waiting for staff review', 'Approved', 'Declined'][this.status?.guestApprovalStatus] || 'Processing'; }
    get orderLabel(): string { return ['Draft', 'Sent to kitchen', 'Preparing', 'Ready', 'Served', 'Billed', 'Closed', 'Cancelled'][this.status?.status] || 'Processing'; }

    ngOnInit(): void {
        this.orderId = this.route.snapshot.paramMap.get('orderId') || '';
        this.access = this.route.snapshot.queryParamMap.get('access') || '';
        this.subscriptions.add(timer(0, 20000).pipe(switchMap(() => this.api.orderStatus({ orderId: this.orderId, statusAccessToken: this.access })))
            .subscribe({ next: (result) => { this.status = result; this.error = ''; }, error: (error) => this.error = error?.error?.error?.message || 'Order status is unavailable.' }));
    }
    refresh(): void { this.api.orderStatus({ orderId: this.orderId, statusAccessToken: this.access }).subscribe({ next: (result) => { this.status = result; this.error = ''; }, error: () => this.error = 'Order status is unavailable.' }); }
    ngOnDestroy(): void { this.subscriptions.unsubscribe(); }
}
