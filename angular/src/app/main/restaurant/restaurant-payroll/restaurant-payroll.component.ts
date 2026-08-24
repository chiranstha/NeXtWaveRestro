import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Injector, OnInit, ViewEncapsulation, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NepaliDatepickerService } from '@app/shared/common/nepalidatepicker/services/nepali-datepicker-angular.service';
import { finalize, forkJoin, of } from 'rxjs';
import {
    GenerateRestaurantPayrollRunDto,
    RestaurantPayrollAllowanceHistoryDto,
    RestaurantPayrollAdjustmentDto,
    RestaurantPayrollApiService,
    RestaurantPayrollAttendanceDto,
    RestaurantPayrollDashboardDto,
    RestaurantPayrollEmployeeDto,
    RestaurantPayrollLineDto,
    RestaurantPayrollRunDetailDto,
    RestaurantPayrollRunDto,
    RestaurantPayrollStaffAccessOptionsDto,
    RestaurantPayrollStaffMastersDto,
    RestaurantPayrollUserLookupDto,
    SaveRestaurantPayrollAttendanceDto,
    SaveRestaurantPayrollEmployeeDto,
} from './restaurant-payroll-api.service';

type PayrollSection = 'overview' | 'staff' | 'attendance' | 'runs' | 'payslips';

interface PayrollAdjustmentEditor {
    additionalAllowance: number;
    otherDeduction: number;
    advanceRecovery: number;
    taxDeduction: number;
    notes: string;
}

@Component({
    selector: 'restaurant-payroll',
    templateUrl: './restaurant-payroll.component.html',
    styleUrls: ['./restaurant-payroll.component.css'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantPayrollComponent extends AppComponentBase implements OnInit {
    activeSection: PayrollSection = 'overview';
    loading = false;
    saving = false;
    includeInactive = false;
    employeeSearch = '';
    employeeEditorOpen = false;
    staffSetupOpen = false;

    dashboard: RestaurantPayrollDashboardDto = this.emptyDashboard();
    employees: RestaurantPayrollEmployeeDto[] = [];
    attendance: RestaurantPayrollAttendanceDto[] = [];
    payrollRuns: RestaurantPayrollRunDto[] = [];
    selectedRun?: RestaurantPayrollRunDetailDto;
    payslips: RestaurantPayrollLineDto[] = [];
    accessOptions: RestaurantPayrollStaffAccessOptionsDto = { roles: [], availableUsers: [] };
    staffMasters: RestaurantPayrollStaffMastersDto = { departments: [], jobRoles: [] };
    currentEditedEmployee?: RestaurantPayrollEmployeeDto;
    profileEmployee?: RestaurantPayrollEmployeeDto;
    passwordEmployee?: RestaurantPayrollEmployeeDto;
    allowanceEmployee?: RestaurantPayrollEmployeeDto;
    allowanceHistory: RestaurantPayrollAllowanceHistoryDto[] = [];
    runAdjustments: Record<string, PayrollAdjustmentEditor> = {};

    employeeForm!: FormGroup;
    attendanceForm!: FormGroup;
    payrollRunForm!: FormGroup;
    passwordForm!: FormGroup;
    departmentForm!: FormGroup;
    jobRoleForm!: FormGroup;
    allowanceForm!: FormGroup;
    attendanceFrom = this.dateInput(-6);
    attendanceTo = this.dateInput();
    attendanceEmployeeId = '';

    readonly employmentTypes = [
        { value: 0, label: 'Monthly' },
        { value: 1, label: 'Hourly' },
    ];
    readonly attendanceStatuses = [
        { value: 0, label: 'Present' },
        { value: 1, label: 'Late' },
        { value: 2, label: 'Leave' },
        { value: 3, label: 'Absent' },
    ];

    private readonly fb = inject(FormBuilder);
    private readonly payrollApi = inject(RestaurantPayrollApiService);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly nepaliDates = inject(NepaliDatepickerService);

    constructor() {
        super(inject(Injector));
        this.buildForms();
    }

    get canManageStaff(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.Staff');
    }

    get canManageStaffAccess(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.Staff.Access');
    }

    get canUseAttendance(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.Attendance');
    }

    get canManageAttendance(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.Attendance.Manage');
    }

    get canProcessPayroll(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.Process');
    }

    get canApprovePayroll(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.Approve');
    }

    get canViewPayrollReports(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.Reports');
    }

    get canViewRuns(): boolean {
        return this.canProcessPayroll || this.canApprovePayroll || this.canViewPayrollReports;
    }

    get canViewOwnPayslip(): boolean {
        return this.isGranted('Pages.Restaurant.Payroll.OwnPayslip');
    }

    get canLoadEmployees(): boolean {
        return this.canManageStaff || this.canManageAttendance || this.canProcessPayroll;
    }

    get linkedUsersForForm(): RestaurantPayrollUserLookupDto[] {
        const users = [...this.accessOptions.availableUsers];
        const employee = this.currentEditedEmployee;
        if (employee?.userId && !users.some((user) => user.id === employee.userId)) {
            users.unshift({
                id: employee.userId,
                userName: employee.loginUserName || '',
                name: employee.name,
                emailAddress: employee.loginEmailAddress,
                phoneNumber: employee.loginPhoneNumber,
                restaurantRoleName: employee.restaurantRoleName,
                isActive: employee.loginIsActive ?? true,
            });
        }
        return users;
    }

    get filteredEmployees(): RestaurantPayrollEmployeeDto[] {
        const query = this.employeeSearch.trim().toLocaleLowerCase();
        if (!query) {
            return this.employees;
        }
        return this.employees.filter((employee) =>
            [
                employee.name,
                employee.staffCode,
                employee.jobRole,
                employee.department,
                employee.phoneNumber,
                employee.emailAddress,
                employee.loginUserName,
                employee.restaurantRoleName,
            ].some((value) => String(value || '').toLocaleLowerCase().includes(query))
        );
    }

    get activeDepartments() {
        return this.staffMasters.departments.filter((item) => item.isActive);
    }

    get filteredJobRoles() {
        const departmentId = String(this.employeeForm?.get('departmentId')?.value || '');
        const currentRoleId = String(this.employeeForm?.get('jobRoleId')?.value || '');
        return this.staffMasters.jobRoles.filter(
            (item) => item.departmentId === departmentId && (item.isActive || item.id === currentRoleId)
        );
    }

    ngOnInit(): void {
        this.refresh();
    }

    refresh(): void {
        this.loading = true;
        forkJoin({
            dashboard: this.payrollApi.getDashboard(),
            employees: this.canLoadEmployees ? this.payrollApi.getEmployees(this.includeInactive) : of([]),
            attendance: this.canUseAttendance
                ? this.payrollApi.getAttendance(this.attendanceFrom, this.attendanceTo, this.attendanceEmployeeId || undefined)
                : of([]),
            runs: this.canViewRuns ? this.payrollApi.getPayrollRuns() : of([]),
            payslips: this.canViewOwnPayslip ? this.payrollApi.getMyPayslips() : of([]),
            access: this.canManageStaffAccess
                ? this.payrollApi.getStaffAccessOptions()
                : of({ roles: [], availableUsers: [] } as RestaurantPayrollStaffAccessOptionsDto),
            masters: this.canManageStaff
                ? this.payrollApi.getStaffMasters()
                : of({ departments: [], jobRoles: [] } as RestaurantPayrollStaffMastersDto),
        })
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((result) => {
                this.dashboard = result.dashboard || this.emptyDashboard();
                this.employees = result.employees || [];
                this.attendance = result.attendance || [];
                this.payrollRuns = result.runs || [];
                this.payslips = result.payslips || [];
                this.accessOptions = result.access || { roles: [], availableUsers: [] };
                this.staffMasters = result.masters || { departments: [], jobRoles: [] };
                this.initializeAdjustments();
                if (this.selectedRun) {
                    const stillExists = this.payrollRuns.some((run) => run.id === this.selectedRun?.id);
                    if (!stillExists) {
                        this.selectedRun = undefined;
                    }
                }
                this.cdr.markForCheck();
            });
    }

    changeSection(section: PayrollSection): void {
        this.activeSection = section;
        if (section === 'attendance') {
            this.loadAttendance();
        }
    }

    loadEmployees(): void {
        if (!this.canLoadEmployees) {
            return;
        }
        this.loading = true;
        this.payrollApi
            .getEmployees(this.includeInactive)
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((employees) => {
                this.employees = employees || [];
                this.initializeAdjustments();
                this.cdr.markForCheck();
            });
    }

    saveEmployee(): void {
        if (this.employeeForm.invalid) {
            this.employeeForm.markAllAsTouched();
            return;
        }

        const value = this.employeeForm.getRawValue();
        if (value.updateLoginAccess && value.loginMode === 1 && !value.userId) {
            this.message.warn('Choose the existing user account to link.');
            return;
        }
        if (value.updateLoginAccess && value.loginMode === 2 && !this.isEmailOrPhone(value.loginUserName)) {
            this.message.warn('Employee username must be a valid email address or phone number.');
            return;
        }

        const loginIdentifier = String(value.loginUserName || '').trim();
        const loginUsesEmail = loginIdentifier.includes('@');

        const input: SaveRestaurantPayrollEmployeeDto = {
            id: value.id || undefined,
            userId: value.userId ? Number(value.userId) : undefined,
            updateLoginAccess: Boolean(value.updateLoginAccess),
            loginMode: Number(value.loginMode),
            loginUserName: this.clean(loginIdentifier),
            loginEmailAddress: loginUsesEmail ? this.clean(loginIdentifier) : this.clean(value.emailAddress),
            loginPhoneNumber: loginUsesEmail ? this.clean(value.phoneNumber) : this.clean(loginIdentifier),
            restaurantRoleName: this.clean(value.restaurantRoleName),
            loginIsActive: Boolean(value.loginIsActive),
            staffCode: String(value.staffCode || '').trim(),
            name: String(value.name || '').trim(),
            phoneNumber: this.clean(value.phoneNumber),
            emailAddress: this.clean(value.emailAddress),
            dateOfBirth: value.dateOfBirth || undefined,
            dateOfBirthMiti: this.clean(value.dateOfBirthMiti),
            gender: this.clean(value.gender),
            bloodGroup: this.clean(value.bloodGroup),
            maritalStatus: this.clean(value.maritalStatus),
            address: this.clean(value.address),
            citizenshipNumber: this.clean(value.citizenshipNumber),
            emergencyContactName: this.clean(value.emergencyContactName),
            emergencyContactPhone: this.clean(value.emergencyContactPhone),
            departmentId: String(value.departmentId || ''),
            jobRoleId: String(value.jobRoleId || ''),
            employmentType: Number(value.employmentType),
            basicSalary: this.number(value.basicSalary),
            hourlyRate: this.number(value.hourlyRate),
            overtimeRate: this.number(value.overtimeRate),
            fixedAllowance: this.number(value.fixedAllowance),
            fixedDeduction: this.number(value.fixedDeduction),
            serviceChargeWeight: this.number(value.serviceChargeWeight),
            bankName: this.clean(value.bankName),
            bankAccountNumber: this.clean(value.bankAccountNumber),
            panNumber: this.clean(value.panNumber),
            ssfNumber: this.clean(value.ssfNumber),
            joinedOn: value.joinedOn,
            joinedOnMiti: this.clean(value.joinedOnMiti),
            notes: this.clean(value.notes),
            isActive: Boolean(value.isActive),
        };

        this.saving = true;
        this.payrollApi
            .saveEmployee(input)
            .pipe(finalize(() => this.finishSaving()))
            .subscribe((result) => {
                this.notify.success(this.l('SavedSuccessfully'));
                if (result?.temporaryPassword) {
                    this.message.info(
                        `Username: ${result.userName}\nTemporary password: ${result.temporaryPassword}\nThe employee must change it on first sign in.`,
                        'Employee login created'
                    );
                }
                this.employeeEditorOpen = false;
                this.resetEmployee();
                this.refresh();
            });
    }

    addEmployee(): void {
        this.resetEmployee();
        this.employeeEditorOpen = true;
    }

    viewEmployee(employee: RestaurantPayrollEmployeeDto): void {
        this.profileEmployee = employee;
    }

    closeEmployeeProfile(): void {
        this.profileEmployee = undefined;
    }

    closeEmployeeEditor(): void {
        this.employeeEditorOpen = false;
        this.resetEmployee();
    }

    editEmployee(employee: RestaurantPayrollEmployeeDto): void {
        this.profileEmployee = undefined;
        this.currentEditedEmployee = employee;
        this.employeeForm.patchValue({
            id: employee.id,
            userId: employee.userId || null,
            updateLoginAccess: false,
            loginMode: employee.userId ? 1 : 0,
            loginUserName: employee.loginUserName || '',
            loginEmailAddress: employee.loginEmailAddress || '',
            loginPhoneNumber: employee.loginPhoneNumber || '',
            restaurantRoleName: employee.restaurantRoleName || this.accessOptions.roles[0]?.name || '',
            loginIsActive: employee.loginIsActive ?? true,
            staffCode: employee.staffCode,
            name: employee.name,
            phoneNumber: employee.phoneNumber || '',
            emailAddress: employee.emailAddress || '',
            dateOfBirth: employee.dateOfBirth ? this.datePart(employee.dateOfBirth) : '',
            dateOfBirthMiti: employee.dateOfBirthMiti || this.toNepaliDate(employee.dateOfBirth),
            gender: employee.gender || '',
            bloodGroup: employee.bloodGroup || '',
            maritalStatus: employee.maritalStatus || '',
            address: employee.address || '',
            citizenshipNumber: employee.citizenshipNumber || '',
            emergencyContactName: employee.emergencyContactName || '',
            emergencyContactPhone: employee.emergencyContactPhone || '',
            departmentId: employee.departmentId,
            jobRoleId: employee.jobRoleId,
            employmentType: employee.employmentType,
            basicSalary: employee.basicSalary,
            hourlyRate: employee.hourlyRate,
            overtimeRate: employee.overtimeRate,
            fixedAllowance: employee.fixedAllowance,
            fixedDeduction: employee.fixedDeduction,
            serviceChargeWeight: employee.serviceChargeWeight,
            bankName: employee.bankName || '',
            bankAccountNumber: employee.bankAccountNumber || '',
            panNumber: employee.panNumber || '',
            ssfNumber: employee.ssfNumber || '',
            joinedOn: this.datePart(employee.joinedOn),
            joinedOnMiti: employee.joinedOnMiti || this.toNepaliDate(employee.joinedOn),
            notes: employee.notes || '',
            isActive: employee.isActive,
        });
        this.activeSection = 'staff';
        this.employeeEditorOpen = true;
    }

    resetEmployee(): void {
        this.currentEditedEmployee = undefined;
        this.employeeForm.reset(this.employeeDefaults());
    }

    onEmployeeDepartmentChanged(): void {
        const selectedRoleId = String(this.employeeForm.get('jobRoleId')?.value || '');
        if (!this.filteredJobRoles.some((item) => item.id === selectedRoleId)) {
            this.employeeForm.patchValue({ jobRoleId: this.filteredJobRoles[0]?.id || '' });
        }
    }

    openStaffSetup(): void {
        this.departmentForm.reset({ id: null, name: '', description: '', sortOrder: 0, isActive: true });
        this.jobRoleForm.reset({
            id: null,
            departmentId: this.activeDepartments[0]?.id || '',
            name: '',
            description: '',
            sortOrder: 0,
            isActive: true,
        });
        this.staffSetupOpen = true;
    }

    closeStaffSetup(): void {
        this.staffSetupOpen = false;
    }

    editDepartment(item: { id: string; name: string; description?: string; sortOrder: number; isActive: boolean }): void {
        this.departmentForm.reset(item);
    }

    editJobRole(item: { id: string; departmentId: string; name: string; description?: string; sortOrder: number; isActive: boolean }): void {
        this.jobRoleForm.reset(item);
    }

    saveDepartment(): void {
        if (this.departmentForm.invalid) {
            this.departmentForm.markAllAsTouched();
            return;
        }
        const value = this.departmentForm.getRawValue();
        this.saving = true;
        this.payrollApi
            .saveDepartment({
                id: value.id || undefined,
                name: String(value.name || '').trim(),
                description: this.clean(value.description),
                sortOrder: this.number(value.sortOrder),
                isActive: Boolean(value.isActive),
            })
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success('Department saved.');
                this.loadStaffMasters();
                this.departmentForm.reset({ id: null, name: '', description: '', sortOrder: 0, isActive: true });
            });
    }

    saveJobRole(): void {
        if (this.jobRoleForm.invalid) {
            this.jobRoleForm.markAllAsTouched();
            return;
        }
        const value = this.jobRoleForm.getRawValue();
        this.saving = true;
        this.payrollApi
            .saveJobRole({
                id: value.id || undefined,
                departmentId: value.departmentId,
                name: String(value.name || '').trim(),
                description: this.clean(value.description),
                sortOrder: this.number(value.sortOrder),
                isActive: Boolean(value.isActive),
            })
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success('Job role saved.');
                this.loadStaffMasters();
                this.jobRoleForm.reset({
                    id: null,
                    departmentId: value.departmentId,
                    name: '',
                    description: '',
                    sortOrder: 0,
                    isActive: true,
                });
            });
    }

    openAllowanceHistory(employee: RestaurantPayrollEmployeeDto): void {
        this.profileEmployee = undefined;
        this.allowanceEmployee = employee;
        const effectiveFrom = this.dateInput();
        this.allowanceForm.reset({
            amount: employee.fixedAllowance,
            effectiveFrom,
            effectiveFromMiti: this.toNepaliDate(effectiveFrom),
            reason: '',
        });
        this.loadAllowanceHistory();
    }

    closeAllowanceHistory(): void {
        this.allowanceEmployee = undefined;
        this.allowanceHistory = [];
    }

    saveAllowanceRevision(): void {
        if (!this.allowanceEmployee || this.allowanceForm.invalid) {
            this.allowanceForm.markAllAsTouched();
            return;
        }
        const value = this.allowanceForm.getRawValue();
        this.saving = true;
        this.payrollApi
            .addAllowanceRevision({
                employeeId: this.allowanceEmployee.id,
                amount: this.number(value.amount),
                effectiveFrom: value.effectiveFrom,
                effectiveFromMiti: this.clean(value.effectiveFromMiti),
                reason: String(value.reason || '').trim(),
            })
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success('Allowance revision saved.');
                this.closeAllowanceHistory();
                this.refresh();
            });
    }

    setMiti(controlName: string, value: string, form: FormGroup = this.employeeForm): void {
        form.patchValue({ [controlName]: value });
    }

    private loadStaffMasters(): void {
        this.payrollApi.getStaffMasters().subscribe((masters) => {
            this.staffMasters = masters || { departments: [], jobRoles: [] };
            this.cdr.markForCheck();
        });
    }

    private loadAllowanceHistory(): void {
        if (!this.allowanceEmployee) {
            return;
        }
        this.payrollApi.getAllowanceHistory(this.allowanceEmployee.id).subscribe((history) => {
            this.allowanceHistory = history || [];
            this.cdr.markForCheck();
        });
    }

    resetDefaultPassword(employee: RestaurantPayrollEmployeeDto): void {
        this.message.confirm(
            `Reset ${employee.name} to the tenant default employee password and require a change on next sign in?`,
            'Reset employee password',
            (confirmed) => {
                if (!confirmed) {
                    return;
                }
                this.saving = true;
                this.payrollApi
                    .resetEmployeeDefaultPassword(employee.id)
                    .pipe(finalize(() => this.finishSaving()))
                    .subscribe(() => this.notify.success('Employee password reset successfully.'));
            }
        );
    }

    openPasswordDialog(employee: RestaurantPayrollEmployeeDto): void {
        this.profileEmployee = undefined;
        this.passwordEmployee = employee;
        this.passwordForm.reset({ password: '', confirmPassword: '', forceChangeOnNextLogin: true });
    }

    closePasswordDialog(): void {
        this.passwordEmployee = undefined;
        this.passwordForm.reset({ password: '', confirmPassword: '', forceChangeOnNextLogin: true });
    }

    saveEmployeePassword(): void {
        if (!this.passwordEmployee || this.passwordForm.invalid) {
            this.passwordForm.markAllAsTouched();
            return;
        }
        const value = this.passwordForm.getRawValue();
        this.saving = true;
        this.payrollApi
            .setEmployeePassword({
                employeeId: this.passwordEmployee.id,
                password: value.password,
                forceChangeOnNextLogin: Boolean(value.forceChangeOnNextLogin),
            })
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success('Employee password updated successfully.');
                this.closePasswordDialog();
            });
    }

    onExistingUserChanged(): void {
        const userId = Number(this.employeeForm.get('userId')?.value);
        const user = this.linkedUsersForForm.find((item) => item.id === userId);
        if (!user) {
            return;
        }
        this.employeeForm.patchValue({
            name: this.employeeForm.get('name')?.value || user.name,
            phoneNumber: this.employeeForm.get('phoneNumber')?.value || user.phoneNumber || '',
            emailAddress: this.employeeForm.get('emailAddress')?.value || user.emailAddress || '',
            loginUserName: user.userName,
            loginEmailAddress: user.emailAddress || '',
            loginPhoneNumber: user.phoneNumber || '',
            restaurantRoleName: user.restaurantRoleName || this.employeeForm.get('restaurantRoleName')?.value,
            loginIsActive: user.isActive,
        });
    }

    loadAttendance(): void {
        if (!this.canUseAttendance) {
            return;
        }
        if (!this.attendanceFrom || !this.attendanceTo) {
            this.message.warn('Choose both attendance dates.');
            return;
        }
        this.loading = true;
        this.payrollApi
            .getAttendance(this.attendanceFrom, this.attendanceTo, this.attendanceEmployeeId || undefined)
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((rows) => {
                this.attendance = rows || [];
                this.cdr.markForCheck();
            });
    }

    clockIn(): void {
        this.saving = true;
        this.payrollApi
            .clockIn({ breakMinutes: 0 })
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success('Clocked in successfully.');
                this.refresh();
            });
    }

    clockOut(): void {
        this.saving = true;
        this.payrollApi
            .clockOut({ breakMinutes: 0 })
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success('Clocked out successfully.');
                this.refresh();
            });
    }

    saveAttendance(): void {
        if (this.attendanceForm.invalid) {
            this.attendanceForm.markAllAsTouched();
            return;
        }
        const value = this.attendanceForm.getRawValue();
        const input: SaveRestaurantPayrollAttendanceDto = {
            id: value.id || undefined,
            employeeId: value.employeeId,
            workDate: value.workDate,
            clockIn: this.toServerDateTime(value.clockIn),
            clockOut: this.toServerDateTime(value.clockOut),
            breakMinutes: this.number(value.breakMinutes),
            regularHours: this.optionalNumber(value.regularHours),
            overtimeHours: this.optionalNumber(value.overtimeHours),
            status: Number(value.status),
            shiftName: this.clean(value.shiftName),
            notes: this.clean(value.notes),
        };
        this.saving = true;
        this.payrollApi
            .saveAttendance(input)
            .pipe(finalize(() => this.finishSaving()))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetAttendance();
                this.loadAttendance();
                this.payrollApi.getDashboard().subscribe((dashboard) => {
                    this.dashboard = dashboard;
                    this.cdr.markForCheck();
                });
            });
    }

    editAttendance(row: RestaurantPayrollAttendanceDto): void {
        this.attendanceForm.patchValue({
            id: row.id,
            employeeId: row.employeeId,
            workDate: this.datePart(row.workDate),
            clockIn: this.dateTimeInput(row.clockIn),
            clockOut: this.dateTimeInput(row.clockOut),
            breakMinutes: row.breakMinutes,
            regularHours: row.regularHours,
            overtimeHours: row.overtimeHours,
            status: row.status,
            shiftName: row.shiftName || '',
            notes: row.notes || '',
        });
        this.activeSection = 'attendance';
    }

    resetAttendance(): void {
        this.attendanceForm.reset(this.attendanceDefaults());
    }

    generatePayrollRun(): void {
        if (this.payrollRunForm.invalid) {
            this.payrollRunForm.markAllAsTouched();
            return;
        }
        const value = this.payrollRunForm.getRawValue();
        const adjustments: RestaurantPayrollAdjustmentDto[] = Object.entries(this.runAdjustments)
            .filter(([, item]) => this.hasAdjustment(item))
            .map(([employeeId, item]) => ({
                employeeId,
                additionalAllowance: this.number(item.additionalAllowance),
                otherDeduction: this.number(item.otherDeduction),
                advanceRecovery: this.number(item.advanceRecovery),
                taxDeduction: this.number(item.taxDeduction),
                notes: this.clean(item.notes),
            }));
        const input: GenerateRestaurantPayrollRunDto = {
            periodStart: value.periodStart,
            periodEnd: value.periodEnd,
            tipsPool: this.number(value.tipsPool),
            serviceChargePool: this.number(value.serviceChargePool),
            notes: this.clean(value.notes),
            adjustments,
        };
        this.saving = true;
        this.payrollApi
            .generatePayrollRun(input)
            .pipe(finalize(() => this.finishSaving()))
            .subscribe((id) => {
                this.notify.success('Payroll draft generated.');
                this.loadPayrollRun(id);
                this.refresh();
            });
    }

    loadPayrollRun(id: string): void {
        this.loading = true;
        this.payrollApi
            .getPayrollRun(id)
            .pipe(finalize(() => this.finishLoading()))
            .subscribe((run) => {
                this.selectedRun = run;
                this.activeSection = 'runs';
                this.cdr.markForCheck();
            });
    }

    approveRun(run: RestaurantPayrollRunDto): void {
        this.message.confirm(`Approve ${run.runNumber}? The payroll amounts will be locked.`, 'Approve payroll', (confirmed) => {
            if (!confirmed) {
                return;
            }
            this.saving = true;
            this.payrollApi
                .approvePayrollRun(run.id)
                .pipe(finalize(() => this.finishSaving()))
                .subscribe(() => {
                    this.notify.success('Payroll approved.');
                    this.selectedRun = undefined;
                    this.refresh();
                });
        });
    }

    markRunPaid(run: RestaurantPayrollRunDto): void {
        this.message.confirm(`Mark ${run.runNumber} as paid?`, 'Complete payroll', (confirmed) => {
            if (!confirmed) {
                return;
            }
            this.saving = true;
            this.payrollApi
                .markPayrollRunPaid(run.id)
                .pipe(finalize(() => this.finishSaving()))
                .subscribe(() => {
                    this.notify.success('Payroll marked as paid.');
                    this.selectedRun = undefined;
                    this.refresh();
                });
        });
    }

    deleteDraft(run: RestaurantPayrollRunDto): void {
        this.message.confirm(`Delete draft ${run.runNumber}?`, this.l('AreYouSure'), (confirmed) => {
            if (!confirmed) {
                return;
            }
            this.saving = true;
            this.payrollApi
                .deleteDraftPayrollRun(run.id)
                .pipe(finalize(() => this.finishSaving()))
                .subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.selectedRun = undefined;
                    this.refresh();
                });
        });
    }

    employmentTypeText(value: number): string {
        return value === 1 ? 'Hourly' : 'Monthly';
    }

    attendanceStatusText(value: number): string {
        return this.attendanceStatuses.find((item) => item.value === value)?.label || 'Unknown';
    }

    runStatusText(value: number | undefined): string {
        return value === 2 ? 'Paid' : value === 1 ? 'Approved' : 'Draft';
    }

    departmentName(departmentId: string): string {
        return this.staffMasters.departments.find((item) => item.id === departmentId)?.name || 'Unknown department';
    }

    money(value: number | null | undefined): string {
        return new Intl.NumberFormat('en-NP', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value || 0);
    }

    date(value: string | null | undefined): string {
        if (!value) {
            return '-';
        }
        const parsed = new Date(value);
        return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleDateString('en-NP');
    }

    dateBs(value: string | null | undefined, savedMiti?: string): string {
        if (savedMiti) {
            return savedMiti.replaceAll('/', '-');
        }
        const miti = this.toNepaliDate(value);
        return miti ? miti.replaceAll('/', '-') : '-';
    }

    dateTime(value: string | null | undefined): string {
        if (!value) {
            return '-';
        }
        const parsed = new Date(value);
        return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString('en-NP', { dateStyle: 'medium', timeStyle: 'short' });
    }

    trackById(_index: number, item: { id: string }): string {
        return item.id;
    }

    initials(name: string | null | undefined): string {
        return String(name || '?')
            .trim()
            .split(/\s+/)
            .slice(0, 2)
            .map((part) => part.charAt(0).toUpperCase())
            .join('') || '?';
    }

    private buildForms(): void {
        this.employeeForm = this.fb.group({
            id: [null],
            userId: [null],
            updateLoginAccess: [false],
            loginMode: [0],
            loginUserName: [''],
            loginEmailAddress: ['', Validators.email],
            loginPhoneNumber: [''],
            restaurantRoleName: [''],
            loginIsActive: [true],
            staffCode: ['', [Validators.required, Validators.maxLength(30)]],
            name: ['', [Validators.required, Validators.maxLength(150)]],
            phoneNumber: ['', Validators.maxLength(32)],
            emailAddress: ['', [Validators.email, Validators.maxLength(256)]],
            dateOfBirth: [''],
            dateOfBirthMiti: [''],
            gender: ['', Validators.maxLength(20)],
            bloodGroup: ['', Validators.maxLength(10)],
            maritalStatus: ['', Validators.maxLength(20)],
            address: ['', Validators.maxLength(300)],
            citizenshipNumber: ['', Validators.maxLength(80)],
            emergencyContactName: ['', Validators.maxLength(150)],
            emergencyContactPhone: ['', Validators.maxLength(32)],
            departmentId: ['', Validators.required],
            jobRoleId: ['', Validators.required],
            employmentType: [0, Validators.required],
            basicSalary: [0, [Validators.required, Validators.min(0)]],
            hourlyRate: [0, [Validators.required, Validators.min(0)]],
            overtimeRate: [0, [Validators.required, Validators.min(0)]],
            fixedAllowance: [0, [Validators.required, Validators.min(0)]],
            fixedDeduction: [0, [Validators.required, Validators.min(0)]],
            serviceChargeWeight: [1, [Validators.required, Validators.min(0)]],
            bankName: ['', Validators.maxLength(100)],
            bankAccountNumber: ['', Validators.maxLength(80)],
            panNumber: ['', Validators.maxLength(80)],
            ssfNumber: ['', Validators.maxLength(80)],
            joinedOn: [this.dateInput(), Validators.required],
            joinedOnMiti: [this.toNepaliDate(this.dateInput())],
            notes: ['', Validators.maxLength(500)],
            isActive: [true],
        });
        this.attendanceForm = this.fb.group({
            id: [null],
            employeeId: ['', Validators.required],
            workDate: [this.dateInput(), Validators.required],
            clockIn: [''],
            clockOut: [''],
            breakMinutes: [0, [Validators.required, Validators.min(0), Validators.max(1440)]],
            regularHours: [null, [Validators.min(0), Validators.max(24)]],
            overtimeHours: [null, [Validators.min(0), Validators.max(24)]],
            status: [0, Validators.required],
            shiftName: ['', Validators.maxLength(80)],
            notes: ['', Validators.maxLength(500)],
        });
        const today = new Date();
        this.payrollRunForm = this.fb.group({
            periodStart: [this.monthStart(today), Validators.required],
            periodEnd: [this.dateInput(), Validators.required],
            tipsPool: [0, [Validators.required, Validators.min(0)]],
            serviceChargePool: [0, [Validators.required, Validators.min(0)]],
            notes: ['', Validators.maxLength(500)],
        });
        this.passwordForm = this.fb.group(
            {
                password: ['', [Validators.required, Validators.minLength(6)]],
                confirmPassword: ['', Validators.required],
                forceChangeOnNextLogin: [true],
            },
            {
                validators: (form) =>
                    form.get('password')?.value === form.get('confirmPassword')?.value
                        ? null
                        : { passwordMismatch: true },
            }
        );
        this.departmentForm = this.fb.group({
            id: [null],
            name: ['', [Validators.required, Validators.maxLength(80)]],
            description: ['', Validators.maxLength(300)],
            sortOrder: [0, Validators.min(0)],
            isActive: [true],
        });
        this.jobRoleForm = this.fb.group({
            id: [null],
            departmentId: ['', Validators.required],
            name: ['', [Validators.required, Validators.maxLength(80)]],
            description: ['', Validators.maxLength(300)],
            sortOrder: [0, Validators.min(0)],
            isActive: [true],
        });
        this.allowanceForm = this.fb.group({
            amount: [0, [Validators.required, Validators.min(0)]],
            effectiveFrom: [this.dateInput(), Validators.required],
            effectiveFromMiti: [this.toNepaliDate(this.dateInput())],
            reason: ['', [Validators.required, Validators.maxLength(300)]],
        });
    }

    private employeeDefaults(): Record<string, unknown> {
        return {
            id: null,
            userId: null,
            updateLoginAccess: false,
            loginMode: 0,
            loginUserName: '',
            loginEmailAddress: '',
            loginPhoneNumber: '',
            restaurantRoleName: this.accessOptions.roles[0]?.name || '',
            loginIsActive: true,
            staffCode: '',
            name: '',
            phoneNumber: '',
            emailAddress: '',
            dateOfBirth: '',
            dateOfBirthMiti: '',
            gender: '',
            bloodGroup: '',
            maritalStatus: '',
            address: '',
            citizenshipNumber: '',
            emergencyContactName: '',
            emergencyContactPhone: '',
            departmentId: this.activeDepartments[0]?.id || '',
            jobRoleId:
                this.staffMasters.jobRoles.find(
                    (item) => item.departmentId === this.activeDepartments[0]?.id && item.isActive
                )?.id || '',
            employmentType: 0,
            basicSalary: 0,
            hourlyRate: 0,
            overtimeRate: 0,
            fixedAllowance: 0,
            fixedDeduction: 0,
            serviceChargeWeight: 1,
            bankName: '',
            bankAccountNumber: '',
            panNumber: '',
            ssfNumber: '',
            joinedOn: this.dateInput(),
            joinedOnMiti: this.toNepaliDate(this.dateInput()),
            notes: '',
            isActive: true,
        };
    }

    private attendanceDefaults(): Record<string, unknown> {
        return {
            id: null,
            employeeId: this.employees[0]?.id || '',
            workDate: this.dateInput(),
            clockIn: '',
            clockOut: '',
            breakMinutes: 0,
            regularHours: null,
            overtimeHours: null,
            status: 0,
            shiftName: '',
            notes: '',
        };
    }

    private initializeAdjustments(): void {
        const current = this.runAdjustments;
        this.runAdjustments = {};
        for (const employee of this.employees.filter((item) => item.isActive)) {
            this.runAdjustments[employee.id] = current[employee.id] || {
                additionalAllowance: 0,
                otherDeduction: 0,
                advanceRecovery: 0,
                taxDeduction: 0,
                notes: '',
            };
        }
        if (!this.attendanceForm.get('employeeId')?.value && this.employees.length) {
            this.attendanceForm.patchValue({ employeeId: this.employees[0].id });
        }
    }

    private hasAdjustment(item: PayrollAdjustmentEditor): boolean {
        return Boolean(
            this.number(item.additionalAllowance) ||
                this.number(item.otherDeduction) ||
                this.number(item.advanceRecovery) ||
                this.number(item.taxDeduction) ||
                String(item.notes || '').trim()
        );
    }

    private finishLoading(): void {
        this.loading = false;
        this.cdr.markForCheck();
    }

    private finishSaving(): void {
        this.saving = false;
        this.cdr.markForCheck();
    }

    private emptyDashboard(): RestaurantPayrollDashboardDto {
        return {
            activeEmployeeCount: 0,
            presentToday: 0,
            openClockIns: 0,
            currentMonthGross: 0,
            currentMonthNet: 0,
            recentRuns: [],
            myRecentPayslips: [],
        };
    }

    private dateInput(dayOffset = 0): string {
        const date = new Date();
        date.setDate(date.getDate() + dayOffset);
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    }

    private monthStart(date: Date): string {
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-01`;
    }

    private datePart(value: string | undefined): string {
        return value ? value.slice(0, 10) : this.dateInput();
    }

    private dateTimeInput(value: string | undefined): string {
        if (!value) {
            return '';
        }
        const parsed = new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return value.slice(0, 16);
        }
        const local = new Date(parsed.getTime() - parsed.getTimezoneOffset() * 60000);
        return local.toISOString().slice(0, 16);
    }

    private toServerDateTime(value: string | null | undefined): string | undefined {
        if (!value) {
            return undefined;
        }
        const parsed = new Date(value);
        return Number.isNaN(parsed.getTime()) ? value : parsed.toISOString();
    }

    private number(value: unknown): number {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    private optionalNumber(value: unknown): number | undefined {
        return value === null || value === undefined || value === '' ? undefined : this.number(value);
    }

    private toNepaliDate(value: string | null | undefined): string {
        if (!value) {
            return '';
        }
        try {
            return this.nepaliDates.ADToBS(this.datePart(value), 'yyyy/mm/dd');
        } catch {
            return '';
        }
    }

    private isEmailOrPhone(value: unknown): boolean {
        const identifier = String(value || '').trim();
        if (/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(identifier)) {
            return true;
        }
        const digits = identifier.replace(/\D/g, '');
        return digits.length >= 7 && digits.length <= 15 && /^\+?[\d\s()-]+$/.test(identifier);
    }

    private clean(value: unknown): string | undefined {
        const cleaned = String(value ?? '').trim();
        return cleaned || undefined;
    }
}
