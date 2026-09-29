using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantCashierSessionsAndTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrintConfirmedAt",
                table: "tbl_RestaurantTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintRequestedAt",
                table: "tbl_RestaurantTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientPayloadHash",
                table: "tbl_RestaurantOrder",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestClientRequestId",
                table: "tbl_RestaurantOrder",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestStatusTokenHash",
                table: "tbl_RestaurantOrder",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashShiftId",
                table: "tbl_RestaurantBillPayment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "tbl_RestaurantBillPayment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOrderFullyBilled",
                table: "tbl_RestaurantBillPayment",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "tbl_RestaurantBillPayment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingGrandTotal",
                table: "tbl_RestaurantBillPayment",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "tbl_RestaurantBillPayment",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantCashShift",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OpenedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CountedClosingCash = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    ExpectedClosingCash = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    CashVariance = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    CloseNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantCashShift", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantBillTender",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantBillTender", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantBillTender_tbl_RestaurantBillPayment_BillPaymentId",
                        column: x => x.BillPaymentId,
                        principalTable: "tbl_RestaurantBillPayment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantBillTender_tbl_RestaurantCashShift_CashShiftId",
                        column: x => x.CashShiftId,
                        principalTable: "tbl_RestaurantCashShift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantCashMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCashIn = table.Column<bool>(type: "bit", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantCashMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantCashMovement_tbl_RestaurantCashShift_CashShiftId",
                        column: x => x.CashShiftId,
                        principalTable: "tbl_RestaurantCashShift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrder_TenantId_GuestClientRequestId",
                table: "tbl_RestaurantOrder",
                columns: new[] { "TenantId", "GuestClientRequestId" },
                unique: true,
                filter: "[GuestClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillPayment_CashShiftId",
                table: "tbl_RestaurantBillPayment",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillPayment_TenantId_ClientRequestId",
                table: "tbl_RestaurantBillPayment",
                columns: new[] { "TenantId", "ClientRequestId" },
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillTender_BillPaymentId",
                table: "tbl_RestaurantBillTender",
                column: "BillPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillTender_CashShiftId",
                table: "tbl_RestaurantBillTender",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillTender_TenantId_BillPaymentId",
                table: "tbl_RestaurantBillTender",
                columns: new[] { "TenantId", "BillPaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillTender_TenantId_CashShiftId",
                table: "tbl_RestaurantBillTender",
                columns: new[] { "TenantId", "CashShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantCashMovement_CashShiftId",
                table: "tbl_RestaurantCashMovement",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantCashMovement_TenantId_CashShiftId_CreatedAt",
                table: "tbl_RestaurantCashMovement",
                columns: new[] { "TenantId", "CashShiftId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantCashShift_TenantId_OpenedByUserId",
                table: "tbl_RestaurantCashShift",
                columns: new[] { "TenantId", "OpenedByUserId" },
                unique: true,
                filter: "[IsClosed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantCashShift_TenantId_RegisterName",
                table: "tbl_RestaurantCashShift",
                columns: new[] { "TenantId", "RegisterName" },
                unique: true,
                filter: "[IsClosed] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_RestaurantBillPayment_tbl_RestaurantCashShift_CashShiftId",
                table: "tbl_RestaurantBillPayment",
                column: "CashShiftId",
                principalTable: "tbl_RestaurantCashShift",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_RestaurantBillPayment_tbl_RestaurantCashShift_CashShiftId",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantBillTender");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantCashMovement");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantCashShift");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantOrder_TenantId_GuestClientRequestId",
                table: "tbl_RestaurantOrder");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantBillPayment_CashShiftId",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantBillPayment_TenantId_ClientRequestId",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropColumn(
                name: "LastPrintConfirmedAt",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "PrintRequestedAt",
                table: "tbl_RestaurantTicket");

            migrationBuilder.DropColumn(
                name: "ClientPayloadHash",
                table: "tbl_RestaurantOrder");

            migrationBuilder.DropColumn(
                name: "GuestClientRequestId",
                table: "tbl_RestaurantOrder");

            migrationBuilder.DropColumn(
                name: "GuestStatusTokenHash",
                table: "tbl_RestaurantOrder");

            migrationBuilder.DropColumn(
                name: "CashShiftId",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropColumn(
                name: "IsOrderFullyBilled",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropColumn(
                name: "RemainingGrandTotal",
                table: "tbl_RestaurantBillPayment");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "tbl_RestaurantBillPayment");
        }
    }
}
