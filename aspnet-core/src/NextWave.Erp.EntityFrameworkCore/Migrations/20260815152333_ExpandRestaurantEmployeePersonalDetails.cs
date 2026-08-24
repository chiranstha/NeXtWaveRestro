using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class ExpandRestaurantEmployeePersonalDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CitizenshipNumber",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "tbl_RestaurantPayrollEmployee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailAddress",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "tbl_RestaurantPayrollEmployee",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "CitizenshipNumber",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "EmailAddress",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "tbl_RestaurantPayrollEmployee");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "tbl_RestaurantPayrollEmployee");
        }
    }
}
