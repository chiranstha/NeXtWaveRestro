using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantKotBotOperationalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "tbl_RestaurantTicketItem",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "tbl_RestaurantTicket",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "tbl_RestaurantTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrintedAt",
                table: "tbl_RestaurantTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintCount",
                table: "tbl_RestaurantTicket",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintedAt",
                table: "tbl_RestaurantTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "tbl_RestaurantTicket",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServedAt",
                table: "tbl_RestaurantTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "tbl_RestaurantTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "tbl_RestaurantOrderItem",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "tbl_RestaurantTicketItem");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "LastPrintedAt",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "PrintCount",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "PrintedAt",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "ServedAt",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "tbl_RestaurantOrderItem");
        }
    }
}
