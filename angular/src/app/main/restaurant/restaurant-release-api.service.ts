import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { Observable, map } from 'rxjs';

export interface RestaurantReleaseCapabilities {
    mixedTenderEnabled: boolean;
    refundsEnabled: boolean;
    orderVersionChecksEnabled: boolean;
    androidDraftRecoveryEnabled: boolean;
    supportsRefundSettlement: boolean;
    supportsOrderVersioning: boolean;
    refundPayableLedgerId?: string | null;
    cardLedgerId?: string | null;
    qrLedgerId?: string | null;
    paymentMethods: Array<{ method: number; enabled: boolean; ledgerId?: string | null }>;
}

export interface RestaurantSetupCheck {
    key: string;
    label: string;
    isReady: boolean;
    details: string;
    completedByUserId?: number | null;
    completedAt?: string | null;
}

export interface RestaurantSetupReadiness {
    isReady: boolean;
    checks: RestaurantSetupCheck[];
}

export interface RestaurantDailyClosing {
    businessDate: string;
    periodStartUtc: string;
    periodEndUtc: string;
    sales: number;
    collected: number;
    unpaidBalance: number;
    discounts: number;
    voids: number;
    wastage: number;
    refunds: number;
    tips: number;
    collectionsByTender: Array<{ paymentMethod: number; amount: number }>;
    shifts: Array<{
        shiftId: string;
        registerName: string;
        cashierUserId: number;
        openingCash: number;
        cashSales: number;
        cashIn: number;
        cashOut: number;
        expectedCash: number;
        countedCash?: number | null;
        difference?: number | null;
    }>;
}

export interface CreateRestaurantRefundRequest {
    salesMasterId: string;
    clientRequestId: string;
    reason: string;
    managerPin: string;
    tipRefundAmount: number;
    lines: Array<{
        salesDetailId: string;
        quantity: number;
        restockQuantity: number;
        returnedUnopenedPackagedItem: boolean;
    }>;
}

export interface RestaurantRefundRecord {
    id: string;
    salesMasterId: string;
    status: string;
    reason: string;
    itemRefundAmount: number;
    tipRefundAmount: number;
    creditNoteAmount: number;
    payoutAmount: number;
    settledAmount: number;
    tenders: Array<{
        id: string;
        paymentMethod: number;
        paymentLedgerId: string;
        allocatedAmount: number;
        settledAmount: number;
    }>;
}

@Injectable()
export class RestaurantReleaseApiService {
    private readonly baseUrl: string;

    constructor(private readonly http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.baseUrl = (baseUrl || '').replace(/\/$/, '');
    }

    capabilities(): Observable<RestaurantReleaseCapabilities> {
        return this.get('RestaurantRelease', 'GetCapabilities');
    }

    readiness(): Observable<RestaurantSetupReadiness> {
        return this.get('RestaurantRelease', 'GetSetupReadiness');
    }

    acknowledgeSetupCheck(checkKey: number, note: string): Observable<void> {
        return this.post('RestaurantRelease', 'AcknowledgeSetupCheck', { checkKey, note });
    }

    updateFeatures(features: {
        mixedTenderEnabled: boolean;
        refundsEnabled: boolean;
        orderVersionChecksEnabled: boolean;
        androidDraftRecoveryEnabled: boolean;
        clientCompatibilityConfirmed: boolean;
    }): Observable<void> {
        return this.post('RestaurantRelease', 'UpdateReleaseFeatures', features);
    }

    dailyClosing(date: string): Observable<RestaurantDailyClosing> {
        const params = new HttpParams().set('businessDate', `${date}T00:00:00`);
        return this.http.get<any>(`${this.baseUrl}/api/services/app/RestaurantReports/GetDailyClosing`, { params })
            .pipe(map((response) => response?.result ?? response));
    }

    createRefund(input: CreateRestaurantRefundRequest): Observable<RestaurantRefundRecord> {
        return this.post('RestaurantRefund', 'Create', input);
    }

    settleRefund(input: {
        refundId: string;
        clientRequestId: string;
        cashShiftId?: string;
        payouts: Array<{ refundTenderId: string; amount: number; reference?: string }>;
    }): Observable<RestaurantRefundRecord> {
        return this.post('RestaurantRefund', 'Settle', input);
    }

    refund(id: string): Observable<RestaurantRefundRecord> {
        return this.get('RestaurantRefund', 'Get', { id });
    }

    operationStatus(operationType: string, clientRequestId: string): Observable<any> {
        return this.get('RestaurantRefund', 'GetOperationStatus', { operationType, clientRequestId });
    }

    orderOperationStatus(operationType: string, clientRequestId: string): Observable<any> {
        return this.get('RestaurantOrder', 'GetOperationStatus', { operationType, clientRequestId });
    }

    currentCashShift(): Observable<any> {
        const params = new HttpParams().set('registerName', 'Main');
        return this.http.get<any>(`${this.baseUrl}/api/services/app/RestaurantCashShift/GetCurrent`, { params })
            .pipe(map((response) => response?.result ?? response));
    }

    private get<T>(service: string, action: string, query?: Record<string, string>): Observable<T> {
        let params = new HttpParams();
        Object.entries(query || {}).forEach(([key, value]) => params = params.set(key, value));
        return this.http.get<any>(`${this.baseUrl}/api/services/app/${service}/${action}`, { params })
            .pipe(map((response) => response?.result ?? response));
    }

    private post<T = void>(service: string, action: string, body: unknown): Observable<T> {
        return this.http.post<any>(`${this.baseUrl}/api/services/app/${service}/${action}`, body)
            .pipe(map((response) => response?.result ?? response));
    }
}
