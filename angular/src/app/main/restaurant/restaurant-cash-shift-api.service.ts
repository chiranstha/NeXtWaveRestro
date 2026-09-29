import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { map, Observable } from 'rxjs';

export interface RestaurantCashShift {
    id: string;
    registerName: string;
    openingCash: number;
    expectedClosingCash: number;
    cashSales: number;
    cashIn: number;
    cashOut: number;
    isClosed: boolean;
    cashVariance?: number | null;
}

@Injectable()
export class RestaurantCashShiftApiService {
    private readonly baseUrl: string;

    constructor(private http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.baseUrl = (baseUrl || '').replace(/\/$/, '');
    }

    current(): Observable<RestaurantCashShift | null> {
        const params = new HttpParams().set('registerName', 'Main');
        return this.http.get<any>(`${this.baseUrl}/api/services/app/RestaurantCashShift/GetCurrent`, { params })
            .pipe(map((response) => response?.result ?? null));
    }

    open(openingCash: number): Observable<RestaurantCashShift> {
        return this.post('Open', { registerName: 'Main', openingCash });
    }

    movement(shiftId: string, isCashIn: boolean, amount: number, reason: string): Observable<RestaurantCashShift> {
        return this.post('AddMovement', { shiftId, isCashIn, amount, reason });
    }

    close(shiftId: string, countedCash: number, note: string, managerPin: string): Observable<RestaurantCashShift> {
        return this.post('Close', { shiftId, countedCash, note, managerPin });
    }

    private post(method: string, body: unknown): Observable<RestaurantCashShift> {
        return this.http.post<any>(`${this.baseUrl}/api/services/app/RestaurantCashShift/${method}`, body)
            .pipe(map((response) => response?.result ?? response));
    }
}
