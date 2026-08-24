using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant;

[Table("tbl_RestaurantPayrollLine")]
public class RestaurantPayrollLine : Entity<Guid>, IMustHaveTenant
{
    public int TenantId { get; set; }
    public Guid PayrollRunId { get; set; }
    [ForeignKey(nameof(PayrollRunId))] public RestaurantPayrollRun PayrollRunFk { get; set; }
    public Guid EmployeeId { get; set; }
    [ForeignKey(nameof(EmployeeId))] public RestaurantPayrollEmployee EmployeeFk { get; set; }
    [Required, StringLength(150)] public string EmployeeName { get; set; }
    [StringLength(30)] public string StaffCode { get; set; }
    [StringLength(80)] public string JobRole { get; set; }
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
    [StringLength(500)] public string Notes { get; set; }
}
