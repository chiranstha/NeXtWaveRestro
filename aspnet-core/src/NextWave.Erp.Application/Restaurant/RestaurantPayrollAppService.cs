using Abp;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Runtime.Caching;
using Abp.UI;
using Abp.Zero.Configuration;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Configuration;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant;

[AbpAuthorize(AppPermissions.PagesRestaurantPayroll)]
public class RestaurantPayrollAppService(
    IRepository<RestaurantPayrollDepartment, Guid> departmentRepository,
    IRepository<RestaurantPayrollJobRole, Guid> jobRoleRepository,
    IRepository<RestaurantPayrollEmployee, Guid> employeeRepository,
    IRepository<RestaurantPayrollAllowanceHistory, Guid> allowanceHistoryRepository,
    IRepository<RestaurantPayrollAttendance, Guid> attendanceRepository,
    IRepository<RestaurantPayrollRun, Guid> payrollRunRepository,
    IRepository<RestaurantPayrollLine, Guid> payrollLineRepository,
    IRepository<User, long> userRepository,
    IRepository<UserRole, long> userRoleRepository,
    IRepository<Role> roleRepository,
    UserManager userManager,
    IUserPolicy userPolicy,
    ICacheManager cacheManager) : ErpAppServiceBase
{
    private static readonly string[] RestaurantOperatingRoleNames =
    {
        StaticRoleNames.Tenants.RestaurantManager,
        StaticRoleNames.Tenants.RestaurantCashier,
        StaticRoleNames.Tenants.RestaurantWaiter,
        StaticRoleNames.Tenants.RestaurantKitchen,
        StaticRoleNames.Tenants.RestaurantInventory,
        StaticRoleNames.Tenants.RestaurantPayroll
    };

    public async Task<RestaurantPayrollDashboardDto> GetDashboard()
    {
        var tenantId = AbpSession.GetTenantId();
        var today = DateTime.Today;
        var canViewAll = await HasAnyPermission(
            AppPermissions.PagesRestaurantPayrollReports,
            AppPermissions.PagesRestaurantPayrollProcess,
            AppPermissions.PagesRestaurantPayrollApprove,
            AppPermissions.PagesRestaurantPayrollStaff);
        var myEmployee = await GetCurrentEmployeeOrNull();

        var output = new RestaurantPayrollDashboardDto
        {
            ActiveEmployeeCount = canViewAll
                ? await employeeRepository.CountAsync(x => x.TenantId == tenantId && x.IsActive)
                : myEmployee == null ? 0 : 1
        };

        var attendanceQuery = attendanceRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.EmployeeFk)
            .Where(x => x.TenantId == tenantId && x.WorkDate == today);
        if (!canViewAll)
        {
            if (myEmployee == null)
                attendanceQuery = attendanceQuery.Where(x => false);
            else
                attendanceQuery = attendanceQuery.Where(x => x.EmployeeId == myEmployee.Id);
        }

        var todayAttendance = await attendanceQuery.ToListAsync();
        output.PresentToday = todayAttendance.Count(x => x.Status == RestaurantAttendanceStatus.Present || x.Status == RestaurantAttendanceStatus.Late);
        output.OpenClockIns = todayAttendance.Count(x => x.ClockIn.HasValue && !x.ClockOut.HasValue);
        output.MyProfile = myEmployee == null
            ? null
            : MapEmployee(myEmployee, await GetAllowanceAt(myEmployee.Id, today));
        output.MyTodayAttendance = myEmployee == null
            ? null
            : todayAttendance.Where(x => x.EmployeeId == myEmployee.Id).Select(MapAttendance).FirstOrDefault();

        if (canViewAll)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var monthRuns = await payrollRunRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.PeriodEnd >= monthStart && x.PeriodStart <= monthEnd)
                .ToListAsync();
            output.CurrentMonthGross = RoundMoney(monthRuns.Sum(x => x.TotalGross));
            output.CurrentMonthNet = RoundMoney(monthRuns.Sum(x => x.TotalNet));
            var recent = await payrollRunRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.PeriodEnd)
                .ThenByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync();
            output.RecentRuns = await MapRuns(recent);
        }

        if (myEmployee != null && await PermissionChecker.IsGrantedAsync(AppPermissions.PagesRestaurantPayrollOwnPayslip))
        {
            var payslips = await payrollLineRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.PayrollRunFk)
                .Where(x => x.TenantId == tenantId && x.EmployeeId == myEmployee.Id &&
                            x.PayrollRunFk.Status != RestaurantPayrollRunStatus.Draft)
                .OrderByDescending(x => x.PayrollRunFk.PeriodEnd)
                .Take(6)
                .ToListAsync();
            output.MyRecentPayslips = payslips.Select(MapLine).ToList();
        }

        if (output.MyProfile != null)
            await EnrichEmployeeAccess(new List<RestaurantPayrollEmployeeDto> { output.MyProfile });

        return output;
    }

    public async Task<List<RestaurantPayrollEmployeeDto>> GetEmployees(bool includeInactive = false)
    {
        var tenantId = AbpSession.GetTenantId();
        var canManage = await PermissionChecker.IsGrantedAsync(AppPermissions.PagesRestaurantPayrollStaff);
        var query = employeeRepository.GetAll().AsNoTracking().Where(x => x.TenantId == tenantId);
        if (!includeInactive)
            query = query.Where(x => x.IsActive);
        if (!canManage)
        {
            var userId = AbpSession.UserId;
            query = userId.HasValue ? query.Where(x => x.UserId == userId.Value) : query.Where(x => false);
        }

        var employeeEntities = await query
            .Include(x => x.DepartmentFk)
            .Include(x => x.JobRoleFk)
            .OrderBy(x => x.DepartmentFk.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();
        var allowanceByEmployee = await GetAllowancesAt(employeeEntities.Select(x => x.Id).ToList(), DateTime.Today);
        var employees = employeeEntities
            .Select(x => MapEmployee(x, allowanceByEmployee.GetValueOrDefault(x.Id)))
            .ToList();
        await EnrichEmployeeAccess(employees);
        return employees;
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaff)]
    public async Task<RestaurantPayrollStaffMastersDto> GetStaffMasters()
    {
        var tenantId = AbpSession.GetTenantId();
        var departments = await departmentRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new RestaurantPayrollDepartmentDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            }).ToListAsync();
        var jobRoles = await jobRoleRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new RestaurantPayrollJobRoleDto
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                Name = x.Name,
                Description = x.Description,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            }).ToListAsync();
        return new RestaurantPayrollStaffMastersDto { Departments = departments, JobRoles = jobRoles };
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaff)]
    public async Task<Guid> SaveDepartment(SaveRestaurantPayrollDepartmentDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new UserFriendlyException("Department name is required.");
        var duplicate = await departmentRepository.GetAll().AsNoTracking().AnyAsync(x =>
            x.TenantId == tenantId && x.Name == name && (!input.Id.HasValue || x.Id != input.Id.Value));
        if (duplicate)
            throw new UserFriendlyException($"Department {name} already exists.");

        var department = input.Id.HasValue
            ? await departmentRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.TenantId == tenantId)
                ?? throw new UserFriendlyException("Department was not found.")
            : new RestaurantPayrollDepartment
            {
                Id = SequentialGuidGenerator.Instance.Create(),
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };
        department.Name = name;
        department.Description = input.Description?.Trim();
        department.SortOrder = input.SortOrder;
        department.IsActive = input.IsActive;
        if (input.Id.HasValue)
            await departmentRepository.UpdateAsync(department);
        else
            await departmentRepository.InsertAsync(department);
        return department.Id;
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaff)]
    public async Task<Guid> SaveJobRole(SaveRestaurantPayrollJobRoleDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        var name = input.Name?.Trim();
        if (input.DepartmentId == Guid.Empty || string.IsNullOrWhiteSpace(name))
            throw new UserFriendlyException("Department and job role name are required.");
        if (!await departmentRepository.GetAll().AsNoTracking().AnyAsync(x =>
                x.Id == input.DepartmentId && x.TenantId == tenantId && x.IsActive))
            throw new UserFriendlyException("Choose an active department.");
        var duplicate = await jobRoleRepository.GetAll().AsNoTracking().AnyAsync(x =>
            x.TenantId == tenantId && x.DepartmentId == input.DepartmentId && x.Name == name &&
            (!input.Id.HasValue || x.Id != input.Id.Value));
        if (duplicate)
            throw new UserFriendlyException($"Job role {name} already exists in that department.");

        var jobRole = input.Id.HasValue
            ? await jobRoleRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.TenantId == tenantId)
                ?? throw new UserFriendlyException("Job role was not found.")
            : new RestaurantPayrollJobRole
            {
                Id = SequentialGuidGenerator.Instance.Create(),
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };
        jobRole.DepartmentId = input.DepartmentId;
        jobRole.Name = name;
        jobRole.Description = input.Description?.Trim();
        jobRole.SortOrder = input.SortOrder;
        jobRole.IsActive = input.IsActive;
        if (input.Id.HasValue)
            await jobRoleRepository.UpdateAsync(jobRole);
        else
            await jobRoleRepository.InsertAsync(jobRole);
        return jobRole.Id;
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaffAccess)]
    public async Task<List<RestaurantPayrollUserLookupDto>> GetAvailableUsers()
        => (await GetStaffAccessOptions()).AvailableUsers;

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaffAccess)]
    public async Task<RestaurantPayrollStaffAccessOptionsDto> GetStaffAccessOptions()
    {
        var tenantId = AbpSession.GetTenantId();
        var assignedIds = await employeeRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId.HasValue)
            .Select(x => x.UserId.Value)
            .ToListAsync();

        var protectedUserIds = await (from userRole in userRoleRepository.GetAll().AsNoTracking()
                                      join role in roleRepository.GetAll().AsNoTracking() on userRole.RoleId equals role.Id
                                      where role.Name == StaticRoleNames.Tenants.Admin
                                      select userRole.UserId).Distinct().ToListAsync();

        var users = await userRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && x.UserName != AbpUserBase.AdminUserName &&
                        !assignedIds.Contains(x.Id) && !protectedUserIds.Contains(x.Id))
            .OrderBy(x => x.Name).ThenBy(x => x.Surname)
            .Select(x => new RestaurantPayrollUserLookupDto
            {
                Id = x.Id,
                UserName = x.UserName,
                Name = (x.Name + " " + x.Surname).Trim(),
                EmailAddress = x.EmailAddress,
                PhoneNumber = x.PhoneNumber,
                IsActive = x.IsActive
            }).ToListAsync();

        users = users.Where(x => new EmailAddressAttribute().IsValid(x.UserName) ||
                                 NormalizePhoneIdentifier(x.UserName) != null).ToList();

        await EnrichUserRoles(users);
        var roles = await roleRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId && RestaurantOperatingRoleNames.Contains(x.Name))
            .OrderBy(x => x.DisplayName)
            .Select(x => new RestaurantRoleOptionDto { Name = x.Name, DisplayName = x.DisplayName })
            .ToListAsync();

        return new RestaurantPayrollStaffAccessOptionsDto
        {
            Roles = roles,
            AvailableUsers = users
        };
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaff)]
    public async Task<RestaurantPayrollEmployeeSaveResultDto> CreateOrEditEmployee(CreateOrEditRestaurantPayrollEmployeeDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        var code = input.StaffCode?.Trim();
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new UserFriendlyException("Staff code and name are required.");
        if (input.EmploymentType == RestaurantEmploymentType.Monthly && input.BasicSalary <= 0)
            throw new UserFriendlyException("Monthly employees require a basic salary.");
        if (input.EmploymentType == RestaurantEmploymentType.Hourly && input.HourlyRate <= 0)
            throw new UserFriendlyException("Hourly employees require an hourly rate.");
        if (input.DateOfBirth.HasValue && input.DateOfBirth.Value.Date > DateTime.Today)
            throw new UserFriendlyException("Date of birth cannot be in the future.");

        var department = await departmentRepository.FirstOrDefaultAsync(x =>
            x.Id == input.DepartmentId && x.TenantId == tenantId && x.IsActive)
            ?? throw new UserFriendlyException("Choose an active department.");
        var jobRole = await jobRoleRepository.FirstOrDefaultAsync(x =>
            x.Id == input.JobRoleId && x.TenantId == tenantId && x.DepartmentId == department.Id && x.IsActive)
            ?? throw new UserFriendlyException("Choose an active job role from the selected department.");

        var duplicateCode = await employeeRepository.GetAll().AsNoTracking().AnyAsync(x =>
            x.TenantId == tenantId && x.StaffCode == code && (!input.Id.HasValue || x.Id != input.Id.Value));
        if (duplicateCode)
            throw new UserFriendlyException($"Staff code {code} is already in use.");

        RestaurantPayrollEmployee employee;
        var isNewEmployee = !input.Id.HasValue || input.Id.Value == Guid.Empty;
        if (input.Id.HasValue && input.Id.Value != Guid.Empty)
        {
            employee = await employeeRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.TenantId == tenantId)
                ?? throw new UserFriendlyException("Payroll employee was not found.");
        }
        else
        {
            employee = new RestaurantPayrollEmployee
            {
                Id = SequentialGuidGenerator.Instance.Create(),
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };
        }

        var accessResult = new RestaurantPayrollEmployeeSaveResultDto();
        if (input.UpdateLoginAccess)
        {
            if (!await PermissionChecker.IsGrantedAsync(AppPermissions.PagesRestaurantPayrollStaffAccess))
                throw new AbpAuthorizationException("You are not allowed to manage employee login access.");
            accessResult = await ApplyEmployeeLoginAccess(employee, input, name, tenantId);
        }

        employee.StaffCode = code;
        employee.Name = name;
        employee.PhoneNumber = input.PhoneNumber?.Trim();
        employee.EmailAddress = input.EmailAddress?.Trim();
        employee.DateOfBirth = input.DateOfBirth?.Date;
        employee.DateOfBirthMiti = NormalizeMiti(input.DateOfBirthMiti);
        employee.Gender = input.Gender?.Trim();
        employee.BloodGroup = input.BloodGroup?.Trim();
        employee.MaritalStatus = input.MaritalStatus?.Trim();
        employee.Address = input.Address?.Trim();
        employee.CitizenshipNumber = input.CitizenshipNumber?.Trim();
        employee.EmergencyContactName = input.EmergencyContactName?.Trim();
        employee.EmergencyContactPhone = input.EmergencyContactPhone?.Trim();
        employee.DepartmentId = department.Id;
        employee.JobRoleId = jobRole.Id;
        employee.EmploymentType = input.EmploymentType;
        employee.BasicSalary = RoundMoney(input.BasicSalary);
        employee.HourlyRate = RoundMoney(input.HourlyRate);
        employee.OvertimeRate = RoundMoney(input.OvertimeRate);
        employee.FixedDeduction = RoundMoney(input.FixedDeduction);
        employee.ServiceChargeWeight = input.ServiceChargeWeight < 0 ? 0 : input.ServiceChargeWeight;
        employee.BankName = input.BankName?.Trim();
        employee.BankAccountNumber = input.BankAccountNumber?.Trim();
        employee.PanNumber = input.PanNumber?.Trim();
        employee.SsfNumber = input.SsfNumber?.Trim();
        employee.JoinedOn = input.JoinedOn == default ? DateTime.Today : input.JoinedOn.Date;
        employee.JoinedOnMiti = NormalizeMiti(input.JoinedOnMiti);
        employee.Notes = input.Notes?.Trim();
        employee.IsActive = input.IsActive;

        if (!employee.IsActive && employee.UserId.HasValue)
        {
            var linkedUser = await GetTenantUser(employee.UserId.Value, tenantId);
            if (employee.LoginManagedByRestaurant)
                await SetLinkedUserActive(linkedUser, false);
            else
                await RemoveRestaurantRoles(linkedUser, false);
        }

        if (input.Id.HasValue && input.Id.Value != Guid.Empty)
            await employeeRepository.UpdateAsync(employee);
        else
            await employeeRepository.InsertAsync(employee);

        if (isNewEmployee || !await allowanceHistoryRepository.GetAll().AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId && x.EmployeeId == employee.Id))
        {
            await allowanceHistoryRepository.InsertAsync(new RestaurantPayrollAllowanceHistory
            {
                Id = SequentialGuidGenerator.Instance.Create(),
                TenantId = tenantId,
                EmployeeId = employee.Id,
                Amount = RoundMoney(input.FixedAllowance),
                EffectiveFrom = employee.JoinedOn,
                EffectiveFromMiti = employee.JoinedOnMiti,
                Reason = "Starting allowance",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = AbpSession.UserId
            });
        }

        accessResult.EmployeeId = employee.Id;
        accessResult.UserId = employee.UserId;
        if (employee.UserId.HasValue && string.IsNullOrWhiteSpace(accessResult.UserName))
            accessResult.UserName = (await GetTenantUser(employee.UserId.Value, tenantId)).UserName;
        return accessResult;
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaff)]
    public async Task<List<RestaurantPayrollAllowanceHistoryDto>> GetAllowanceHistory(Guid employeeId)
    {
        var tenantId = AbpSession.GetTenantId();
        if (!await employeeRepository.GetAll().AsNoTracking().AnyAsync(x => x.Id == employeeId && x.TenantId == tenantId))
            throw new UserFriendlyException("Payroll employee was not found.");
        return await allowanceHistoryRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EmployeeId == employeeId)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new RestaurantPayrollAllowanceHistoryDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                Amount = x.Amount,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveFromMiti = x.EffectiveFromMiti,
                Reason = x.Reason,
                CreatedAt = x.CreatedAt,
                CreatedByUserId = x.CreatedByUserId
            }).ToListAsync();
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaff)]
    public async Task<Guid> AddAllowanceRevision(AddRestaurantPayrollAllowanceRevisionDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        var employee = await employeeRepository.FirstOrDefaultAsync(x =>
            x.Id == input.EmployeeId && x.TenantId == tenantId)
            ?? throw new UserFriendlyException("Payroll employee was not found.");
        var effectiveFrom = input.EffectiveFrom.Date;
        if (effectiveFrom < employee.JoinedOn.Date)
            throw new UserFriendlyException("Allowance effective date cannot be before the employee joined.");
        var closedThrough = await payrollRunRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != RestaurantPayrollRunStatus.Draft)
            .MaxAsync(x => (DateTime?)x.PeriodEnd);
        if (closedThrough.HasValue && effectiveFrom <= closedThrough.Value.Date)
            throw new UserFriendlyException($"Allowance cannot be backdated into payroll closed through {closedThrough.Value:yyyy-MM-dd}.");
        if (await allowanceHistoryRepository.GetAll().AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId && x.EmployeeId == input.EmployeeId && x.EffectiveFrom == effectiveFrom))
            throw new UserFriendlyException("An allowance revision already exists for that effective date.");

        var revision = new RestaurantPayrollAllowanceHistory
        {
            Id = SequentialGuidGenerator.Instance.Create(),
            TenantId = tenantId,
            EmployeeId = input.EmployeeId,
            Amount = RoundMoney(input.Amount),
            EffectiveFrom = effectiveFrom,
            EffectiveFromMiti = NormalizeMiti(input.EffectiveFromMiti),
            Reason = input.Reason?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = AbpSession.UserId
        };
        if (string.IsNullOrWhiteSpace(revision.Reason))
            throw new UserFriendlyException("Enter a reason for the allowance change.");
        await allowanceHistoryRepository.InsertAsync(revision);
        return revision.Id;
    }

    [DisableAuditing]
    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaffAccess)]
    public async Task ResetEmployeeDefaultPassword(EntityDto<Guid> input)
    {
        var employee = await GetManagedEmployeeForPasswordChange(input.Id);
        await SetManagedEmployeePassword(
            employee,
            await GetEmployeeDefaultPassword(),
            true);
    }

    [DisableAuditing]
    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollStaffAccess)]
    public async Task SetEmployeePassword(ChangeRestaurantPayrollEmployeePasswordDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Password))
            throw new UserFriendlyException("Password is required.");

        var employee = await GetManagedEmployeeForPasswordChange(input.EmployeeId);
        await SetManagedEmployeePassword(
            employee,
            input.Password,
            input.ForceChangeOnNextLogin);
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollAttendance)]
    public async Task<List<RestaurantPayrollAttendanceDto>> GetAttendance(DateTime from, DateTime to, Guid? employeeId = null)
    {
        var tenantId = AbpSession.GetTenantId();
        var start = from.Date;
        var end = to.Date;
        if (end < start || (end - start).TotalDays > 366)
            throw new UserFriendlyException("Choose an attendance range of up to 366 days.");

        var canManage = await PermissionChecker.IsGrantedAsync(AppPermissions.PagesRestaurantPayrollAttendanceManage);
        var query = attendanceRepository.GetAll().AsNoTracking().Include(x => x.EmployeeFk)
            .Where(x => x.TenantId == tenantId && x.WorkDate >= start && x.WorkDate <= end);
        if (canManage)
        {
            if (employeeId.HasValue)
                query = query.Where(x => x.EmployeeId == employeeId.Value);
        }
        else
        {
            var mine = await GetCurrentEmployeeOrNull();
            query = mine == null ? query.Where(x => false) : query.Where(x => x.EmployeeId == mine.Id);
        }

        return await query.OrderByDescending(x => x.WorkDate).ThenBy(x => x.EmployeeFk.Name)
            .Select(x => new RestaurantPayrollAttendanceDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.EmployeeFk.Name,
                StaffCode = x.EmployeeFk.StaffCode,
                WorkDate = x.WorkDate,
                ClockIn = x.ClockIn,
                ClockOut = x.ClockOut,
                BreakMinutes = x.BreakMinutes,
                RegularHours = x.RegularHours,
                OvertimeHours = x.OvertimeHours,
                Status = x.Status,
                ShiftName = x.ShiftName,
                Notes = x.Notes
            }).ToListAsync();
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollAttendance)]
    public async Task<RestaurantPayrollAttendanceDto> ClockIn(RestaurantPayrollClockDto input)
    {
        var employee = await ResolveClockEmployee(input.EmployeeId);
        var at = input.At ?? DateTime.Now;
        var workDate = at.Date;
        var existing = await attendanceRepository.FirstOrDefaultAsync(x =>
            x.TenantId == AbpSession.GetTenantId() && x.EmployeeId == employee.Id && x.WorkDate == workDate);
        if (existing != null)
            throw new UserFriendlyException(existing.ClockOut.HasValue ? "Attendance is already completed for today." : "You are already clocked in.");

        var attendance = new RestaurantPayrollAttendance
        {
            Id = SequentialGuidGenerator.Instance.Create(),
            TenantId = AbpSession.GetTenantId(),
            EmployeeId = employee.Id,
            EmployeeFk = employee,
            WorkDate = workDate,
            ClockIn = at,
            BreakMinutes = input.BreakMinutes,
            Status = RestaurantAttendanceStatus.Present,
            ShiftName = input.ShiftName?.Trim(),
            CapturedByUserId = AbpSession.UserId
        };
        await attendanceRepository.InsertAsync(attendance);
        return MapAttendance(attendance);
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollAttendance)]
    public async Task<RestaurantPayrollAttendanceDto> ClockOut(RestaurantPayrollClockDto input)
    {
        var employee = await ResolveClockEmployee(input.EmployeeId);
        var at = input.At ?? DateTime.Now;
        var attendance = await attendanceRepository.FirstOrDefaultAsync(x =>
            x.TenantId == AbpSession.GetTenantId() && x.EmployeeId == employee.Id && x.WorkDate == at.Date)
            ?? throw new UserFriendlyException("No clock-in was found for today.");
        if (!attendance.ClockIn.HasValue)
            throw new UserFriendlyException("Clock-in time is missing.");
        if (attendance.ClockOut.HasValue)
            throw new UserFriendlyException("Attendance is already completed for today.");
        if (at < attendance.ClockIn.Value)
            throw new UserFriendlyException("Clock-out cannot be earlier than clock-in.");

        attendance.ClockOut = at;
        if (input.BreakMinutes > 0)
            attendance.BreakMinutes = input.BreakMinutes;
        CalculateHours(attendance);
        await attendanceRepository.UpdateAsync(attendance);
        attendance.EmployeeFk = employee;
        return MapAttendance(attendance);
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollAttendanceManage)]
    public async Task<Guid> SaveAttendance(SaveRestaurantPayrollAttendanceDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        var employee = await employeeRepository.FirstOrDefaultAsync(x => x.Id == input.EmployeeId && x.TenantId == tenantId)
            ?? throw new UserFriendlyException("Payroll employee was not found.");
        var workDate = input.WorkDate == default ? DateTime.Today : input.WorkDate.Date;
        RestaurantPayrollAttendance attendance;
        if (input.Id.HasValue && input.Id.Value != Guid.Empty)
        {
            attendance = await attendanceRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value && x.TenantId == tenantId)
                ?? throw new UserFriendlyException("Attendance entry was not found.");
        }
        else
        {
            var duplicate = await attendanceRepository.GetAll().AsNoTracking().AnyAsync(x =>
                x.TenantId == tenantId && x.EmployeeId == input.EmployeeId && x.WorkDate == workDate);
            if (duplicate)
                throw new UserFriendlyException("Attendance already exists for this employee and date.");
            attendance = new RestaurantPayrollAttendance
            {
                Id = SequentialGuidGenerator.Instance.Create(),
                TenantId = tenantId,
                EmployeeId = input.EmployeeId
            };
        }

        attendance.EmployeeId = input.EmployeeId;
        attendance.EmployeeFk = employee;
        attendance.WorkDate = workDate;
        attendance.ClockIn = input.ClockIn;
        attendance.ClockOut = input.ClockOut;
        attendance.BreakMinutes = input.BreakMinutes;
        attendance.Status = input.Status;
        attendance.ShiftName = input.ShiftName?.Trim();
        attendance.Notes = input.Notes?.Trim();
        attendance.CapturedByUserId = AbpSession.UserId;
        if (input.RegularHours.HasValue || input.OvertimeHours.HasValue)
        {
            attendance.RegularHours = input.RegularHours ?? 0;
            attendance.OvertimeHours = input.OvertimeHours ?? 0;
        }
        else
        {
            CalculateHours(attendance);
        }

        if (input.Id.HasValue && input.Id.Value != Guid.Empty)
            await attendanceRepository.UpdateAsync(attendance);
        else
            await attendanceRepository.InsertAsync(attendance);
        return attendance.Id;
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollProcess)]
    public async Task<Guid> GeneratePayrollRun(GenerateRestaurantPayrollRunDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        var start = input.PeriodStart.Date;
        var end = input.PeriodEnd.Date;
        if (end < start || (end - start).TotalDays > 366)
            throw new UserFriendlyException("Choose a payroll period of up to 366 days.");

        var employees = await employeeRepository.GetAll().AsNoTracking()
            .Include(x => x.JobRoleFk)
            .Where(x => x.TenantId == tenantId && x.IsActive && x.JoinedOn <= end)
            .OrderBy(x => x.Name)
            .ToListAsync();
        if (employees.Count == 0)
            throw new UserFriendlyException("Add at least one active payroll employee before generating payroll.");

        var attendance = await attendanceRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.WorkDate >= start && x.WorkDate <= end)
            .ToListAsync();
        var attendanceByEmployee = attendance.GroupBy(x => x.EmployeeId).ToDictionary(x => x.Key, x => x.ToList());
        var allowanceByEmployee = await GetAllowancesAt(employees.Select(x => x.Id).ToList(), end);
        var adjustments = (input.Adjustments ?? new List<RestaurantPayrollAdjustmentDto>())
            .GroupBy(x => x.EmployeeId).ToDictionary(x => x.Key, x => x.Last());

        var existing = await payrollRunRepository.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.PeriodStart == start && x.PeriodEnd == end);
        if (existing != null && existing.Status != RestaurantPayrollRunStatus.Draft)
            throw new UserFriendlyException("An approved or paid payroll run already exists for this period.");

        RestaurantPayrollRun run;
        if (existing == null)
        {
            var prefix = $"PAY-{end:yyyyMM}-";
            var sequence = await payrollRunRepository.CountAsync(x => x.TenantId == tenantId && x.RunNumber.StartsWith(prefix)) + 1;
            run = new RestaurantPayrollRun
            {
                Id = SequentialGuidGenerator.Instance.Create(),
                TenantId = tenantId,
                RunNumber = $"{prefix}{sequence:000}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = AbpSession.UserId
            };
            await payrollRunRepository.InsertAsync(run);
        }
        else
        {
            run = existing;
            await payrollLineRepository.DeleteAsync(x => x.TenantId == tenantId && x.PayrollRunId == run.Id);
        }

        run.PeriodStart = start;
        run.PeriodEnd = end;
        run.Status = RestaurantPayrollRunStatus.Draft;
        run.TipsPool = RoundMoney(input.TipsPool);
        run.ServiceChargePool = RoundMoney(input.ServiceChargePool);
        run.Notes = input.Notes?.Trim();

        var eligible = employees.Where(employee =>
            attendanceByEmployee.TryGetValue(employee.Id, out var rows) &&
            rows.Any(x => x.Status == RestaurantAttendanceStatus.Present || x.Status == RestaurantAttendanceStatus.Late))
            .ToList();
        if (eligible.Count == 0)
            eligible = employees;
        var totalWeight = eligible.Sum(x => x.ServiceChargeWeight > 0 ? x.ServiceChargeWeight : 1);

        var lines = new List<RestaurantPayrollLine>();
        foreach (var employee in employees)
        {
            attendanceByEmployee.TryGetValue(employee.Id, out var rows);
            rows ??= new List<RestaurantPayrollAttendance>();
            var workedHours = rows.Sum(x => x.RegularHours);
            var overtimeHours = rows.Sum(x => x.OvertimeHours);
            var basePay = employee.EmploymentType == RestaurantEmploymentType.Hourly
                ? workedHours * employee.HourlyRate
                : employee.BasicSalary * GetMonthlyPeriodFraction(start, end);
            var overtimePay = overtimeHours * employee.OvertimeRate;
            var adjustment = adjustments.GetValueOrDefault(employee.Id);
            var allowance = allowanceByEmployee.GetValueOrDefault(employee.Id) + (adjustment?.AdditionalAllowance ?? 0);
            var isEligible = eligible.Any(x => x.Id == employee.Id);
            var weight = employee.ServiceChargeWeight > 0 ? employee.ServiceChargeWeight : 1;
            var tipsShare = isEligible && totalWeight > 0 ? run.TipsPool * weight / totalWeight : 0;
            var serviceShare = isEligible && totalWeight > 0 ? run.ServiceChargePool * weight / totalWeight : 0;
            var tax = Math.Max(0, adjustment?.TaxDeduction ?? 0);
            var otherDeduction = Math.Max(0, employee.FixedDeduction + (adjustment?.OtherDeduction ?? 0));
            var advance = Math.Max(0, adjustment?.AdvanceRecovery ?? 0);
            var gross = basePay + overtimePay + allowance + tipsShare + serviceShare;
            var deductions = tax + otherDeduction + advance;
            var line = new RestaurantPayrollLine
            {
                Id = SequentialGuidGenerator.Instance.Create(),
                TenantId = tenantId,
                PayrollRunId = run.Id,
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                StaffCode = employee.StaffCode,
                JobRole = employee.JobRoleFk?.Name,
                EmploymentType = employee.EmploymentType,
                WorkedHours = RoundHours(workedHours),
                OvertimeHours = RoundHours(overtimeHours),
                BasicPay = RoundMoney(basePay),
                OvertimePay = RoundMoney(overtimePay),
                Allowance = RoundMoney(allowance),
                TipsShare = RoundMoney(tipsShare),
                ServiceChargeShare = RoundMoney(serviceShare),
                GrossPay = RoundMoney(gross),
                TaxDeduction = RoundMoney(tax),
                OtherDeduction = RoundMoney(otherDeduction),
                AdvanceRecovery = RoundMoney(advance),
                NetPay = RoundMoney(Math.Max(0, gross - deductions)),
                Notes = adjustment?.Notes?.Trim()
            };
            lines.Add(line);
            await payrollLineRepository.InsertAsync(line);
        }

        run.TotalGross = RoundMoney(lines.Sum(x => x.GrossPay));
        run.TotalDeduction = RoundMoney(lines.Sum(x => x.TaxDeduction + x.OtherDeduction + x.AdvanceRecovery));
        run.TotalNet = RoundMoney(lines.Sum(x => x.NetPay));
        await payrollRunRepository.UpdateAsync(run);
        return run.Id;
    }

    public async Task<List<RestaurantPayrollRunDto>> GetPayrollRuns()
    {
        await EnsureAnyPermission(
            AppPermissions.PagesRestaurantPayrollReports,
            AppPermissions.PagesRestaurantPayrollProcess,
            AppPermissions.PagesRestaurantPayrollApprove);
        var tenantId = AbpSession.GetTenantId();
        var runs = await payrollRunRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.PeriodEnd)
            .ThenByDescending(x => x.CreatedAt)
            .Take(60)
            .ToListAsync();
        return await MapRuns(runs);
    }

    public async Task<RestaurantPayrollRunDetailDto> GetPayrollRun(Guid id)
    {
        var tenantId = AbpSession.GetTenantId();
        var run = await payrollRunRepository.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new UserFriendlyException("Payroll run was not found.");
        var canViewAll = await HasAnyPermission(
            AppPermissions.PagesRestaurantPayrollReports,
            AppPermissions.PagesRestaurantPayrollProcess,
            AppPermissions.PagesRestaurantPayrollApprove);
        var lineQuery = payrollLineRepository.GetAll().AsNoTracking().Where(x => x.TenantId == tenantId && x.PayrollRunId == id);
        if (!canViewAll)
        {
            if (!await PermissionChecker.IsGrantedAsync(AppPermissions.PagesRestaurantPayrollOwnPayslip) || run.Status == RestaurantPayrollRunStatus.Draft)
                throw new AbpAuthorizationException("You do not have permission to view this payroll run.");
            var mine = await GetCurrentEmployeeOrNull();
            lineQuery = mine == null ? lineQuery.Where(x => false) : lineQuery.Where(x => x.EmployeeId == mine.Id);
        }
        var lines = await lineQuery.OrderBy(x => x.EmployeeName).ToListAsync();
        if (!canViewAll && lines.Count == 0)
            throw new AbpAuthorizationException("This payslip does not belong to the current user.");
        return MapRunDetail(run, lines);
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollOwnPayslip)]
    public async Task<List<RestaurantPayrollLineDto>> GetMyPayslips()
    {
        var tenantId = AbpSession.GetTenantId();
        var mine = await GetCurrentEmployeeOrNull();
        if (mine == null)
            return new List<RestaurantPayrollLineDto>();
        var lines = await payrollLineRepository.GetAll().AsNoTracking().Include(x => x.PayrollRunFk)
            .Where(x => x.TenantId == tenantId && x.EmployeeId == mine.Id && x.PayrollRunFk.Status != RestaurantPayrollRunStatus.Draft)
            .OrderByDescending(x => x.PayrollRunFk.PeriodEnd)
            .Take(36)
            .ToListAsync();
        return lines.Select(MapLine).ToList();
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollApprove)]
    public async Task ApprovePayrollRun(EntityDto<Guid> input)
    {
        var run = await GetRunForTransition(input.Id, RestaurantPayrollRunStatus.Draft);
        run.Status = RestaurantPayrollRunStatus.Approved;
        run.ApprovedAt = DateTime.UtcNow;
        run.ApprovedByUserId = AbpSession.UserId;
        await payrollRunRepository.UpdateAsync(run);
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollApprove)]
    public async Task MarkPayrollRunPaid(EntityDto<Guid> input)
    {
        var run = await GetRunForTransition(input.Id, RestaurantPayrollRunStatus.Approved);
        run.Status = RestaurantPayrollRunStatus.Paid;
        run.PaidAt = DateTime.UtcNow;
        run.PaidByUserId = AbpSession.UserId;
        await payrollRunRepository.UpdateAsync(run);
    }

    [AbpAuthorize(AppPermissions.PagesRestaurantPayrollProcess)]
    public async Task DeleteDraftPayrollRun(EntityDto<Guid> input)
    {
        var run = await GetRunForTransition(input.Id, RestaurantPayrollRunStatus.Draft);
        await payrollLineRepository.DeleteAsync(x => x.TenantId == run.TenantId && x.PayrollRunId == run.Id);
        await payrollRunRepository.DeleteAsync(run);
    }

    private async Task<RestaurantPayrollEmployeeSaveResultDto> ApplyEmployeeLoginAccess(
        RestaurantPayrollEmployee employee,
        CreateOrEditRestaurantPayrollEmployeeDto input,
        string employeeName,
        int tenantId)
    {
        if (input.LoginMode == RestaurantEmployeeLoginMode.None)
        {
            if (employee.UserId.HasValue)
                await RemoveRestaurantRoles(
                    await GetTenantUser(employee.UserId.Value, tenantId),
                    employee.LoginManagedByRestaurant);
            employee.UserId = null;
            employee.LoginManagedByRestaurant = false;
            return new RestaurantPayrollEmployeeSaveResultDto();
        }

        ValidateRestaurantRole(input.RestaurantRoleName);

        if (input.LoginMode == RestaurantEmployeeLoginMode.LinkExisting)
        {
            if (!input.UserId.HasValue)
                throw new UserFriendlyException("Choose an existing login user.");
            await EnsureUserIsNotLinked(input.UserId.Value, employee.Id, tenantId);
            var user = await GetTenantUser(input.UserId.Value, tenantId);
            await EnsureUserCanReceiveRestaurantRole(user);
            var keepsManagedLogin = employee.UserId == user.Id && employee.LoginManagedByRestaurant;

            if (employee.UserId.HasValue && employee.UserId.Value != user.Id)
                await RemoveRestaurantRoles(
                    await GetTenantUser(employee.UserId.Value, tenantId),
                    employee.LoginManagedByRestaurant);

            await SetRestaurantRole(user, input.RestaurantRoleName);
            user.IsActive = input.IsActive && input.LoginIsActive;
            CheckErrors(await userManager.UpdateAsync(user));
            CheckErrors(await userManager.UpdateSecurityStampAsync(user));
            employee.UserId = user.Id;
            employee.LoginManagedByRestaurant = keepsManagedLogin;
            return new RestaurantPayrollEmployeeSaveResultDto
            {
                UserId = user.Id,
                UserName = user.UserName
            };
        }

        if (input.LoginMode != RestaurantEmployeeLoginMode.CreateNew)
            throw new UserFriendlyException("Choose a valid employee login option.");

        var requestedIdentifier = input.LoginUserName?.Trim();
        var isEmailIdentifier = !string.IsNullOrWhiteSpace(requestedIdentifier) &&
                                new EmailAddressAttribute().IsValid(requestedIdentifier);
        var phoneIdentifier = isEmailIdentifier ? null : NormalizePhoneIdentifier(requestedIdentifier);
        if (!isEmailIdentifier && string.IsNullOrWhiteSpace(phoneIdentifier))
            throw new UserFriendlyException("Employee username must be a valid email address or phone number.");
        var userName = isEmailIdentifier ? requestedIdentifier.ToLowerInvariant() : phoneIdentifier;
        await userPolicy.CheckMaxUserCountAsync(tenantId);

        if (employee.UserId.HasValue)
            await RemoveRestaurantRoles(
                await GetTenantUser(employee.UserId.Value, tenantId),
                employee.LoginManagedByRestaurant);

        var (firstName, surname) = SplitEmployeeName(employeeName);
        var password = await GetEmployeeDefaultPassword();
        var newUser = new User
        {
            TenantId = tenantId,
            UserName = userName,
            Name = firstName,
            Surname = surname,
            EmailAddress = BuildEmployeeEmail(
                isEmailIdentifier ? userName : input.LoginEmailAddress ?? input.EmailAddress,
                userName,
                tenantId),
            PhoneNumber = isEmailIdentifier
                ? input.LoginPhoneNumber?.Trim() ?? input.PhoneNumber?.Trim()
                : phoneIdentifier,
            IsActive = input.IsActive && input.LoginIsActive,
            IsEmailConfirmed = true,
            IsTwoFactorEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEnabled),
            IsLockoutEnabled = true,
            ShouldChangePasswordOnNextLogin = true,
            Roles = new List<UserRole>()
        };
        newUser.SetNormalizedNames();
        await userManager.InitializeOptionsAsync(tenantId);
        CheckErrors(await userManager.CreateAsync(newUser, password));
        await CurrentUnitOfWork.SaveChangesAsync();
        await SetRestaurantRole(newUser, input.RestaurantRoleName);
        employee.UserId = newUser.Id;
        employee.LoginManagedByRestaurant = true;

        return new RestaurantPayrollEmployeeSaveResultDto
        {
            UserId = newUser.Id,
            UserName = newUser.UserName,
            TemporaryPassword = password
        };
    }

    private async Task EnsureUserIsNotLinked(long userId, Guid employeeId, int tenantId)
    {
        var duplicate = await employeeRepository.GetAll().AsNoTracking().AnyAsync(x =>
            x.TenantId == tenantId && x.UserId == userId && x.Id != employeeId);
        if (duplicate)
            throw new UserFriendlyException("That login user is already linked to another payroll employee.");
    }

    private async Task<User> GetTenantUser(long userId, int tenantId)
        => await userRepository.FirstOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId)
           ?? throw new UserFriendlyException("Login user was not found in this restaurant.");

    private async Task EnsureUserCanReceiveRestaurantRole(User user)
    {
        var roleNames = await userManager.GetRolesAsync(user);
        if (user.UserName.Equals(AbpUserBase.AdminUserName, StringComparison.OrdinalIgnoreCase) ||
            roleNames.Contains(StaticRoleNames.Tenants.Admin, StringComparer.OrdinalIgnoreCase))
            throw new UserFriendlyException("The tenant Admin account cannot be linked or changed from restaurant staff access.");
        if (!new EmailAddressAttribute().IsValid(user.UserName) && NormalizePhoneIdentifier(user.UserName) == null)
            throw new UserFriendlyException("Employee username must be an email address or phone number.");
    }

    private async Task SetRestaurantRole(User user, string roleName)
    {
        ValidateRestaurantRole(roleName);
        var exists = await roleRepository.GetAll().AsNoTracking()
            .AnyAsync(x => x.TenantId == user.TenantId && x.Name == roleName);
        if (!exists)
            throw new UserFriendlyException("The selected restaurant role is not available. Restart the backend once to seed static roles.");

        var current = await userManager.GetRolesAsync(user);
        var retained = current.Where(x => !RestaurantOperatingRoleNames.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
        retained.Add(roleName);
        CheckErrors(await userManager.SetRolesAsync(user, retained.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
    }

    private async Task RemoveRestaurantRoles(User user, bool disableLoginWhenUnused)
    {
        var current = await userManager.GetRolesAsync(user);
        var retained = current.Where(x => !RestaurantOperatingRoleNames.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();
        var rolesChanged = retained.Length != current.Count;
        if (rolesChanged)
            CheckErrors(await userManager.SetRolesAsync(user, retained));

        var hasOtherApplicationRole = retained.Any(x =>
            !x.Equals(StaticRoleNames.Tenants.User, StringComparison.OrdinalIgnoreCase));
        var activeChanged = disableLoginWhenUnused && !hasOtherApplicationRole && user.IsActive;
        if (activeChanged)
        {
            user.IsActive = false;
            CheckErrors(await userManager.UpdateAsync(user));
        }
        if (rolesChanged || activeChanged)
            CheckErrors(await userManager.UpdateSecurityStampAsync(user));
    }

    private async Task SetLinkedUserActive(User user, bool isActive)
    {
        var roleNames = await userManager.GetRolesAsync(user);
        if (user.UserName.Equals(AbpUserBase.AdminUserName, StringComparison.OrdinalIgnoreCase) ||
            roleNames.Contains(StaticRoleNames.Tenants.Admin, StringComparer.OrdinalIgnoreCase))
            return;
        if (user.IsActive == isActive)
            return;
        user.IsActive = isActive;
        CheckErrors(await userManager.UpdateAsync(user));
        CheckErrors(await userManager.UpdateSecurityStampAsync(user));
    }

    private async Task<string> GetEmployeeDefaultPassword()
    {
        var value = await SettingManager.GetSettingValueForTenantAsync(
            AppSettings.UserManagement.EmployeeDefaultPassword,
            AbpSession.GetTenantId());
        return string.IsNullOrWhiteSpace(value)
            ? ErpConsts.DefaultRestaurantEmployeePassword
            : value;
    }

    private async Task<RestaurantPayrollEmployee> GetManagedEmployeeForPasswordChange(Guid employeeId)
    {
        var tenantId = AbpSession.GetTenantId();
        var employee = await employeeRepository.FirstOrDefaultAsync(x =>
            x.Id == employeeId && x.TenantId == tenantId)
            ?? throw new UserFriendlyException("Payroll employee was not found.");

        if (!employee.UserId.HasValue)
            throw new UserFriendlyException("This employee does not have a login account.");
        if (!employee.LoginManagedByRestaurant)
            throw new UserFriendlyException(
                "This linked login is managed outside restaurant payroll. Change its password from user administration.");
        return employee;
    }

    private async Task SetManagedEmployeePassword(
        RestaurantPayrollEmployee employee,
        string password,
        bool forceChangeOnNextLogin)
    {
        var user = await GetTenantUser(employee.UserId.Value, employee.TenantId);
        await userManager.InitializeOptionsAsync(employee.TenantId);
        CheckErrors(await userManager.ChangePasswordAsync(user, password));
        user.ShouldChangePasswordOnNextLogin = forceChangeOnNextLogin;
        user.PasswordResetCode = null;
        user.IsActive = employee.IsActive;
        CheckErrors(await userManager.UpdateAsync(user));
        CheckErrors(await userManager.UpdateSecurityStampAsync(user));
        await CurrentUnitOfWork.SaveChangesAsync();
        await cacheManager.GetCache(AppConsts.SecurityStampKey)
            .SetAsync($"{user.TenantId}.{user.Id}", user.SecurityStamp);
    }

    private static void ValidateRestaurantRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName) ||
            !RestaurantOperatingRoleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            throw new UserFriendlyException("Choose one approved restaurant operating role.");
    }

    private static (string FirstName, string Surname) SplitEmployeeName(string fullName)
    {
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = Truncate(parts.Length == 0 ? "Staff" : parts[0], AbpUserBase.MaxNameLength);
        var surname = Truncate(parts.Length > 1 ? parts[1] : "Member", AbpUserBase.MaxSurnameLength);
        return (firstName, surname);
    }

    private static string BuildEmployeeEmail(string requestedEmail, string userName, int tenantId)
    {
        if (!string.IsNullOrWhiteSpace(requestedEmail))
            return requestedEmail.Trim();
        var safeName = new string(userName.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "staff";
        safeName = Truncate(safeName, 32);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{safeName}.{tenantId}.{suffix}@staff.nextwaverestro.local";
    }

    private static string NormalizePhoneIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        var hasLeadingPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length < 7 || digits.Length > 15)
            return null;
        return hasLeadingPlus ? $"+{digits}" : digits;
    }

    private static string NormalizeMiti(string value)
    {
        var normalized = value?.Trim().Replace('-', '/');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private async Task EnrichEmployeeAccess(List<RestaurantPayrollEmployeeDto> employees)
    {
        var userIds = employees.Where(x => x.UserId.HasValue).Select(x => x.UserId.Value).Distinct().ToList();
        if (userIds.Count == 0)
            return;
        var users = await userRepository.GetAll().AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        var roleNames = await GetRestaurantRoleNames(userIds);
        foreach (var employee in employees.Where(x => x.UserId.HasValue))
        {
            if (!users.TryGetValue(employee.UserId.Value, out var user))
                continue;
            employee.LoginUserName = user.UserName;
            employee.LoginEmailAddress = user.EmailAddress;
            employee.LoginPhoneNumber = user.PhoneNumber;
            employee.LoginIsActive = user.IsActive;
            employee.RestaurantRoleName = roleNames.GetValueOrDefault(user.Id);
        }
    }

    private async Task EnrichUserRoles(List<RestaurantPayrollUserLookupDto> users)
    {
        var roleNames = await GetRestaurantRoleNames(users.Select(x => x.Id).ToList());
        foreach (var user in users)
            user.RestaurantRoleName = roleNames.GetValueOrDefault(user.Id);
    }

    private async Task<Dictionary<long, string>> GetRestaurantRoleNames(List<long> userIds)
    {
        if (userIds.Count == 0)
            return new Dictionary<long, string>();
        var assignments = await (from userRole in userRoleRepository.GetAll().AsNoTracking()
                                 join role in roleRepository.GetAll().AsNoTracking() on userRole.RoleId equals role.Id
                                 where userIds.Contains(userRole.UserId) && RestaurantOperatingRoleNames.Contains(role.Name)
                                 select new { userRole.UserId, role.Name }).ToListAsync();
        return assignments.GroupBy(x => x.UserId).ToDictionary(
            group => group.Key,
            group => RestaurantOperatingRoleNames.FirstOrDefault(roleName =>
                group.Any(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))));
    }

    private async Task<RestaurantPayrollEmployee> ResolveClockEmployee(Guid? requestedEmployeeId)
    {
        var tenantId = AbpSession.GetTenantId();
        var canManage = await PermissionChecker.IsGrantedAsync(AppPermissions.PagesRestaurantPayrollAttendanceManage);
        if (requestedEmployeeId.HasValue && canManage)
            return await employeeRepository.FirstOrDefaultAsync(x => x.Id == requestedEmployeeId.Value && x.TenantId == tenantId && x.IsActive)
                ?? throw new UserFriendlyException("Active payroll employee was not found.");
        return await GetCurrentEmployeeOrNull()
            ?? throw new UserFriendlyException("Your login is not linked to an active payroll employee profile.");
    }

    private async Task<RestaurantPayrollEmployee> GetCurrentEmployeeOrNull()
    {
        if (!AbpSession.UserId.HasValue)
            return null;
        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId.Value;
        return await employeeRepository.GetAll()
            .Include(x => x.DepartmentFk)
            .Include(x => x.JobRoleFk)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive);
    }

    private async Task<RestaurantPayrollRun> GetRunForTransition(Guid id, RestaurantPayrollRunStatus expected)
    {
        var tenantId = AbpSession.GetTenantId();
        var run = await payrollRunRepository.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new UserFriendlyException("Payroll run was not found.");
        if (run.Status != expected)
            throw new UserFriendlyException($"Payroll run must be {expected} for this action.");
        return run;
    }

    private async Task<List<RestaurantPayrollRunDto>> MapRuns(List<RestaurantPayrollRun> runs)
    {
        if (runs.Count == 0)
            return new List<RestaurantPayrollRunDto>();
        var ids = runs.Select(x => x.Id).ToList();
        var counts = await payrollLineRepository.GetAll().AsNoTracking()
            .Where(x => ids.Contains(x.PayrollRunId))
            .GroupBy(x => x.PayrollRunId)
            .Select(x => new { Id = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);
        return runs.Select(x => MapRun(x, counts.GetValueOrDefault(x.Id))).ToList();
    }

    private async Task<decimal> GetAllowanceAt(Guid employeeId, DateTime effectiveAt)
        => (await GetAllowancesAt(new List<Guid> { employeeId }, effectiveAt)).GetValueOrDefault(employeeId);

    private async Task<Dictionary<Guid, decimal>> GetAllowancesAt(List<Guid> employeeIds, DateTime effectiveAt)
    {
        if (employeeIds.Count == 0)
            return new Dictionary<Guid, decimal>();
        var tenantId = AbpSession.GetTenantId();
        var histories = await allowanceHistoryRepository.GetAll().AsNoTracking()
            .Where(x => x.TenantId == tenantId && employeeIds.Contains(x.EmployeeId) &&
                        x.EffectiveFrom <= effectiveAt.Date)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
        return histories.GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.First().Amount);
    }

    private static RestaurantPayrollEmployeeDto MapEmployee(RestaurantPayrollEmployee x, decimal fixedAllowance) => new()
    {
        Id = x.Id,
        UserId = x.UserId,
        LoginManagedByRestaurant = x.LoginManagedByRestaurant,
        StaffCode = x.StaffCode,
        Name = x.Name,
        PhoneNumber = x.PhoneNumber,
        EmailAddress = x.EmailAddress,
        DateOfBirth = x.DateOfBirth,
        DateOfBirthMiti = x.DateOfBirthMiti,
        Gender = x.Gender,
        BloodGroup = x.BloodGroup,
        MaritalStatus = x.MaritalStatus,
        Address = x.Address,
        CitizenshipNumber = x.CitizenshipNumber,
        EmergencyContactName = x.EmergencyContactName,
        EmergencyContactPhone = x.EmergencyContactPhone,
        DepartmentId = x.DepartmentId,
        JobRoleId = x.JobRoleId,
        JobRole = x.JobRoleFk?.Name,
        Department = x.DepartmentFk?.Name,
        EmploymentType = x.EmploymentType,
        BasicSalary = x.BasicSalary,
        HourlyRate = x.HourlyRate,
        OvertimeRate = x.OvertimeRate,
        FixedAllowance = fixedAllowance,
        FixedDeduction = x.FixedDeduction,
        ServiceChargeWeight = x.ServiceChargeWeight,
        BankName = x.BankName,
        BankAccountNumber = x.BankAccountNumber,
        PanNumber = x.PanNumber,
        SsfNumber = x.SsfNumber,
        JoinedOn = x.JoinedOn,
        JoinedOnMiti = x.JoinedOnMiti,
        Notes = x.Notes,
        IsActive = x.IsActive
    };

    private static RestaurantPayrollAttendanceDto MapAttendance(RestaurantPayrollAttendance x) => new()
    {
        Id = x.Id,
        EmployeeId = x.EmployeeId,
        EmployeeName = x.EmployeeFk?.Name,
        StaffCode = x.EmployeeFk?.StaffCode,
        WorkDate = x.WorkDate,
        ClockIn = x.ClockIn,
        ClockOut = x.ClockOut,
        BreakMinutes = x.BreakMinutes,
        RegularHours = x.RegularHours,
        OvertimeHours = x.OvertimeHours,
        Status = x.Status,
        ShiftName = x.ShiftName,
        Notes = x.Notes
    };

    private static RestaurantPayrollRunDto MapRun(RestaurantPayrollRun x, int employeeCount) => new()
    {
        Id = x.Id,
        RunNumber = x.RunNumber,
        PeriodStart = x.PeriodStart,
        PeriodEnd = x.PeriodEnd,
        Status = x.Status,
        TipsPool = x.TipsPool,
        ServiceChargePool = x.ServiceChargePool,
        TotalGross = x.TotalGross,
        TotalDeduction = x.TotalDeduction,
        TotalNet = x.TotalNet,
        Notes = x.Notes,
        CreatedAt = x.CreatedAt,
        EmployeeCount = employeeCount
    };

    private static RestaurantPayrollRunDetailDto MapRunDetail(RestaurantPayrollRun x, List<RestaurantPayrollLine> lines) => new()
    {
        Id = x.Id,
        RunNumber = x.RunNumber,
        PeriodStart = x.PeriodStart,
        PeriodEnd = x.PeriodEnd,
        Status = x.Status,
        TipsPool = x.TipsPool,
        ServiceChargePool = x.ServiceChargePool,
        TotalGross = x.TotalGross,
        TotalDeduction = x.TotalDeduction,
        TotalNet = x.TotalNet,
        Notes = x.Notes,
        CreatedAt = x.CreatedAt,
        EmployeeCount = lines.Count,
        Lines = lines.Select(line => MapLine(line, x)).ToList()
    };

    private static RestaurantPayrollLineDto MapLine(RestaurantPayrollLine x) => MapLine(x, x.PayrollRunFk);

    private static RestaurantPayrollLineDto MapLine(RestaurantPayrollLine x, RestaurantPayrollRun run) => new()
    {
        Id = x.Id,
        EmployeeId = x.EmployeeId,
        EmployeeName = x.EmployeeName,
        StaffCode = x.StaffCode,
        JobRole = x.JobRole,
        EmploymentType = x.EmploymentType,
        WorkedHours = x.WorkedHours,
        OvertimeHours = x.OvertimeHours,
        BasicPay = x.BasicPay,
        OvertimePay = x.OvertimePay,
        Allowance = x.Allowance,
        TipsShare = x.TipsShare,
        ServiceChargeShare = x.ServiceChargeShare,
        GrossPay = x.GrossPay,
        TaxDeduction = x.TaxDeduction,
        OtherDeduction = x.OtherDeduction,
        AdvanceRecovery = x.AdvanceRecovery,
        NetPay = x.NetPay,
        Notes = x.Notes,
        RunNumber = run?.RunNumber,
        PeriodStart = run?.PeriodStart,
        PeriodEnd = run?.PeriodEnd,
        RunStatus = run?.Status
    };

    private static void CalculateHours(RestaurantPayrollAttendance attendance)
    {
        if (!attendance.ClockIn.HasValue || !attendance.ClockOut.HasValue || attendance.Status is RestaurantAttendanceStatus.Leave or RestaurantAttendanceStatus.Absent)
        {
            attendance.RegularHours = 0;
            attendance.OvertimeHours = 0;
            return;
        }
        var hours = Math.Max(0, (attendance.ClockOut.Value - attendance.ClockIn.Value).TotalHours - attendance.BreakMinutes / 60d);
        attendance.RegularHours = RoundHours((decimal)Math.Min(8, hours));
        attendance.OvertimeHours = RoundHours((decimal)Math.Max(0, hours - 8));
    }

    private static decimal GetMonthlyPeriodFraction(DateTime start, DateTime end)
    {
        decimal fraction = 0;
        var cursor = start.Date;
        while (cursor <= end.Date)
        {
            var monthEnd = new DateTime(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month));
            var coveredEnd = monthEnd < end ? monthEnd : end.Date;
            var coveredDays = (coveredEnd - cursor).Days + 1;
            fraction += (decimal)coveredDays / DateTime.DaysInMonth(cursor.Year, cursor.Month);
            cursor = monthEnd.AddDays(1);
        }
        return fraction;
    }

    private async Task<bool> HasAnyPermission(params string[] permissions)
    {
        foreach (var permission in permissions)
            if (await PermissionChecker.IsGrantedAsync(permission))
                return true;
        return false;
    }

    private async Task EnsureAnyPermission(params string[] permissions)
    {
        if (!await HasAnyPermission(permissions))
            throw new AbpAuthorizationException("You do not have permission to view restaurant payroll runs.");
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal RoundHours(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
