import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable, map } from 'rxjs';

export interface RestaurantDailySalesSummaryReportDto {
    date: string | Date;
    outletName: string;
    tableName: string;
    userId?: number;
    userName: string;
    orderCount: number;
    grossAmount: number;
    discountAmount: number;
    taxAmount: number;
    netAmount: number;
    grandTotal: number;
    averageBill: number;
}

export interface RestaurantTicketStatusReportDto {
    ticketNo: string;
    orderNo: string;
    ticketType: string;
    status: string;
    stationName: string;
    tableName: string;
    waiterName: string;
    sentAt: string | Date;
    cancelledAt?: string | Date;
    printCount: number;
    itemCount: number;
    qty: number;
    cancelReason: string;
}

export interface RestaurantItemMarginReportDto {
    productId: string;
    productName: string;
    categoryId?: string;
    categoryName: string;
    qty: number;
    grossAmount: number;
    discountAmount: number;
    taxAmount: number;
    netAmount: number;
    grandTotal: number;
    costAmount: number;
    marginAmount: number;
    marginPercent: number;
}

export interface RestaurantWaiterPerformanceReportDto {
    waiterUserId?: number;
    waiterName: string;
    orderCount: number;
    itemCount: number;
    guestCount: number;
    grossAmount: number;
    discountAmount: number;
    grandTotal: number;
    averageBill: number;
}

export interface RestaurantTableTurnoverReportDto {
    tableId?: string;
    tableName: string;
    sessionCount: number;
    orderCount: number;
    guestCount: number;
    totalMinutes: number;
    averageMinutes: number;
    grandTotal: number;
    averageBill: number;
}

export interface RestaurantVoidAuditReportDto {
    date: string | Date;
    orderNo: string;
    ticketNo: string;
    tableName: string;
    waiterName: string;
    itemName: string;
    qty: number;
    amount: number;
    status: string;
    reason: string;
}

export interface RestaurantDiscountReportDto {
    date: string | Date;
    orderNo: string;
    userName: string;
    tableName: string;
    customerName: string;
    grossAmount: number;
    orderDiscountAmount: number;
    itemDiscountAmount: number;
    totalDiscountAmount: number;
    reason: string;
}

export interface RestaurantSettlementReportDto {
    paymentMethod: number;
    paymentMethodName: string;
    orderCount: number;
    grossAmount: number;
    discountAmount: number;
    taxAmount: number;
    netAmount: number;
    grandTotal: number;
    collectedAmount: number;
    tipAmount: number;
    cashReceivedAmount: number;
    changeAmount: number;
}

export interface RestaurantRecipeCostingLineDto {
    rawMaterialId: string;
    rawMaterialName: string;
    unitName: string;
    quantity: number;
    wastagePercentage: number;
    costRate: number;
    costAmount: number;
}

export interface RestaurantRecipeCostingReportDto {
    productId: string;
    productName: string;
    categoryName: string;
    menuPrice: number;
    recipeCost: number;
    foodCostPercent: number;
    marginAmount: number;
    lines: RestaurantRecipeCostingLineDto[];
}

export interface RestaurantFoodCostingReportDto {
    productId: string;
    productName: string;
    categoryName: string;
    soldQty: number;
    salesAmount: number;
    theoreticalCostAmount: number;
    actualCostAmount: number;
    wastageCostAmount: number;
    marginAmount: number;
    foodCostPercent: number;
}

export interface RestaurantWastageReportDto {
    productId: string;
    productName: string;
    unitName: string;
    qty: number;
    rate: number;
    amount: number;
    reason: string;
    userName: string;
    date: string | Date;
}

export interface RestaurantLowStockReportDto {
    productId: string;
    productName: string;
    unitName: string;
    availableQty: number;
    minimumStock: number;
    maximumStock: number;
    pendingPurchaseQty: number;
    supplierName: string;
    suggestedQty: number;
    missingSupplierMapping: boolean;
}

export interface RestaurantPayrollReportSummaryDto {
    activeEmployeeCount: number;
    payrollRunCount: number;
    attendanceRecordCount: number;
    totalGross: number;
    totalDeduction: number;
    totalNet: number;
    totalOvertimeHours: number;
}

export interface RestaurantPayrollRunReportDto {
    id: string;
    runNumber: string;
    periodStart: string | Date;
    periodEnd: string | Date;
    status: number;
    tipsPool: number;
    serviceChargePool: number;
    totalGross: number;
    totalDeduction: number;
    totalNet: number;
    employeeCount: number;
}

export interface RestaurantPayrollEmployeeCostReportDto {
    employeeId: string;
    employeeName: string;
    staffCode: string;
    jobRole: string;
    workedHours: number;
    overtimeHours: number;
    basicPay: number;
    allowance: number;
    tipsAndServiceCharge: number;
    grossPay: number;
    totalDeduction: number;
    netPay: number;
}

export interface RestaurantPayrollAttendanceStatusReportDto {
    status: number;
    statusName: string;
    recordCount: number;
    regularHours: number;
    overtimeHours: number;
}

export interface RestaurantPayrollReportBundleDto {
    summary: RestaurantPayrollReportSummaryDto;
    runs: RestaurantPayrollRunReportDto[];
    employeeCosts: RestaurantPayrollEmployeeCostReportDto[];
    attendance: RestaurantPayrollAttendanceStatusReportDto[];
}

@Injectable()
export class RestaurantReportsApiService {
    private baseUrl: string;

    constructor(private http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.baseUrl = baseUrl ?? '';
    }

    getDailySalesSummary(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantDailySalesSummaryReportDto[]> {
        return this.get<RestaurantDailySalesSummaryReportDto[]>('GetDailySalesSummary', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getKotBotStatus(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantTicketStatusReportDto[]> {
        return this.get<RestaurantTicketStatusReportDto[]>('GetKotBotStatus', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getItemSalesWithMargin(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantItemMarginReportDto[]> {
        return this.get<RestaurantItemMarginReportDto[]>('GetItemSalesWithMargin', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getWaiterPerformance(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantWaiterPerformanceReportDto[]> {
        return this.get<RestaurantWaiterPerformanceReportDto[]>('GetWaiterPerformance', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getTableTurnover(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantTableTurnoverReportDto[]> {
        return this.get<RestaurantTableTurnoverReportDto[]>('GetTableTurnover', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getVoidCancelledAudit(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantVoidAuditReportDto[]> {
        return this.get<RestaurantVoidAuditReportDto[]>('GetVoidCancelledAudit', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getDiscountReport(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantDiscountReportDto[]> {
        return this.get<RestaurantDiscountReportDto[]>('GetDiscountReport', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getSettlementReport(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantSettlementReportDto[]> {
        return this.get<RestaurantSettlementReportDto[]>('GetSettlementReport', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getRecipeCosting(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantRecipeCostingReportDto[]> {
        return this.get<RestaurantRecipeCostingReportDto[]>('GetRecipeCosting', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getFoodCosting(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantFoodCostingReportDto[]> {
        return this.get<RestaurantFoodCostingReportDto[]>('GetFoodCosting', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getWastageReport(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantWastageReportDto[]> {
        return this.get<RestaurantWastageReportDto[]>('GetWastageReport', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getLowStockReport(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantLowStockReportDto[]> {
        return this.get<RestaurantLowStockReportDto[]>('GetLowStockReport', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    getPayrollReport(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<RestaurantPayrollReportBundleDto> {
        return this.get<RestaurantPayrollReportBundleDto>('GetPayrollReport', fromDate, toDate, tableId, waiterUserId, categoryId);
    }

    private get<T>(
        action: string,
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): Observable<T> {
        return this.http
            .get<T | { result: T }>(`${this.baseUrl}/api/services/app/RestaurantReports/${action}`, {
                params: this.buildParams(fromDate, toDate, tableId, waiterUserId, categoryId),
            })
            .pipe(
                map((response) =>
                    response && typeof response === 'object' && 'result' in response
                        ? (response as { result: T }).result
                        : (response as T),
                ),
            );
    }

    private buildParams(
        fromDate: DateTime | null,
        toDate: DateTime | null,
        tableId: string | null,
        waiterUserId: number | null,
        categoryId: string | null
    ): HttpParams {
        let params = new HttpParams();

        if (fromDate) {
            params = params.set('FromDate', fromDate.toString());
        }

        if (toDate) {
            params = params.set('ToDate', toDate.toString());
        }

        if (tableId) {
            params = params.set('TableId', tableId);
        }

        if (waiterUserId) {
            params = params.set('WaiterUserId', waiterUserId.toString());
        }

        if (categoryId) {
            params = params.set('CategoryId', categoryId);
        }

        return params;
    }
}
