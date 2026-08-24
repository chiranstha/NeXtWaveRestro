using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using Abp.Authorization.Users;
using NextWave.Erp.Authorization.Users;

namespace NextWave.Erp.Restaurant.Dtos;

public class RestaurantPayrollEmployeeDto
{
    public Guid Id { get; set; }
    public long? UserId { get; set; }
    public string LoginUserName { get; set; }
    public string LoginEmailAddress { get; set; }
    public string LoginPhoneNumber { get; set; }
    public string RestaurantRoleName { get; set; }
    public bool? LoginIsActive { get; set; }
    public bool LoginManagedByRestaurant { get; set; }
    public string StaffCode { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string EmailAddress { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string DateOfBirthMiti { get; set; }
    public string Gender { get; set; }
    public string BloodGroup { get; set; }
    public string MaritalStatus { get; set; }
    public string Address { get; set; }
    public string CitizenshipNumber { get; set; }
    public string EmergencyContactName { get; set; }
    public string EmergencyContactPhone { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid JobRoleId { get; set; }
    public string JobRole { get; set; }
    public string Department { get; set; }
    public RestaurantEmploymentType EmploymentType { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal FixedAllowance { get; set; }
    public decimal FixedDeduction { get; set; }
    public decimal ServiceChargeWeight { get; set; }
    public string BankName { get; set; }
    public string BankAccountNumber { get; set; }
    public string PanNumber { get; set; }
    public string SsfNumber { get; set; }
    public DateTime JoinedOn { get; set; }
    public string JoinedOnMiti { get; set; }
    public string Notes { get; set; }
    public bool IsActive { get; set; }
}

public class CreateOrEditRestaurantPayrollEmployeeDto
{
    public Guid? Id { get; set; }
    public long? UserId { get; set; }
    public bool UpdateLoginAccess { get; set; }
    public RestaurantEmployeeLoginMode LoginMode { get; set; }
    [StringLength(AbpUserBase.MaxUserNameLength)] public string LoginUserName { get; set; }
    [EmailAddress, StringLength(AbpUserBase.MaxEmailAddressLength)] public string LoginEmailAddress { get; set; }
    [StringLength(UserConsts.MaxPhoneNumberLength)] public string LoginPhoneNumber { get; set; }
    [StringLength(64)] public string RestaurantRoleName { get; set; }
    public bool LoginIsActive { get; set; } = true;
    [Required, StringLength(30)] public string StaffCode { get; set; }
    [Required, StringLength(150)] public string Name { get; set; }
    [StringLength(32)] public string PhoneNumber { get; set; }
    [EmailAddress, StringLength(256)] public string EmailAddress { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [StringLength(10)] public string DateOfBirthMiti { get; set; }
    [StringLength(20)] public string Gender { get; set; }
    [StringLength(10)] public string BloodGroup { get; set; }
    [StringLength(20)] public string MaritalStatus { get; set; }
    [StringLength(300)] public string Address { get; set; }
    [StringLength(80)] public string CitizenshipNumber { get; set; }
    [StringLength(150)] public string EmergencyContactName { get; set; }
    [StringLength(32)] public string EmergencyContactPhone { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid JobRoleId { get; set; }
    public RestaurantEmploymentType EmploymentType { get; set; }
    [Range(0, double.MaxValue)] public decimal BasicSalary { get; set; }
    [Range(0, double.MaxValue)] public decimal HourlyRate { get; set; }
    [Range(0, double.MaxValue)] public decimal OvertimeRate { get; set; }
    [Range(0, double.MaxValue)] public decimal FixedAllowance { get; set; }
    [Range(0, double.MaxValue)] public decimal FixedDeduction { get; set; }
    [Range(0, double.MaxValue)] public decimal ServiceChargeWeight { get; set; } = 1;
    [StringLength(100)] public string BankName { get; set; }
    [StringLength(80)] public string BankAccountNumber { get; set; }
    [StringLength(80)] public string PanNumber { get; set; }
    [StringLength(80)] public string SsfNumber { get; set; }
    public DateTime JoinedOn { get; set; }
    [StringLength(10)] public string JoinedOnMiti { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RestaurantPayrollUserLookupDto
{
    public long Id { get; set; }
    public string UserName { get; set; }
    public string Name { get; set; }
    public string EmailAddress { get; set; }
    public string PhoneNumber { get; set; }
    public string RestaurantRoleName { get; set; }
    public bool IsActive { get; set; }
}

public class RestaurantRoleOptionDto
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
}

public class RestaurantPayrollStaffAccessOptionsDto
{
    public List<RestaurantRoleOptionDto> Roles { get; set; } = new();
    public List<RestaurantPayrollUserLookupDto> AvailableUsers { get; set; } = new();
}

public class RestaurantPayrollEmployeeSaveResultDto
{
    public Guid EmployeeId { get; set; }
    public long? UserId { get; set; }
    public string UserName { get; set; }
    [DisableAuditing]
    public string TemporaryPassword { get; set; }
}

public class RestaurantPayrollDepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class RestaurantPayrollJobRoleDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class RestaurantPayrollStaffMastersDto
{
    public List<RestaurantPayrollDepartmentDto> Departments { get; set; } = new();
    public List<RestaurantPayrollJobRoleDto> JobRoles { get; set; } = new();
}

public class SaveRestaurantPayrollDepartmentDto
{
    public Guid? Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; }
    [StringLength(300)] public string Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveRestaurantPayrollJobRoleDto
{
    public Guid? Id { get; set; }
    public Guid DepartmentId { get; set; }
    [Required, StringLength(80)] public string Name { get; set; }
    [StringLength(300)] public string Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RestaurantPayrollAllowanceHistoryDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string EffectiveFromMiti { get; set; }
    public string Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public class AddRestaurantPayrollAllowanceRevisionDto
{
    public Guid EmployeeId { get; set; }
    [Range(0, double.MaxValue)] public decimal Amount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    [StringLength(10)] public string EffectiveFromMiti { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; }
}

public class ChangeRestaurantPayrollEmployeePasswordDto
{
    public Guid EmployeeId { get; set; }

    [Required, MinLength(6), DisableAuditing]
    public string Password { get; set; }

    public bool ForceChangeOnNextLogin { get; set; } = true;
}

public class RestaurantPayrollAttendanceDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string StaffCode { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public int BreakMinutes { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public RestaurantAttendanceStatus Status { get; set; }
    public string ShiftName { get; set; }
    public string Notes { get; set; }
}

public class SaveRestaurantPayrollAttendanceDto
{
    public Guid? Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    [Range(0, 1440)] public int BreakMinutes { get; set; }
    [Range(0, 24)] public decimal? RegularHours { get; set; }
    [Range(0, 24)] public decimal? OvertimeHours { get; set; }
    public RestaurantAttendanceStatus Status { get; set; }
    [StringLength(80)] public string ShiftName { get; set; }
    [StringLength(500)] public string Notes { get; set; }
}

public class RestaurantPayrollClockDto
{
    public Guid? EmployeeId { get; set; }
    public DateTime? At { get; set; }
    [StringLength(80)] public string ShiftName { get; set; }
    [Range(0, 1440)] public int BreakMinutes { get; set; }
}

public class RestaurantPayrollAdjustmentDto
{
    public Guid EmployeeId { get; set; }
    public decimal AdditionalAllowance { get; set; }
    public decimal OtherDeduction { get; set; }
    public decimal AdvanceRecovery { get; set; }
    public decimal TaxDeduction { get; set; }
    [StringLength(500)] public string Notes { get; set; }
}

public class GenerateRestaurantPayrollRunDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    [Range(0, double.MaxValue)] public decimal TipsPool { get; set; }
    [Range(0, double.MaxValue)] public decimal ServiceChargePool { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    public List<RestaurantPayrollAdjustmentDto> Adjustments { get; set; } = new();
}

public class RestaurantPayrollRunDto
{
    public Guid Id { get; set; }
    public string RunNumber { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public RestaurantPayrollRunStatus Status { get; set; }
    public decimal TipsPool { get; set; }
    public decimal ServiceChargePool { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal TotalNet { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EmployeeCount { get; set; }
}

public class RestaurantPayrollLineDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string StaffCode { get; set; }
    public string JobRole { get; set; }
    public RestaurantEmploymentType EmploymentType { get; set; }
    public decimal WorkedHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BasicPay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Allowance { get; set; }
    public decimal TipsShare { get; set; }
    public decimal ServiceChargeShare { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal OtherDeduction { get; set; }
    public decimal AdvanceRecovery { get; set; }
    public decimal NetPay { get; set; }
    public string Notes { get; set; }
    public string RunNumber { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public RestaurantPayrollRunStatus? RunStatus { get; set; }
}

public class RestaurantPayrollRunDetailDto : RestaurantPayrollRunDto
{
    public List<RestaurantPayrollLineDto> Lines { get; set; } = new();
}

public class RestaurantPayrollDashboardDto
{
    public int ActiveEmployeeCount { get; set; }
    public int PresentToday { get; set; }
    public int OpenClockIns { get; set; }
    public decimal CurrentMonthGross { get; set; }
    public decimal CurrentMonthNet { get; set; }
    public RestaurantPayrollEmployeeDto MyProfile { get; set; }
    public RestaurantPayrollAttendanceDto MyTodayAttendance { get; set; }
    public List<RestaurantPayrollRunDto> RecentRuns { get; set; } = new();
    public List<RestaurantPayrollLineDto> MyRecentPayslips { get; set; } = new();
}

public class RestaurantPayrollReportSummaryDto
{
    public int ActiveEmployeeCount { get; set; }
    public int PayrollRunCount { get; set; }
    public int AttendanceRecordCount { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalOvertimeHours { get; set; }
}

public class RestaurantPayrollEmployeeCostReportDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string StaffCode { get; set; }
    public string JobRole { get; set; }
    public decimal WorkedHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BasicPay { get; set; }
    public decimal Allowance { get; set; }
    public decimal TipsAndServiceCharge { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetPay { get; set; }
}

public class RestaurantPayrollAttendanceStatusReportDto
{
    public RestaurantAttendanceStatus Status { get; set; }
    public string StatusName { get; set; }
    public int RecordCount { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
}

public class RestaurantPayrollReportBundleDto
{
    public RestaurantPayrollReportSummaryDto Summary { get; set; } = new();
    public List<RestaurantPayrollRunDto> Runs { get; set; } = new();
    public List<RestaurantPayrollEmployeeCostReportDto> EmployeeCosts { get; set; } = new();
    public List<RestaurantPayrollAttendanceStatusReportDto> Attendance { get; set; } = new();
}
