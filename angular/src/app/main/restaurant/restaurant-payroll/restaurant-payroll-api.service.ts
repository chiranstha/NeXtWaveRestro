import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { map, Observable } from 'rxjs';

interface AbpAjaxResponse<T> {
    result: T;
}

export interface RestaurantPayrollEmployeeDto {
    id: string;
    userId?: number;
    loginUserName?: string;
    loginEmailAddress?: string;
    loginPhoneNumber?: string;
    restaurantRoleName?: string;
    loginIsActive?: boolean;
    loginManagedByRestaurant: boolean;
    staffCode: string;
    name: string;
    phoneNumber?: string;
    emailAddress?: string;
    dateOfBirth?: string;
    dateOfBirthMiti?: string;
    gender?: string;
    bloodGroup?: string;
    maritalStatus?: string;
    address?: string;
    citizenshipNumber?: string;
    emergencyContactName?: string;
    emergencyContactPhone?: string;
    departmentId: string;
    jobRoleId: string;
    jobRole?: string;
    department?: string;
    employmentType: number;
    basicSalary: number;
    hourlyRate: number;
    overtimeRate: number;
    fixedAllowance: number;
    fixedDeduction: number;
    serviceChargeWeight: number;
    bankName?: string;
    bankAccountNumber?: string;
    panNumber?: string;
    ssfNumber?: string;
    joinedOn: string;
    joinedOnMiti?: string;
    notes?: string;
    isActive: boolean;
}

export interface SaveRestaurantPayrollEmployeeDto {
    id?: string;
    userId?: number;
    updateLoginAccess: boolean;
    loginMode: number;
    loginUserName?: string;
    loginEmailAddress?: string;
    loginPhoneNumber?: string;
    restaurantRoleName?: string;
    loginIsActive: boolean;
    staffCode: string;
    name: string;
    phoneNumber?: string;
    emailAddress?: string;
    dateOfBirth?: string;
    dateOfBirthMiti?: string;
    gender?: string;
    bloodGroup?: string;
    maritalStatus?: string;
    address?: string;
    citizenshipNumber?: string;
    emergencyContactName?: string;
    emergencyContactPhone?: string;
    departmentId: string;
    jobRoleId: string;
    employmentType: number;
    basicSalary: number;
    hourlyRate: number;
    overtimeRate: number;
    fixedAllowance: number;
    fixedDeduction: number;
    serviceChargeWeight: number;
    bankName?: string;
    bankAccountNumber?: string;
    panNumber?: string;
    ssfNumber?: string;
    joinedOn: string;
    joinedOnMiti?: string;
    notes?: string;
    isActive: boolean;
}

export interface RestaurantPayrollEmployeeSaveResultDto {
    employeeId: string;
    userId?: number;
    userName?: string;
    temporaryPassword?: string;
}

export interface RestaurantPayrollDepartmentDto {
    id: string;
    name: string;
    description?: string;
    sortOrder: number;
    isActive: boolean;
}

export interface RestaurantPayrollJobRoleDto {
    id: string;
    departmentId: string;
    name: string;
    description?: string;
    sortOrder: number;
    isActive: boolean;
}

export interface RestaurantPayrollStaffMastersDto {
    departments: RestaurantPayrollDepartmentDto[];
    jobRoles: RestaurantPayrollJobRoleDto[];
}

export interface SaveRestaurantPayrollDepartmentDto {
    id?: string;
    name: string;
    description?: string;
    sortOrder: number;
    isActive: boolean;
}

export interface SaveRestaurantPayrollJobRoleDto {
    id?: string;
    departmentId: string;
    name: string;
    description?: string;
    sortOrder: number;
    isActive: boolean;
}

export interface RestaurantPayrollAllowanceHistoryDto {
    id: string;
    employeeId: string;
    amount: number;
    effectiveFrom: string;
    effectiveFromMiti?: string;
    reason: string;
    createdAt: string;
    createdByUserId?: number;
}

export interface AddRestaurantPayrollAllowanceRevisionDto {
    employeeId: string;
    amount: number;
    effectiveFrom: string;
    effectiveFromMiti?: string;
    reason: string;
}

export interface ChangeRestaurantPayrollEmployeePasswordDto {
    employeeId: string;
    password: string;
    forceChangeOnNextLogin: boolean;
}

export interface RestaurantPayrollUserLookupDto {
    id: number;
    userName: string;
    name: string;
    emailAddress?: string;
    phoneNumber?: string;
    restaurantRoleName?: string;
    isActive: boolean;
}

export interface RestaurantRoleOptionDto {
    name: string;
    displayName: string;
}

export interface RestaurantPayrollStaffAccessOptionsDto {
    roles: RestaurantRoleOptionDto[];
    availableUsers: RestaurantPayrollUserLookupDto[];
}

export interface RestaurantPayrollAttendanceDto {
    id: string;
    employeeId: string;
    employeeName: string;
    staffCode: string;
    workDate: string;
    clockIn?: string;
    clockOut?: string;
    breakMinutes: number;
    regularHours: number;
    overtimeHours: number;
    status: number;
    shiftName?: string;
    notes?: string;
}

export interface SaveRestaurantPayrollAttendanceDto {
    id?: string;
    employeeId: string;
    workDate: string;
    clockIn?: string;
    clockOut?: string;
    breakMinutes: number;
    regularHours?: number;
    overtimeHours?: number;
    status: number;
    shiftName?: string;
    notes?: string;
}

export interface RestaurantPayrollClockDto {
    employeeId?: string;
    at?: string;
    shiftName?: string;
    breakMinutes: number;
}

export interface RestaurantPayrollAdjustmentDto {
    employeeId: string;
    additionalAllowance: number;
    otherDeduction: number;
    advanceRecovery: number;
    taxDeduction: number;
    notes?: string;
}

export interface GenerateRestaurantPayrollRunDto {
    periodStart: string;
    periodEnd: string;
    tipsPool: number;
    serviceChargePool: number;
    notes?: string;
    adjustments: RestaurantPayrollAdjustmentDto[];
}

export interface RestaurantPayrollRunDto {
    id: string;
    runNumber: string;
    periodStart: string;
    periodEnd: string;
    status: number;
    tipsPool: number;
    serviceChargePool: number;
    totalGross: number;
    totalDeduction: number;
    totalNet: number;
    notes?: string;
    createdAt: string;
    employeeCount: number;
}

export interface RestaurantPayrollLineDto {
    id: string;
    employeeId: string;
    employeeName: string;
    staffCode: string;
    jobRole?: string;
    employmentType: number;
    workedHours: number;
    overtimeHours: number;
    basicPay: number;
    overtimePay: number;
    allowance: number;
    tipsShare: number;
    serviceChargeShare: number;
    grossPay: number;
    taxDeduction: number;
    otherDeduction: number;
    advanceRecovery: number;
    netPay: number;
    notes?: string;
    runNumber?: string;
    periodStart?: string;
    periodEnd?: string;
    runStatus?: number;
}

export interface RestaurantPayrollRunDetailDto extends RestaurantPayrollRunDto {
    lines: RestaurantPayrollLineDto[];
}

export interface RestaurantPayrollDashboardDto {
    activeEmployeeCount: number;
    presentToday: number;
    openClockIns: number;
    currentMonthGross: number;
    currentMonthNet: number;
    myProfile?: RestaurantPayrollEmployeeDto;
    myTodayAttendance?: RestaurantPayrollAttendanceDto;
    recentRuns: RestaurantPayrollRunDto[];
    myRecentPayslips: RestaurantPayrollLineDto[];
}

@Injectable()
export class RestaurantPayrollApiService {
    private readonly baseUrl: string;
    private readonly serviceUrl: string;

    constructor(private http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.baseUrl = baseUrl ?? '';
        this.serviceUrl = `${this.baseUrl}/api/services/app/RestaurantPayroll`;
    }

    getDashboard(): Observable<RestaurantPayrollDashboardDto> {
        return this.get<RestaurantPayrollDashboardDto>('GetDashboard');
    }

    getEmployees(includeInactive = false): Observable<RestaurantPayrollEmployeeDto[]> {
        return this.get<RestaurantPayrollEmployeeDto[]>(
            'GetEmployees',
            new HttpParams().set('includeInactive', String(includeInactive))
        );
    }

    getStaffAccessOptions(): Observable<RestaurantPayrollStaffAccessOptionsDto> {
        return this.get<RestaurantPayrollStaffAccessOptionsDto>('GetStaffAccessOptions');
    }

    getStaffMasters(): Observable<RestaurantPayrollStaffMastersDto> {
        return this.get<RestaurantPayrollStaffMastersDto>('GetStaffMasters');
    }

    saveDepartment(input: SaveRestaurantPayrollDepartmentDto): Observable<string> {
        return this.post<string>('SaveDepartment', input);
    }

    saveJobRole(input: SaveRestaurantPayrollJobRoleDto): Observable<string> {
        return this.post<string>('SaveJobRole', input);
    }

    getAllowanceHistory(employeeId: string): Observable<RestaurantPayrollAllowanceHistoryDto[]> {
        return this.get<RestaurantPayrollAllowanceHistoryDto[]>(
            'GetAllowanceHistory',
            new HttpParams().set('employeeId', employeeId)
        );
    }

    addAllowanceRevision(input: AddRestaurantPayrollAllowanceRevisionDto): Observable<string> {
        return this.post<string>('AddAllowanceRevision', input);
    }

    saveEmployee(input: SaveRestaurantPayrollEmployeeDto): Observable<RestaurantPayrollEmployeeSaveResultDto> {
        return this.post<RestaurantPayrollEmployeeSaveResultDto>('CreateOrEditEmployee', input);
    }

    resetEmployeeDefaultPassword(employeeId: string): Observable<void> {
        return this.post<void>('ResetEmployeeDefaultPassword', { id: employeeId });
    }

    setEmployeePassword(input: ChangeRestaurantPayrollEmployeePasswordDto): Observable<void> {
        return this.post<void>('SetEmployeePassword', input);
    }

    getAttendance(from: string, to: string, employeeId?: string): Observable<RestaurantPayrollAttendanceDto[]> {
        let params = new HttpParams().set('from', from).set('to', to);
        if (employeeId) {
            params = params.set('employeeId', employeeId);
        }
        return this.get<RestaurantPayrollAttendanceDto[]>('GetAttendance', params);
    }

    clockIn(input: RestaurantPayrollClockDto): Observable<RestaurantPayrollAttendanceDto> {
        return this.post<RestaurantPayrollAttendanceDto>('ClockIn', input);
    }

    clockOut(input: RestaurantPayrollClockDto): Observable<RestaurantPayrollAttendanceDto> {
        return this.post<RestaurantPayrollAttendanceDto>('ClockOut', input);
    }

    saveAttendance(input: SaveRestaurantPayrollAttendanceDto): Observable<string> {
        return this.post<string>('SaveAttendance', input);
    }

    generatePayrollRun(input: GenerateRestaurantPayrollRunDto): Observable<string> {
        return this.post<string>('GeneratePayrollRun', input);
    }

    getPayrollRuns(): Observable<RestaurantPayrollRunDto[]> {
        return this.get<RestaurantPayrollRunDto[]>('GetPayrollRuns');
    }

    getPayrollRun(id: string): Observable<RestaurantPayrollRunDetailDto> {
        return this.get<RestaurantPayrollRunDetailDto>('GetPayrollRun', new HttpParams().set('id', id));
    }

    getMyPayslips(): Observable<RestaurantPayrollLineDto[]> {
        return this.get<RestaurantPayrollLineDto[]>('GetMyPayslips');
    }

    approvePayrollRun(id: string): Observable<void> {
        return this.post<void>('ApprovePayrollRun', { id });
    }

    markPayrollRunPaid(id: string): Observable<void> {
        return this.post<void>('MarkPayrollRunPaid', { id });
    }

    deleteDraftPayrollRun(id: string): Observable<void> {
        return this.post<void>('DeleteDraftPayrollRun', { id });
    }

    private get<T>(action: string, params?: HttpParams): Observable<T> {
        return this.http
            .get<AbpAjaxResponse<T>>(`${this.serviceUrl}/${action}`, { params })
            .pipe(map((response) => response.result));
    }

    private post<T>(action: string, body: unknown): Observable<T> {
        return this.http
            .post<AbpAjaxResponse<T>>(`${this.serviceUrl}/${action}`, body)
            .pipe(map((response) => response.result));
    }
}
