using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantDeviceSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasConflict",
                table: "tbl_RestaurantDevice",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "LastAcknowledgedSeq",
                table: "tbl_RestaurantDevice",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LastPulledSeq",
                table: "tbl_RestaurantDevice",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "tbl_RestaurantDevice",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "tbl_RestaurantDevice",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasConflict",
                table: "tbl_RestaurantDevice");

            migrationBuilder.DropColumn(
                name: "LastAcknowledgedSeq",
                table: "tbl_RestaurantDevice");

            migrationBuilder.DropColumn(
                name: "LastPulledSeq",
                table: "tbl_RestaurantDevice");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "tbl_RestaurantDevice");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "tbl_RestaurantDevice");
        }
    }
}
