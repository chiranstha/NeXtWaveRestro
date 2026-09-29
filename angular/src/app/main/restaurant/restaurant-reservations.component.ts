import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, ICellRendererParams } from 'ag-grid-community';
import { RestaurantGuestApiService } from './restaurant-guest-api.service';

@Component({
    selector: 'restaurant-reservations',
    standalone: true,
    imports: [CommonModule, FormsModule, AgGridAngular],
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

    readonly columnDefs: ColDef[] = [
        {
            headerName: 'Guest',
            minWidth: 190,
            flex: 1.2,
            cellRenderer: (params: ICellRendererParams) => {
                const row = params.data;
                const container = document.createElement('div');
                const name = document.createElement('strong');
                name.textContent = row.guestName || '';
                container.append(name);
                if (row.phoneNumber) {
                    const phone = document.createElement('small');
                    phone.className = 'd-block text-muted';
                    phone.textContent = row.phoneNumber;
                    container.append(phone);
                }
                if (row.isWalkIn) {
                    const walkIn = document.createElement('span');
                    walkIn.className = 'badge bg-light-warning';
                    walkIn.textContent = 'Walk-in';
                    container.append(walkIn);
                }
                return container;
            },
        },
        { headerName: 'Party', field: 'partySize', width: 95 },
        {
            headerName: 'Date and time',
            field: 'startsAt',
            minWidth: 150,
            valueFormatter: (params) => params.value ? new Date(params.value).toLocaleString() : '',
        },
        {
            headerName: 'Table',
            minWidth: 190,
            cellRenderer: (params: ICellRendererParams) => this.createTableSelector(params.data),
        },
        {
            headerName: 'Duration ends',
            minWidth: 175,
            cellRenderer: (params: ICellRendererParams) => this.createEndsAtEditor(params.data),
        },
        { headerName: 'Status', field: 'status', valueFormatter: (params) => this.statusLabel(params.value), width: 130 },
        {
            headerName: 'SMS',
            field: 'smsStatus',
            minWidth: 140,
            cellClass: (params) => params.value?.startsWith('Failed') ? 'status-bad' : undefined,
        },
        {
            headerName: 'Actions',
            minWidth: 245,
            flex: 1,
            sortable: false,
            filter: false,
            cellRenderer: (params: ICellRendererParams) => this.createActions(params.data),
        },
    ];

    readonly defaultColDef: ColDef = { resizable: true, sortable: true, minWidth: 85 };

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

    private createTableSelector(row: any): HTMLElement {
        const select = document.createElement('select');
        select.className = 'form-select form-select-sm';
        const placeholder = document.createElement('option');
        placeholder.value = '';
        placeholder.textContent = 'Choose table';
        select.append(placeholder);
        for (const table of this.suitableTables(row)) {
            const option = document.createElement('option');
            option.value = table.id;
            option.textContent = `${table.name} · ${table.capacity} seats`;
            option.selected = (this.assignedTable[row.id] || row.tableId || '') === table.id;
            select.append(option);
        }
        select.addEventListener('change', () => {
            this.assignedTable[row.id] = select.value;
        });
        return select;
    }

    private createEndsAtEditor(row: any): HTMLElement {
        const input = document.createElement('input');
        input.type = 'datetime-local';
        input.className = 'form-control form-control-sm';
        input.value = this.endsAt[row.id] || this.localDateTime(row.endsAt);
        input.addEventListener('change', () => {
            this.endsAt[row.id] = input.value;
        });
        return input;
    }

    private createActions(row: any): HTMLElement {
        const actions = document.createElement('div');
        actions.className = 'reservation-actions';
        const add = (label: string, cssClass: string, status: number) => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `btn btn-xs ${cssClass}`;
            button.textContent = label;
            button.disabled = this.busyId === row.id;
            button.addEventListener('click', () => this.update(row, status));
            actions.append(button);
        };
        if (row.status === 0 || row.status === 3) add('Confirm', 'btn-success', 1);
        if (row.status === 0) add('Decline', 'btn-light-danger', 2);
        if (row.status === 1 || row.status === 3) add('Seat', 'btn-primary', 4);
        if (row.status === 0 || row.status === 1 || row.status === 3) {
            add('Cancel', 'btn-light-danger', 5);
            add('No-show', 'btn-light', 7);
        }
        return actions;
    }

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
