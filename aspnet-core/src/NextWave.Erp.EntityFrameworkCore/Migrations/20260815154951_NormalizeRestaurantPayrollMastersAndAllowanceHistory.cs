using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRestaurantPayrollMastersAndAllowanceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_Department_IsActive",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.AddColumn<string>(
                name: "DateOfBirthMiti",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "tbl_RestaurantPayrollEmployee",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "JobRoleId",
                table: "tbl_RestaurantPayrollEmployee",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "JoinedOnMiti",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPayrollAllowanceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveFromMiti = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPayrollAllowanceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantPayrollAllowanceHistory_tbl_RestaurantPayrollEmployee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "tbl_RestaurantPayrollEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPayrollDepartment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPayrollDepartment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPayrollJobRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPayrollJobRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantPayrollJobRole_tbl_RestaurantPayrollDepartment_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "tbl_RestaurantPayrollDepartment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
INSERT INTO tbl_RestaurantPayrollDepartment (Id, TenantId, Name, Description, SortOrder, IsActive, CreatedAt)
SELECT NEWID(), source.TenantId, source.Name, 'Created from existing payroll employees',
       ROW_NUMBER() OVER (PARTITION BY source.TenantId ORDER BY source.Name), 1, GETUTCDATE()
FROM (
    SELECT DISTINCT TenantId,
           COALESCE(NULLIF(LTRIM(RTRIM(Department)), ''), 'General') AS Name
    FROM tbl_RestaurantPayrollEmployee
) source;

INSERT INTO tbl_RestaurantPayrollJobRole (Id, TenantId, DepartmentId, Name, Description, SortOrder, IsActive, CreatedAt)
SELECT NEWID(), source.TenantId, department.Id, source.Name, 'Created from existing payroll employees',
       ROW_NUMBER() OVER (PARTITION BY source.TenantId, department.Id ORDER BY source.Name), 1, GETUTCDATE()
FROM (
    SELECT DISTINCT TenantId,
           COALESCE(NULLIF(LTRIM(RTRIM(Department)), ''), 'General') AS DepartmentName,
           COALESCE(NULLIF(LTRIM(RTRIM(JobRole)), ''), 'Staff') AS Name
    FROM tbl_RestaurantPayrollEmployee
) source
INNER JOIN tbl_RestaurantPayrollDepartment department
    ON department.TenantId = source.TenantId AND department.Name = source.DepartmentName;

UPDATE employee
SET DepartmentId = department.Id,
    JobRoleId = jobRole.Id
FROM tbl_RestaurantPayrollEmployee employee
INNER JOIN tbl_RestaurantPayrollDepartment department
    ON department.TenantId = employee.TenantId
   AND department.Name = COALESCE(NULLIF(LTRIM(RTRIM(employee.Department)), ''), 'General')
INNER JOIN tbl_RestaurantPayrollJobRole jobRole
    ON jobRole.TenantId = employee.TenantId
   AND jobRole.DepartmentId = department.Id
   AND jobRole.Name = COALESCE(NULLIF(LTRIM(RTRIM(employee.JobRole)), ''), 'Staff');

INSERT INTO tbl_RestaurantPayrollAllowanceHistory
    (Id, TenantId, EmployeeId, Amount, EffectiveFrom, EffectiveFromMiti, Reason, CreatedAt, CreatedByUserId)
SELECT NEWID(), TenantId, Id, FixedAllowance, JoinedOn, JoinedOnMiti,
       'Opening allowance migrated from employee record', GETUTCDATE(), NULL
FROM tbl_RestaurantPayrollEmployee;
");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "FixedAllowance",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "JobRole",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_DepartmentId",
                table: "tbl_RestaurantPayrollEmployee",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_JobRoleId",
                table: "tbl_RestaurantPayrollEmployee",
                column: "JobRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_DepartmentId_IsActive",
                table: "tbl_RestaurantPayrollEmployee",
                columns: new[] { "TenantId", "DepartmentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_JobRoleId_IsActive",
                table: "tbl_RestaurantPayrollEmployee",
                columns: new[] { "TenantId", "JobRoleId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollAllowanceHistory_EmployeeId",
                table: "tbl_RestaurantPayrollAllowanceHistory",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollAllowanceHistory_TenantId_EmployeeId_EffectiveFrom",
                table: "tbl_RestaurantPayrollAllowanceHistory",
                columns: new[] { "TenantId", "EmployeeId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollDepartment_TenantId_IsActive_SortOrder",
                table: "tbl_RestaurantPayrollDepartment",
                columns: new[] { "TenantId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollDepartment_TenantId_Name",
                table: "tbl_RestaurantPayrollDepartment",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollJobRole_DepartmentId",
                table: "tbl_RestaurantPayrollJobRole",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollJobRole_TenantId_DepartmentId_IsActive_SortOrder",
                table: "tbl_RestaurantPayrollJobRole",
                columns: new[] { "TenantId", "DepartmentId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollJobRole_TenantId_DepartmentId_Name",
                table: "tbl_RestaurantPayrollJobRole",
                columns: new[] { "TenantId", "DepartmentId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_RestaurantPayrollEmployee_tbl_RestaurantPayrollDepartment_DepartmentId",
                table: "tbl_RestaurantPayrollEmployee",
                column: "DepartmentId",
                principalTable: "tbl_RestaurantPayrollDepartment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_RestaurantPayrollEmployee_tbl_RestaurantPayrollJobRole_JobRoleId",
                table: "tbl_RestaurantPayrollEmployee",
                column: "JobRoleId",
                principalTable: "tbl_RestaurantPayrollJobRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_RestaurantPayrollEmployee_tbl_RestaurantPayrollDepartment_DepartmentId",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_RestaurantPayrollEmployee_tbl_RestaurantPayrollJobRole_JobRoleId",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_DepartmentId",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_JobRoleId",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_DepartmentId_IsActive",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_JobRoleId_IsActive",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedAllowance",
                table: "tbl_RestaurantPayrollEmployee",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "JobRole",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE employee
SET Department = department.Name,
    JobRole = jobRole.Name,
    FixedAllowance = COALESCE(allowance.Amount, 0)
FROM tbl_RestaurantPayrollEmployee employee
LEFT JOIN tbl_RestaurantPayrollDepartment department ON department.Id = employee.DepartmentId
LEFT JOIN tbl_RestaurantPayrollJobRole jobRole ON jobRole.Id = employee.JobRoleId
OUTER APPLY (
    SELECT TOP 1 history.Amount
    FROM tbl_RestaurantPayrollAllowanceHistory history
    WHERE history.EmployeeId = employee.Id
    ORDER BY history.EffectiveFrom DESC, history.CreatedAt DESC
) allowance;
");

            migrationBuilder.DropColumn(
                name: "DateOfBirthMiti",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "JobRoleId",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "JoinedOnMiti",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPayrollAllowanceHistory");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPayrollJobRole");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPayrollDepartment");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_Department_IsActive",
                table: "tbl_RestaurantPayrollEmployee",
                columns: new[] { "TenantId", "Department", "IsActive" });
        }
    }
}
