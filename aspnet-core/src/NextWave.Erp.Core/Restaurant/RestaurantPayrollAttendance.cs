using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant;

[Table("tbl_RestaurantPayrollAttendance")]
public class RestaurantPayrollAttendance : Entity<Guid>, IMustHaveTenant
{
    public int TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    [ForeignKey(nameof(EmployeeId))] public RestaurantPayrollEmployee EmployeeFk { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public int BreakMinutes { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public RestaurantAttendanceStatus Status { get; set; }
    [StringLength(80)] public string ShiftName { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    public long? CapturedByUserId { get; set; }
}
