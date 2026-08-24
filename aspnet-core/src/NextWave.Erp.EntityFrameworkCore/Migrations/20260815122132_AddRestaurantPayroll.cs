using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPayrollEmployee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    StaffCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JobRole = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    OvertimeRate = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    FixedAllowance = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    FixedDeduction = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ServiceChargeWeight = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PanNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SsfNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    JoinedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPayrollEmployee", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPayrollRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RunNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TipsPool = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ServiceChargePool = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TotalGross = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TotalDeduction = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TotalNet = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPayrollRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPayrollAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false),
                    RegularHours = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CapturedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPayrollAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantPayrollAttendance_tbl_RestaurantPayrollEmployee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "tbl_RestaurantPayrollEmployee",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPayrollLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StaffCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    JobRole = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    WorkedHours = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    BasicPay = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    OvertimePay = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Allowance = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TipsShare = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ServiceChargeShare = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TaxDeduction = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    OtherDeduction = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    AdvanceRecovery = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPayrollLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantPayrollLine_tbl_RestaurantPayrollEmployee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "tbl_RestaurantPayrollEmployee",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantPayrollLine_tbl_RestaurantPayrollRun_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "tbl_RestaurantPayrollRun",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollAttendance_EmployeeId",
                table: "tbl_RestaurantPayrollAttendance",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollAttendance_TenantId_EmployeeId_WorkDate",
                table: "tbl_RestaurantPayrollAttendance",
                columns: new[] { "TenantId", "EmployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollAttendance_TenantId_WorkDate_Status",
                table: "tbl_RestaurantPayrollAttendance",
                columns: new[] { "TenantId", "WorkDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_Department_IsActive",
                table: "tbl_RestaurantPayrollEmployee",
                columns: new[] { "TenantId", "Department", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_StaffCode",
                table: "tbl_RestaurantPayrollEmployee",
                columns: new[] { "TenantId", "StaffCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollEmployee_TenantId_UserId",
                table: "tbl_RestaurantPayrollEmployee",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollLine_EmployeeId",
                table: "tbl_RestaurantPayrollLine",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollLine_PayrollRunId",
                table: "tbl_RestaurantPayrollLine",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollLine_TenantId_EmployeeId",
                table: "tbl_RestaurantPayrollLine",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollLine_TenantId_PayrollRunId_EmployeeId",
                table: "tbl_RestaurantPayrollLine",
                columns: new[] { "TenantId", "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollRun_TenantId_PeriodStart_PeriodEnd",
                table: "tbl_RestaurantPayrollRun",
                columns: new[] { "TenantId", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollRun_TenantId_RunNumber",
                table: "tbl_RestaurantPayrollRun",
                columns: new[] { "TenantId", "RunNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPayrollRun_TenantId_Status_PeriodEnd",
                table: "tbl_RestaurantPayrollRun",
                columns: new[] { "TenantId", "Status", "PeriodEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_RestaurantPayrollAttendance");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPayrollLine");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPayrollRun");
        }
    }
}
