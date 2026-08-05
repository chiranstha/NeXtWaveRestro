using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantInventoryCostingFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SalesDetailId",
                table: "tbl_RestaurantBillLine",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantStockAdjustment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantStockAdjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantStockAdjustment_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantStockAdjustment_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantSupplierItemMapping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    MinimumOrderQty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantSupplierItemMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantSupplierItemMapping_tbl_AccountLedger_SupplierLedgerId",
                        column: x => x.SupplierLedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantSupplierItemMapping_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantSupplierItemMapping_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantStockAdjustmentLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockAdjustmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    SystemQty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    CountedQty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StockPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantStockAdjustmentLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantStockAdjustmentLine_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantStockAdjustmentLine_tbl_RestaurantStockAdjustment_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalTable: "tbl_RestaurantStockAdjustment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantStockAdjustmentLine_tbl_StockPosting_StockPostingId",
                        column: x => x.StockPostingId,
                        principalTable: "tbl_StockPosting",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantStockAdjustmentLine_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantBillLine_SalesDetailId",
                table: "tbl_RestaurantBillLine",
                column: "SalesDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustment_CreateUserId",
                table: "tbl_RestaurantStockAdjustment",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustment_TenantId_AdjustmentType",
                table: "tbl_RestaurantStockAdjustment",
                columns: new[] { "TenantId", "AdjustmentType" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustment_TenantId_Date",
                table: "tbl_RestaurantStockAdjustment",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustment_VoucherTypeId",
                table: "tbl_RestaurantStockAdjustment",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustmentLine_ProductId",
                table: "tbl_RestaurantStockAdjustmentLine",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustmentLine_StockAdjustmentId",
                table: "tbl_RestaurantStockAdjustmentLine",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustmentLine_StockPostingId",
                table: "tbl_RestaurantStockAdjustmentLine",
                column: "StockPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustmentLine_TenantId_ProductId",
                table: "tbl_RestaurantStockAdjustmentLine",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustmentLine_TenantId_StockAdjustmentId",
                table: "tbl_RestaurantStockAdjustmentLine",
                columns: new[] { "TenantId", "StockAdjustmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantStockAdjustmentLine_UnitId",
                table: "tbl_RestaurantStockAdjustmentLine",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSupplierItemMapping_PreferredActive",
                table: "tbl_RestaurantSupplierItemMapping",
                columns: new[] { "TenantId", "ProductId" },
                unique: true,
                filter: "[IsPreferred] = 1 AND [IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSupplierItemMapping_ProductId",
                table: "tbl_RestaurantSupplierItemMapping",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSupplierItemMapping_SupplierLedgerId",
                table: "tbl_RestaurantSupplierItemMapping",
                column: "SupplierLedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSupplierItemMapping_TenantId_SupplierLedgerId",
                table: "tbl_RestaurantSupplierItemMapping",
                columns: new[] { "TenantId", "SupplierLedgerId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSupplierItemMapping_UnitId",
                table: "tbl_RestaurantSupplierItemMapping",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_RestaurantBillLine_tbl_SalesDetails_SalesDetailId",
                table: "tbl_RestaurantBillLine",
                column: "SalesDetailId",
                principalTable: "tbl_SalesDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_RestaurantBillLine_tbl_SalesDetails_SalesDetailId",
                table: "tbl_RestaurantBillLine");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantStockAdjustmentLine");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantSupplierItemMapping");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantStockAdjustment");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantBillLine_SalesDetailId",
                table: "tbl_RestaurantBillLine");

            migrationBuilder.DropColumn(
                name: "SalesDetailId",
                table: "tbl_RestaurantBillLine");
        }
    }
}
