import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RestaurantGuestApiService } from './restaurant-guest-api.service';

@Component({
    selector: 'restaurant-reservations',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './restaurant-reservations.component.html',
    styleUrl: './restaurant-staff.component.scss',
})
export class RestaurantReservationsComponent implements OnInit {
    private readonly api = inject(RestaurantGuestApiService);
    reservations: any[] = [];
    tables: any[] = [];
    assignedTable: Record<string, string> = {};
    endsAt: Record<string, string> = {};
    guestName = '';
    phoneNumber = '';
    partySize = 2;
    notes = '';
    busyId = '';
    error = '';
    message = '';
    loading = false;

    ngOnInit(): void { this.load(); }

    load(): void {
        this.loading = true;
        this.api.tables().subscribe({ next: (rows) => this.tables = (rows || []).filter((x: any) => x.isActive), error: (e) => this.error = this.errorText(e) });
        this.api.reservations().subscribe({
            next: (rows) => {
                this.reservations = rows || [];
                for (const row of this.reservations) {
                    this.assignedTable[row.id] = row.tableId || this.assignedTable[row.id] || '';
                    this.endsAt[row.id] = this.endsAt[row.id] || this.localDateTime(row.endsAt);
                }
                this.loading = false;
            }, error: (e) => { this.loading = false; this.error = this.errorText(e); },
        });
    }

    suitableTables(row: any): any[] { return this.tables.filter((table) => table.capacity >= row.partySize); }

    addWalkIn(): void {
        if (!this.guestName.trim() || this.partySize < 1) { this.error = 'Enter the guest name and party size.'; return; }
        this.loading = true;
        this.api.addWalkIn({ guestName: this.guestName, phoneNumber: this.phoneNumber, partySize: this.partySize, notes: this.notes }).subscribe({
            next: () => { this.guestName = ''; this.phoneNumber = ''; this.partySize = 2; this.notes = ''; this.message = 'Walk-in added to the waiting list.'; this.loading = false; this.load(); },
            error: (e) => { this.loading = false; this.error = this.errorText(e); },
        });
    }

    confirm(row: any): void { this.update(row, 1); }
    decline(row: any): void { this.update(row, 2); }
    seat(row: any): void { this.update(row, 4); }
    cancel(row: any): void { this.update(row, 5); }
    noShow(row: any): void { this.update(row, 7); }

    private update(row: any, status: number): void {
        const tableId = this.assignedTable[row.id] || row.tableId;
        if ((status === 1 || status === 4) && !tableId) { this.error = 'Choose a suitable table first.'; return; }
        this.busyId = row.id;
        this.error = '';
        const endsAtValue = this.endsAt[row.id];
        const endsAt = endsAtValue ? new Date(endsAtValue).toISOString() : undefined;
        this.api.updateReservation({ id: row.id, status, tableId, endsAt }).subscribe({
            next: () => { this.busyId = ''; this.message = status === 4 ? 'Guest seated; table session opened for QR ordering.' : 'Reservation updated.'; this.load(); },
            error: (e) => { this.busyId = ''; this.error = this.errorText(e); },
        });
    }

    statusLabel(status: number): string { return ['Requested', 'Confirmed', 'Declined', 'Waiting', 'Seated', 'Cancelled', 'Completed', 'No-show'][status] || 'Requested'; }
    private localDateTime(value: string): string {
        const date = new Date(value);
        const two = (n: number) => String(n).padStart(2, '0');
        return `${date.getFullYear()}-${two(date.getMonth() + 1)}-${two(date.getDate())}T${two(date.getHours())}:${two(date.getMinutes())}`;
    }
    private errorText(error: any): string { return error?.error?.error?.message || error?.error?.message || 'Could not update reservations.'; }
}
