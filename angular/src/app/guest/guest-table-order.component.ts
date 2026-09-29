import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { RestaurantGuestApiService } from '@app/main/restaurant/restaurant-guest-api.service';

interface CartLine {
    menuItemId: string;
    variantId?: string;
    qty: number;
    modifiers: { modifierId: string; qty: number }[];
    notes?: string;
}

@Component({
    selector: 'guest-table-order',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './guest-table-order.component.html',
    styleUrl: './guest-pages.component.scss',
})
export class GuestTableOrderComponent implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly api = inject(RestaurantGuestApiService);
    tableToken = '';
    menu: any = { categories: [], items: [] };
    categoryId = '';
    search = '';
    cart: CartLine[] = [];
    quote: any;
    loading = false;
    sending = false;
    error = '';
    statusLink = '';
    pendingOrder: any;
    readonly qtyByItem: Record<string, number> = {};
    readonly variantByItem: Record<string, string> = {};
    readonly modifierByItem: Record<string, string[]> = {};

    ngOnInit(): void {
        this.route.paramMap.subscribe((params) => {
            this.tableToken = params.get('token') || '';
            this.loadMenu();
        });
    }

    get visibleItems(): any[] {
        const needle = this.search.trim().toLowerCase();
        return (this.menu?.items || []).filter((item: any) =>
            (!this.categoryId || item.categoryId === this.categoryId) &&
            (!needle || `${item.name} ${item.categoryName} ${item.description || ''}`.toLowerCase().includes(needle)),
        );
    }

    loadMenu(): void {
        if (!this.tableToken) return;
        this.loading = true;
        this.error = '';
        this.api.menu({ tableToken: this.tableToken, categoryId: this.categoryId || undefined, search: this.search || undefined })
            .subscribe({
                next: (menu) => { this.menu = menu || { categories: [], items: [] }; this.loading = false; },
                error: (error) => { this.error = this.errorText(error); this.loading = false; },
            });
    }

    addItem(item: any): void {
        const qty = Math.max(1, Number(this.qtyByItem[item.menuItemId] || 1));
        this.cart.push({
            menuItemId: item.menuItemId,
            variantId: this.variantByItem[item.menuItemId] || undefined,
            qty,
            modifiers: (this.modifierByItem[item.menuItemId] || []).map((modifierId) => ({ modifierId, qty: 1 })),
        });
        this.refreshQuote();
    }

    removeLine(index: number): void {
        this.cart.splice(index, 1);
        this.refreshQuote();
    }

    itemName(itemId: string): string {
        return (this.menu?.items || []).find((item: any) => item.menuItemId === itemId)?.name || 'Menu item';
    }

    toggleModifier(itemId: string, modifierId: string, selected: boolean): void {
        const current = this.modifierByItem[itemId] || [];
        this.modifierByItem[itemId] = selected ? [...current, modifierId] : current.filter((id) => id !== modifierId);
    }

    refreshQuote(): void {
        if (!this.cart.length) { this.quote = undefined; return; }
        this.api.quote({ tableToken: this.tableToken, lines: this.cart }).subscribe({
            next: (quote) => this.quote = quote,
            error: (error) => this.error = this.errorText(error),
        });
    }

    submitOrder(): void {
        if (!this.cart.length || this.sending) return;
        if (!this.pendingOrder) {
            this.pendingOrder = {
                tableToken: this.tableToken,
                paymentMode: 1,
                clientRequestId: crypto.randomUUID(),
                statusAccessToken: this.newToken(),
                lines: this.cart.map((line) => ({ ...line, modifiers: [...line.modifiers] })),
            };
        }
        this.sending = true;
        this.error = '';
        this.api.createOrder(this.pendingOrder).subscribe({
            next: (result) => {
                const order = result?.result || result;
                this.statusLink = `/guest/order/${order.orderId}?access=${encodeURIComponent(order.statusAccessToken || this.pendingOrder.statusAccessToken)}`;
                this.cart = [];
                this.quote = undefined;
                this.pendingOrder = undefined;
                this.sending = false;
            },
            error: (error) => { this.error = this.errorText(error); this.sending = false; },
        });
    }

    private newToken(): string {
        const bytes = crypto.getRandomValues(new Uint8Array(32));
        return Array.from(bytes, (value) => value.toString(16).padStart(2, '0')).join('');
    }

    private errorText(error: HttpErrorResponse | any): string {
        return error?.error?.error?.message || error?.error?.message || error?.message || 'The menu could not be loaded. Please try again.';
    }
}
