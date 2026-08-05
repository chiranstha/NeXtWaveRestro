import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { API_BASE_URL, FileDto, UniversalDropdownDto } from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';

export interface BookReportVoucherTypeDto {
    id: string;
    displayName: string;
    typeOfVoucher: string;
}

export interface BookReportLookupDto {
    ledgers: UniversalDropdownDto[];
    voucherTypes: BookReportVoucherTypeDto[];
    financialYear: {
        fromDate: Date;
        toDate: Date;
        fromMiti: string;
        toMiti: string;
    };
}

export interface BookReportSummaryDto {
    totalRows: number;
    openingBalance: number;
    totalDebit: number;
    totalCredit: number;
    totalIn: number;
    totalOut: number;
    difference: number;
    closingBalance: number;
}

export interface BookReportRowDto {
    sn: number;
    date: Date | string;
    dateMiti: string;
    voucherType: string;
    voucherNo: string;
    ledgerId?: string;
    ledgerName: string;
    groupName: string;
    lineCount: number;
    openingBalance: number;
    debit: number;
    credit: number;
    inAmount: number;
    outAmount: number;
    difference: number;
    closingBalance: number;
    ageDays: number;
    currentAmount: number;
    age1To30: number;
    age31To60: number;
    age61To90: number;
    ageAbove90: number;
    status: string;
    remarks: string;
}

export interface BookReportResultDto {
    reportType: string;
    reportTitle: string;
    requiresLedger: boolean;
    rows: BookReportRowDto[];
    summary: BookReportSummaryDto;
}

@Injectable()
export class BookReportApiService {
    private baseUrl: string;

    constructor(private http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.baseUrl = baseUrl ?? '';
    }

    getLookups(): Observable<BookReportLookupDto> {
        return this.http.get<BookReportLookupDto>(`${this.baseUrl}/api/services/app/BookReport/GetLookups`);
    }

    getReport(
        reportType: string,
        fromMiti?: string,
        toMiti?: string,
        ledgerId?: string,
        voucherTypeId?: string
    ): Observable<BookReportResultDto> {
        return this.http.get<BookReportResultDto>(`${this.baseUrl}/api/services/app/BookReport/GetReport`, {
            params: this.buildParams(reportType, fromMiti, toMiti, ledgerId, voucherTypeId),
        });
    }

    createBookReportToExcel(
        reportType: string,
        fromMiti?: string,
        toMiti?: string,
        ledgerId?: string,
        voucherTypeId?: string
    ): Observable<FileDto> {
        return this.http.post<FileDto>(`${this.baseUrl}/api/services/app/BookReport/CreateBookReportToExcel`, null, {
            params: this.buildParams(reportType, fromMiti, toMiti, ledgerId, voucherTypeId),
        });
    }

    private buildParams(
        reportType: string,
        fromMiti?: string,
        toMiti?: string,
        ledgerId?: string,
        voucherTypeId?: string
    ): HttpParams {
        let params = new HttpParams().set('reportType', reportType || 'day-book');

        if (fromMiti) {
            params = params.set('fromMiti', fromMiti);
        }

        if (toMiti) {
            params = params.set('toMiti', toMiti);
        }

        if (ledgerId) {
            params = params.set('ledgerId', ledgerId);
        }

        if (voucherTypeId) {
            params = params.set('voucherTypeId', voucherTypeId);
        }

        return params;
    }
}
