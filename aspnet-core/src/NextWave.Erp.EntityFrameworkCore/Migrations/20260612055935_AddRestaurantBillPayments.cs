using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantBillPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_RestaurantBillPayment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TipAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    PayableAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    CustomerPaidAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ReturnAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantBillPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantBillPayment_tbl_RestaurantOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "tbl_RestaurantOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantBillPayment_tbl_SalesMaster_SalesMasterId",
                        column: x => x.SalesMasterId,
                        principalTable: "tbl_SalesMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillPayment_OrderId",
                table: "tbl_RestaurantBillPayment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillPayment_SalesMasterId",
                table: "tbl_RestaurantBillPayment",
                column: "SalesMasterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_RestaurantBillPayment");
        }
    }
}
