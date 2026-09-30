import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { AppConsts } from '@shared/AppConsts';

@Injectable({ providedIn: 'root' })
export class RestaurantGuestApiService {
    private readonly http = inject(HttpClient);
    private readonly base = (AppConsts.remoteServiceBaseUrl || '').replace(/\/$/, '');

    menu(body: any): Observable<any> {
        let params = new HttpParams();
        for (const key of ['tenantId', 'tableToken', 'categoryId', 'search']) {
            if (body?.[key] !== undefined && body?.[key] !== null && body[key] !== '') params = params.set(key, String(body[key]));
        }
        return this.http.get<any>(this.url('RestaurantCustomerOrdering', 'GetMenu'), { params }).pipe(map((x) => this.unwrap(x)));
    }
    quote(body: unknown): Observable<any> { return this.post('RestaurantCustomerOrdering', 'Quote', body); }
    createOrder(body: unknown): Observable<any> { return this.post('RestaurantCustomerOrdering', 'CreateOrder', body); }
    orderStatus(body: unknown): Observable<any> { return this.post('RestaurantCustomerOrdering', 'GetOrderStatus', body); }
    requestOtp(body: unknown): Observable<any> { return this.post('RestaurantReservationPublic', 'RequestOtp', body); }
    createReservation(body: unknown): Observable<any> { return this.post('RestaurantReservationPublic', 'CreateReservation', body); }
    reservationStatus(body: unknown): Observable<any> { return this.post('RestaurantReservationPublic', 'GetStatus', body); }
    pendingGuestOrders(): Observable<any[]> { return this.get('RestaurantGuestOperations', 'GetPendingGuestOrders'); }
    reviewGuestOrder(body: unknown): Observable<any> { return this.post('RestaurantGuestOperations', 'ReviewGuestOrder', body); }
    openTableSession(id: string): Observable<any> { return this.post('RestaurantGuestOperations', 'OpenTableSession', { id }); }
    closeTableSession(id: string): Observable<any> { return this.post('RestaurantGuestOperations', 'CloseTableSession', { id }); }
    reservations(from?: string, to?: string): Observable<any[]> {
        let params = new HttpParams();
        if (from) params = params.set('from', from);
        if (to) params = params.set('to', to);
        return this.http.get<any>(this.url('RestaurantGuestOperations', 'GetReservations'), { params }).pipe(map((x) => this.unwrap(x)));
    }
    addWalkIn(body: unknown): Observable<string> { return this.post('RestaurantGuestOperations', 'AddWalkIn', body); }
    updateReservation(body: unknown): Observable<any> { return this.post('RestaurantGuestOperations', 'UpdateReservation', body); }
    generateTableQr(id: string): Observable<any> { return this.post('RestaurantGuestOperations', 'GenerateTableQr', { id }); }
    revokeTableQr(id: string): Observable<any> { return this.post('RestaurantGuestOperations', 'RevokeTableQr', { id }); }
    claimPrintJob(body: unknown): Observable<any> { return this.post('RestaurantGuestOperations', 'ClaimPrintJob', body); }
    reportPrintJob(body: unknown): Observable<any> { return this.post('RestaurantGuestOperations', 'ReportPrintJob', body); }
    retryPrintJob(body: unknown): Observable<any> { return this.post('RestaurantGuestOperations', 'RetryPrintJob', body); }
    printJobs(): Observable<any[]> { return this.get('RestaurantGuestOperations', 'GetPrintJobs'); }
    registerPrintDevice(body: unknown): Observable<any> { return this.post('RestaurantGuestOperations', 'RegisterPrintDevice', body); }
    printDevices(): Observable<any[]> { return this.get('RestaurantGuestOperations', 'GetPrintDevices'); }
    setPrintDeviceEnabled(body: unknown): Observable<any> { return this.post('RestaurantGuestOperations', 'SetPrintDeviceEnabled', body); }
    tables(): Observable<any[]> { return this.get('RestaurantSetup', 'GetTables'); }
    operationalSettings(): Observable<any> { return this.get('RestaurantSetup', 'GetOperationalSettings'); }
    restaurantStations(): Observable<any[]> { return this.get('RestaurantSetup', 'GetStations'); }
    printRoutes(): Observable<any[]> { return this.get('RestaurantSetup', 'GetPrintRoutes'); }
    savePrintRoutes(body: unknown): Observable<any> { return this.post('RestaurantSetup', 'SavePrintRoutes', body); }

    private get<T>(service: string, method: string): Observable<T> {
        return this.http.get<any>(this.url(service, method)).pipe(map((x) => this.unwrap(x)));
    }

    private post<T>(service: string, method: string, body: unknown): Observable<T> {
        return this.http.post<any>(this.url(service, method), body).pipe(map((x) => this.unwrap(x)));
    }

    private url(service: string, method: string): string {
        return `${this.base}/api/services/app/${service}/${method}`;
    }

    private unwrap<T>(value: any): T {
        return (value && Object.prototype.hasOwnProperty.call(value, 'result') ? value.result : value) as T;
    }
}
