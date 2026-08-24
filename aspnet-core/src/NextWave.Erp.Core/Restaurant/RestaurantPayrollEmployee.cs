using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant;

[Table("tbl_RestaurantPayrollEmployee")]
public class RestaurantPayrollEmployee : Entity<Guid>, IMustHaveTenant
{
    public int TenantId { get; set; }
    public long? UserId { get; set; }
    public bool LoginManagedByRestaurant { get; set; }
    [Required, StringLength(30)] public string StaffCode { get; set; }
    [Required, StringLength(150)] public string Name { get; set; }
    [StringLength(32)] public string PhoneNumber { get; set; }
    [StringLength(256)] public string EmailAddress { get; set; }
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
    public decimal BasicSalary { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal FixedDeduction { get; set; }
    public decimal ServiceChargeWeight { get; set; } = 1;
    [StringLength(100)] public string BankName { get; set; }
    [StringLength(80)] public string BankAccountNumber { get; set; }
    [StringLength(80)] public string PanNumber { get; set; }
    [StringLength(80)] public string SsfNumber { get; set; }
    public DateTime JoinedOn { get; set; }
    [StringLength(10)] public string JoinedOnMiti { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DepartmentId))]
    public virtual RestaurantPayrollDepartment DepartmentFk { get; set; }

    [ForeignKey(nameof(JobRoleId))]
    public virtual RestaurantPayrollJobRole JobRoleFk { get; set; }
}
