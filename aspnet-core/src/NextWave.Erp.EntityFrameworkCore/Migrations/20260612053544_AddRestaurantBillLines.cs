using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantBillLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_RestaurantBillLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ModifierTotal = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    BilledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantBillLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantBillLine_tbl_RestaurantOrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "tbl_RestaurantOrderItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantBillLine_tbl_RestaurantOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "tbl_RestaurantOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantBillLine_tbl_SalesMaster_SalesMasterId",
                        column: x => x.SalesMasterId,
                        principalTable: "tbl_SalesMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillLine_OrderId",
                table: "tbl_RestaurantBillLine",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillLine_OrderItemId",
                table: "tbl_RestaurantBillLine",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillLine_SalesMasterId",
                table: "tbl_RestaurantBillLine",
                column: "SalesMasterId");

            migrationBuilder.Sql(@"
INSERT INTO tbl_RestaurantBillLine
    (Id, OrderId, OrderItemId, SalesMasterId, Qty, GrossAmount, ModifierTotal, DiscountAmount, TaxAmount, NetAmount, Amount, BilledAt, TenantId)
SELECT
    NEWID(),
    o.Id,
    i.Id,
    o.SalesMasterId,
    i.Qty,
    ((i.Qty * i.Rate) + i.ModifierTotal),
    i.ModifierTotal,
    i.DiscountAmount,
    i.TaxAmount,
    i.NetAmount,
    i.Amount,
    COALESCE(o.BilledAt, o.CreatedAt, i.CreatedAt, GETDATE()),
    i.TenantId
FROM tbl_RestaurantOrder o
INNER JOIN tbl_RestaurantOrderItem i ON i.OrderId = o.Id
WHERE o.SalesMasterId IS NOT NULL
  AND i.Status <> 5
  AND i.Qty > 0
  AND NOT EXISTS (
      SELECT 1
      FROM tbl_RestaurantBillLine bl
      WHERE bl.OrderItemId = i.Id
        AND bl.SalesMasterId = o.SalesMasterId
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_RestaurantBillLine");
        }
    }
}
