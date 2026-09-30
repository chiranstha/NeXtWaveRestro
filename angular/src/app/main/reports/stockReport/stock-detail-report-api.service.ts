import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';

export interface StockDetailReportRow {
    id: string;
    date: string | Date;
    dateMiti: string;
    voucherNo: string;
    voucherType: string;
    ledgerName: string;
    unitName: string;
    inwardQty: number;
    inwardRate: number;
    inwardAmount: number;
    outwardQty: number;
    outwardRate: number;
    outwardAmount: number;
    balanceQty: number;
    balanceAmount: number;
}

export interface StockDetailReport {
    productName: string;
    unitName: string;
    openingQty: number;
    openingAmount: number;
    rows: StockDetailReportRow[];
}

@Injectable()
export class StockDetailReportApiService {
    private readonly baseUrl: string;

    constructor(private http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.baseUrl = baseUrl ?? '';
    }

    getReport(productId: string, fromMiti: string, toMiti: string): Observable<StockDetailReport> {
        const params = new HttpParams()
            .set('productId', productId)
            .set('fromMiti', fromMiti || '')
            .set('toMiti', toMiti || '');
        return this.http.get<StockDetailReport>(`${this.baseUrl}/api/services/app/StockReport/GetStockDetail`, { params });
    }
}
