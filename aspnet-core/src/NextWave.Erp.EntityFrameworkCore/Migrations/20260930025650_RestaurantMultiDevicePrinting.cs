using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantMultiDevicePrinting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPrintDelivery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrintJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LeaseOwner = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentJobId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrintedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPrintDelivery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPrintDevice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientDeviceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPrintDevice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPrintDeviceRoute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPrintDeviceRoute", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPrintRoute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPrintRoute", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPrintDelivery_TenantId_DeviceId_Status_CreatedAtUtc",
                table: "tbl_RestaurantPrintDelivery",
                columns: new[] { "TenantId", "DeviceId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPrintDelivery_TenantId_PrintJobId_DeviceId",
                table: "tbl_RestaurantPrintDelivery",
                columns: new[] { "TenantId", "PrintJobId", "DeviceId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPrintDevice_TenantId_ClientDeviceId",
                table: "tbl_RestaurantPrintDevice",
                columns: new[] { "TenantId", "ClientDeviceId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPrintDeviceRoute_TenantId_DeviceId_RouteName",
                table: "tbl_RestaurantPrintDeviceRoute",
                columns: new[] { "TenantId", "DeviceId", "RouteName" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPrintDeviceRoute_TenantId_RouteName",
                table: "tbl_RestaurantPrintDeviceRoute",
                columns: new[] { "TenantId", "RouteName" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPrintRoute_TenantId_Name",
                table: "tbl_RestaurantPrintRoute",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.Sql("UPDATE tbl_RestaurantPrintJob SET Status = 3, LastError = N'Retry this job after registering a print device.' WHERE Status IN (0, 1) AND TenantId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE tbl_RestaurantPrintJob SET Status = 0, LastError = NULL WHERE Status = 3 AND LastError = N'Retry this job after registering a print device.' AND TenantId IS NOT NULL");
            migrationBuilder.DropTable(
                name: "tbl_RestaurantPrintDelivery");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPrintDevice");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPrintDeviceRoute");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPrintRoute");
        }
    }
}
