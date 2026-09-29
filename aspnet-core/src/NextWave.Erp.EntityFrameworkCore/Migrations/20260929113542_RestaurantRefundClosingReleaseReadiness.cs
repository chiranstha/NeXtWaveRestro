using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantRefundClosingReleaseReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "tbl_RestaurantOrder",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantClientOperation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClientRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantClientOperation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantRefund",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    RestaurantOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesReturnMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipSalesReturnMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreditNoteSalesReturnMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ItemRefundAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TipRefundAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    CreditNoteAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    SettledAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantRefund", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefund_tbl_RestaurantOrder_RestaurantOrderId",
                        column: x => x.RestaurantOrderId,
                        principalTable: "tbl_RestaurantOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefund_tbl_SalesMaster_SalesMasterId",
                        column: x => x.SalesMasterId,
                        principalTable: "tbl_SalesMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefund_tbl_SalesReturnMaster_CreditNoteSalesReturnMasterId",
                        column: x => x.CreditNoteSalesReturnMasterId,
                        principalTable: "tbl_SalesReturnMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefund_tbl_SalesReturnMaster_SalesReturnMasterId",
                        column: x => x.SalesReturnMasterId,
                        principalTable: "tbl_SalesReturnMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefund_tbl_SalesReturnMaster_TipSalesReturnMasterId",
                        column: x => x.TipSalesReturnMasterId,
                        principalTable: "tbl_SalesReturnMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantSetupAcknowledgement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    CheckKey = table.Column<int>(type: "int", nullable: false),
                    CompletedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantSetupAcknowledgement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantRefundLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    RestockQty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    StockDisposition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantRefundLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefundLine_tbl_RestaurantRefund_RefundId",
                        column: x => x.RefundId,
                        principalTable: "tbl_RestaurantRefund",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantRefundSettlement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    SettledByUserId = table.Column<long>(type: "bigint", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CashShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantRefundSettlement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefundSettlement_tbl_RestaurantCashShift_CashShiftId",
                        column: x => x.CashShiftId,
                        principalTable: "tbl_RestaurantCashShift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefundSettlement_tbl_RestaurantRefund_RefundId",
                        column: x => x.RefundId,
                        principalTable: "tbl_RestaurantRefund",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantRefundTender",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalTenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalBillPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    SettledAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantRefundTender", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefundTender_tbl_RestaurantRefund_RefundId",
                        column: x => x.RefundId,
                        principalTable: "tbl_RestaurantRefund",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantRefundSettlementTender",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundTenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantRefundSettlementTender", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefundSettlementTender_tbl_RestaurantRefundSettlement_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "tbl_RestaurantRefundSettlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantRefundSettlementTender_tbl_RestaurantRefundTender_RefundTenderId",
                        column: x => x.RefundTenderId,
                        principalTable: "tbl_RestaurantRefundTender",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantClientOperation_TenantId_OperationType_CreatedAt",
                table: "tbl_RestaurantClientOperation",
                columns: new[] { "TenantId", "OperationType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantClientOperation_TenantId_UserId_ClientRequestId",
                table: "tbl_RestaurantClientOperation",
                columns: new[] { "TenantId", "UserId", "ClientRequestId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefund_CreditNoteSalesReturnMasterId",
                table: "tbl_RestaurantRefund",
                column: "CreditNoteSalesReturnMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefund_RestaurantOrderId",
                table: "tbl_RestaurantRefund",
                column: "RestaurantOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefund_SalesMasterId",
                table: "tbl_RestaurantRefund",
                column: "SalesMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefund_SalesReturnMasterId",
                table: "tbl_RestaurantRefund",
                column: "SalesReturnMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefund_TenantId_ClientRequestId",
                table: "tbl_RestaurantRefund",
                columns: new[] { "TenantId", "ClientRequestId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefund_TenantId_SalesMasterId_Status",
                table: "tbl_RestaurantRefund",
                columns: new[] { "TenantId", "SalesMasterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefund_TipSalesReturnMasterId",
                table: "tbl_RestaurantRefund",
                column: "TipSalesReturnMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundLine_RefundId",
                table: "tbl_RestaurantRefundLine",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundLine_TenantId_RefundId",
                table: "tbl_RestaurantRefundLine",
                columns: new[] { "TenantId", "RefundId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundLine_TenantId_SalesDetailId",
                table: "tbl_RestaurantRefundLine",
                columns: new[] { "TenantId", "SalesDetailId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundSettlement_CashShiftId",
                table: "tbl_RestaurantRefundSettlement",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundSettlement_RefundId",
                table: "tbl_RestaurantRefundSettlement",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundSettlement_TenantId_ClientRequestId",
                table: "tbl_RestaurantRefundSettlement",
                columns: new[] { "TenantId", "ClientRequestId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundSettlementTender_RefundTenderId",
                table: "tbl_RestaurantRefundSettlementTender",
                column: "RefundTenderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundSettlementTender_SettlementId",
                table: "tbl_RestaurantRefundSettlementTender",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundSettlementTender_TenantId_SettlementId",
                table: "tbl_RestaurantRefundSettlementTender",
                columns: new[] { "TenantId", "SettlementId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundTender_RefundId",
                table: "tbl_RestaurantRefundTender",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantRefundTender_TenantId_RefundId",
                table: "tbl_RestaurantRefundTender",
                columns: new[] { "TenantId", "RefundId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSetupAcknowledgement_TenantId_CheckKey",
                table: "tbl_RestaurantSetupAcknowledgement",
                columns: new[] { "TenantId", "CheckKey" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_RestaurantClientOperation");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantRefundLine");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantRefundSettlementTender");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantSetupAcknowledgement");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantRefundSettlement");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantRefundTender");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantRefund");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "tbl_RestaurantOrder");
        }
    }
}
