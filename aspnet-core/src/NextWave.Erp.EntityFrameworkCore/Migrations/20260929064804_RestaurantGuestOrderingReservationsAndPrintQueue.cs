using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantGuestOrderingReservationsAndPrintQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrTokenHash",
                table: "tbl_RestaurantTable",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrTokenUpdatedAt",
                table: "tbl_RestaurantTable",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrintRouteName",
                table: "tbl_RestaurantStation",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuestApprovalStatus",
                table: "tbl_RestaurantOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestRejectionReason",
                table: "tbl_RestaurantOrder",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPrintJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalJobId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RouteName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    LeaseOwner = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AgentJobId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ReprintReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeliberateReprint = table.Column<bool>(type: "bit", nullable: false),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrintedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPrintJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantReservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsWalkIn = table.Column<bool>(type: "bit", nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PartySize = table.Column<int>(type: "int", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestStatusTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantReservation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantReservation_tbl_RestaurantTable_TableId",
                        column: x => x.TableId,
                        principalTable: "tbl_RestaurantTable",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantReservationOtpChallenge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantReservationOtpChallenge", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantSmsOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantSmsOutbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantReservation_TableId",
                table: "tbl_RestaurantReservation",
                column: "TableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_RestaurantPrintJob");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantReservation");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantReservationOtpChallenge");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantSmsOutbox");

            migrationBuilder.DropColumn(
                name: "QrTokenHash",
                table: "tbl_RestaurantTable");

            migrationBuilder.DropColumn(
                name: "QrTokenUpdatedAt",
                table: "tbl_RestaurantTable");

            migrationBuilder.DropColumn(
                name: "PrintRouteName",
                table: "tbl_RestaurantStation");

            migrationBuilder.DropColumn(
                name: "GuestApprovalStatus",
                table: "tbl_RestaurantOrder");

            migrationBuilder.DropColumn(
                name: "GuestRejectionReason",
                table: "tbl_RestaurantOrder");
        }
    }
}
