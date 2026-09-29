import { ChangeDetectionStrategy, Component, Injector, OnInit, inject } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditSalesMasterDto, SalesDetailDto, SalesMastersServiceProxy, SalesProductTableDto } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';
import { CreateRestaurantRefundRequest, RestaurantRefundRecord, RestaurantReleaseApiService } from '../restaurant-release-api.service';

interface RefundLineSelection {
    quantity: number;
    restock: boolean;
}

@Component({
    selector: 'restaurant-refunds',
    templateUrl: './restaurant-refunds.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantRefundsComponent extends AppComponentBase implements OnInit {
    invoiceId = '';
    invoice: CreateOrEditSalesMasterDto | null = null;
    refund: RestaurantRefundRecord | null = null;
    products: SalesProductTableDto[] = [];
    lineSelections: Record<string, RefundLineSelection> = {};
    payoutReferences: Record<string, string> = {};
    currentShift: any = null;
    reason = '';
    managerPin = '';
    tipRefundAmount = 0;
    loading = false;
    saving = false;
    settling = false;

    private readonly salesMasters = inject(SalesMastersServiceProxy);
    private readonly releaseApi = inject(RestaurantReleaseApiService);

    constructor() {
        super(inject(Injector));
    }

    ngOnInit(): void {
        this.salesMasters.getAllProduct().subscribe((rows) => this.products = rows || []);
    }

    loadInvoice(): void {
        const id = this.invoiceId.trim();
        if (!id) return;
        this.loading = true;
        this.invoice = null;
        this.refund = null;
        this.salesMasters.getSalesMasterForEdit(id).pipe(finalize(() => this.loading = false)).subscribe((invoice) => {
            this.invoice = invoice;
            this.lineSelections = Object.fromEntries((invoice.salesDetails || [])
                .filter((line) => !!line.id)
                .map((line) => [line.id!, { quantity: 0, restock: false }]));
        });
    }

    productName(line: SalesDetailDto): string {
        return this.products.find((product) => product.productId === line.productId)?.name || line.productCode || line.productId;
    }

    lineTotal(line: SalesDetailDto): number {
        return Number(line.amount ?? line.netAmount ?? line.grossAmount ?? 0);
    }

    approveRefund(): void {
        if (!this.invoice?.id || !this.reason.trim() || !this.managerPin.trim()) {
            this.notify.warn('Load an invoice, enter a reason, and provide manager approval PIN.');
            return;
        }
        const lines = (this.invoice.salesDetails || []).filter((line) => line.id && Number(this.lineSelections[line.id]?.quantity || 0) > 0)
            .map((line) => {
                const selection = this.lineSelections[line.id!]!;
                return {
                    salesDetailId: line.id!,
                    quantity: Number(selection.quantity),
                    restockQuantity: selection.restock ? Number(selection.quantity) : 0,
                    returnedUnopenedPackagedItem: !!selection.restock,
                };
            });
        if (!lines.length && Number(this.tipRefundAmount) <= 0) {
            this.notify.warn('Choose item quantities or enter a separate tip adjustment.');
            return;
        }
        const request: CreateRestaurantRefundRequest = {
            salesMasterId: this.invoice.id,
            clientRequestId: '',
            reason: this.reason.trim(),
            managerPin: this.managerPin.trim(),
            tipRefundAmount: Number(this.tipRefundAmount || 0),
            lines,
        };
        request.clientRequestId = this.requestId('approval', request);
        this.saving = true;
        this.releaseApi.createRefund(request).pipe(finalize(() => this.saving = false)).subscribe((result) => {
            this.refund = result;
            this.clearRequestId('approval');
            this.reason = '';
            this.managerPin = '';
            this.loadCurrentShift();
            this.notify.success('Refund approved. Record the cashier payout separately when ready.');
        }, (error) => {
            if (error?.status > 0 && error.status < 500) {
                this.notify.error(error?.message || 'The refund was rejected. Review the invoice and refund quantities.');
                return;
            }
            const requestId = request.clientRequestId;
            this.releaseApi.operationStatus('RefundApproval', requestId).subscribe((status) => {
                if (status?.entityId && status?.resultJson) {
                    this.refund = JSON.parse(status.resultJson) as RestaurantRefundRecord;
                    this.loadCurrentShift();
                    this.notify.info('Recovered the server result for this refund request.');
                } else {
                    this.notify.warn('Refund result is unknown. Retry the saved request before creating another refund.');
                }
            }, () => this.notify.warn('Could not confirm the refund result. Retry the same request after checking connectivity.'));
        });
    }

    settlePayout(): void {
        if (!this.refund?.id) return;
        const payouts = this.refund.tenders.map((tender) => ({
            refundTenderId: tender.id,
            amount: Math.max(0, Number(tender.allocatedAmount || 0) - Number(tender.settledAmount || 0)),
            reference: this.payoutReferences[tender.id] || '',
        })).filter((payout) => payout.amount > 0);
        if (!payouts.length) {
            this.notify.info('There is no remaining payout. Any unpaid balance is recorded as a credit note.');
            return;
        }
        const cashPayout = this.refund.tenders.some((tender) => tender.paymentMethod === 0 && tender.allocatedAmount > tender.settledAmount);
        if (cashPayout && (!this.currentShift?.id || this.currentShift?.isClosed)) {
            this.notify.warn('Open a cashier shift before recording a cash refund payout.');
            return;
        }
        const request = {
            refundId: this.refund.id,
            clientRequestId: '',
            cashShiftId: cashPayout ? this.currentShift.id : undefined,
            payouts,
        };
        request.clientRequestId = this.requestId('settlement', request);
        this.settling = true;
        this.releaseApi.settleRefund(request).pipe(finalize(() => this.settling = false)).subscribe((result) => {
            this.refund = result;
            this.clearRequestId('settlement');
            this.notify.success('Refund payout recorded.');
        }, (error) => {
            if (error?.status > 0 && error.status < 500) {
                this.notify.error(error?.message || 'The payout was rejected. Check the shift and payment references.');
                return;
            }
            this.releaseApi.operationStatus('RefundSettlement', request.clientRequestId).subscribe((status) => {
                if (status?.entityId && status?.resultJson) {
                    this.refund = JSON.parse(status.resultJson) as RestaurantRefundRecord;
                    this.notify.info('Recovered the server result for this payout.');
                } else {
                    this.notify.warn('Payout result is unknown. Check its status before retrying or recording another payout.');
                }
            }, () => this.notify.warn('Could not confirm the payout result. Retry the same request after checking connectivity.'));
        });
    }

    tenderName(method: number): string {
        if (method === 0) return 'Cash';
        if (method === 3) return 'Card';
        if (method === 5) return 'QR';
        return `Tender ${method}`;
    }

    private loadCurrentShift(): void {
        this.releaseApi.currentCashShift().subscribe((shift) => this.currentShift = shift);
    }

    private requestId(operation: string, request: object): string {
        const body: any = { ...request };
        delete body.clientRequestId;
        delete body.managerPin;
        const signature = JSON.stringify(body);
        const key = this.storageKey(operation);
        try {
            const previous = JSON.parse(localStorage.getItem(key) || 'null');
            if (previous?.signature === signature && previous?.id) return previous.id;
            const id = typeof crypto !== 'undefined' && 'randomUUID' in crypto
                ? crypto.randomUUID()
                : `${Date.now()}-${Math.random().toString(36).slice(2)}`;
            localStorage.setItem(key, JSON.stringify({ signature, id }));
            return id;
        } catch {
            return `${Date.now()}-${Math.random().toString(36).slice(2)}`;
        }
    }

    private clearRequestId(operation: string): void {
        localStorage.removeItem(this.storageKey(operation));
    }

    private storageKey(operation: string): string {
        return `restaurant-refund:${this.appSession.tenantId ?? 'host'}:${this.appSession.userId ?? 'unknown'}:${operation}`;
    }
}
