import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription, timer, switchMap } from 'rxjs';
import { RestaurantGuestApiService } from '@app/main/restaurant/restaurant-guest-api.service';

@Component({
    selector: 'guest-reservation',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './guest-reservation.component.html',
    styleUrl: './guest-pages.component.scss',
})
export class GuestReservationComponent implements OnInit, OnDestroy {
    private readonly route = inject(ActivatedRoute);
    private readonly api = inject(RestaurantGuestApiService);
    private readonly subscriptions = new Subscription();
    tenant = '';
    guestName = '';
    phoneNumber = '';
    partySize = 2;
    startsAt = this.defaultStart();
    notes = '';
    code = '';
    challengeId = '';
    statusId = '';
    statusToken = '';
    status: any;
    error = '';
    message = '';
    loading = false;
    statusLink = '';

    get statusLabel(): string {
        return ['Request received', 'Confirmed', 'Declined', 'Waitlisted', 'Seated', 'Cancelled', 'Completed', 'No-show'][this.status?.status] || 'Request received';
    }

    ngOnInit(): void {
        this.tenant = this.route.snapshot.paramMap.get('tenant') || '';
        this.statusId = this.route.snapshot.queryParamMap.get('reservationId') || '';
        this.statusToken = this.route.snapshot.queryParamMap.get('accessToken') || '';
        if (this.statusId && this.statusToken) {
            this.statusLink = location.href;
            this.startStatusPolling();
        }
    }

    requestCode(): void {
        if (!this.tenant || !this.phoneNumber.trim()) { this.error = 'Enter your phone number first.'; return; }
        this.loading = true;
        this.error = '';
        this.message = '';
        this.api.requestOtp({ tenancyName: this.tenant, phoneNumber: this.phoneNumber }).subscribe({
            next: (result) => {
                this.challengeId = result?.challengeId || '';
                this.loading = false;
                this.message = 'Verification code queued for delivery. A code may take a moment to arrive.';
            },
            error: (error) => { this.loading = false; this.error = this.errorText(error); },
        });
    }

    createReservation(): void {
        if (!this.challengeId || !this.guestName.trim() || !this.startsAt || this.partySize < 1) {
            this.error = 'Enter your name, time, party size, phone, and verification code.';
            return;
        }
        this.loading = true;
        this.error = '';
        this.api.createReservation({
            tenancyName: this.tenant,
            challengeId: this.challengeId,
            verificationCode: this.code,
            guestName: this.guestName,
            phoneNumber: this.phoneNumber,
            partySize: this.partySize,
            startsAt: this.startsAt,
            notes: this.notes,
        }).subscribe({
            next: (result) => {
                this.loading = false;
                if (result?.error) { this.error = result.error; return; }
                this.statusId = result.id;
                this.statusToken = result.statusAccessToken;
                this.statusLink = `${location.origin}/guest/${encodeURIComponent(this.tenant)}/reserve?reservationId=${encodeURIComponent(this.statusId)}&accessToken=${encodeURIComponent(this.statusToken)}`;
                this.message = 'Your request is waiting for staff review. We will send an update after it is reviewed.';
                this.startStatusPolling();
            },
            error: (error) => { this.loading = false; this.error = this.errorText(error); },
        });
    }

    private defaultStart(): string {
        const date = new Date(Date.now() + 2 * 60 * 60 * 1000);
        date.setMinutes(0, 0, 0);
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}T${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
    }

    private errorText(error: any): string {
        return error?.error?.error?.message || error?.error?.message || error?.message || 'The request could not be completed.';
    }

    refreshStatus(): void {
        this.api.reservationStatus({ reservationId: this.statusId, statusAccessToken: this.statusToken }).subscribe({
            next: (value) => { this.status = value; this.error = ''; },
            error: () => this.error = 'Reservation status is unavailable.',
        });
    }

    private startStatusPolling(): void {
        this.subscriptions.add(timer(0, 30000).pipe(switchMap(() => this.api.reservationStatus({ reservationId: this.statusId, statusAccessToken: this.statusToken })))
            .subscribe({ next: (result) => { this.status = result; this.error = ''; }, error: () => this.error = 'Reservation status is unavailable.' }));
    }

    ngOnDestroy(): void { this.subscriptions.unsubscribe(); }
}
