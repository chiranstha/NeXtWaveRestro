using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantManagementMaterialBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostRate",
                table: "tbl_Bom",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "tbl_Bom",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WastagePercentage",
                table: "tbl_Bom",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Parameters",
                table: "AbpAuditLogs",
                type: "nvarchar(max)",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantArea",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantArea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantDevice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantDevice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantMenuCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantMenuCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantStation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StationType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantStation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantTable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantTable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantTable_tbl_RestaurantArea_AreaId",
                        column: x => x.AreaId,
                        principalTable: "tbl_RestaurantArea",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantSyncUpload",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayloadHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ServerReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantSyncUpload", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantSyncUpload_tbl_RestaurantDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "tbl_RestaurantDevice",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantMenuItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    PreparationMinutes = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantMenuItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuItem_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuItem_tbl_RestaurantMenuCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "tbl_RestaurantMenuCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuItem_tbl_RestaurantStation_StationId",
                        column: x => x.StationId,
                        principalTable: "tbl_RestaurantStation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantTableSession",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    WaiterUserId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhoneNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantTableSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantTableSession_tbl_RestaurantTable_TableId",
                        column: x => x.TableId,
                        principalTable: "tbl_RestaurantTable",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TableSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WaiterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClientRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhoneNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    SalesMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BilledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrder_tbl_RestaurantTableSession_TableSessionId",
                        column: x => x.TableSessionId,
                        principalTable: "tbl_RestaurantTableSession",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrder_tbl_RestaurantTable_TableId",
                        column: x => x.TableId,
                        principalTable: "tbl_RestaurantTable",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrder_tbl_SalesMaster_SalesMasterId",
                        column: x => x.SalesMasterId,
                        principalTable: "tbl_SalesMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantOrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItem_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItem_tbl_RestaurantMenuItem_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "tbl_RestaurantMenuItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItem_tbl_RestaurantOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "tbl_RestaurantOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItem_tbl_RestaurantStation_StationId",
                        column: x => x.StationId,
                        principalTable: "tbl_RestaurantStation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItem_tbl_Tax_TaxId",
                        column: x => x.TaxId,
                        principalTable: "tbl_Tax",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItem_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantTicket",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantTicket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantTicket_tbl_RestaurantOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "tbl_RestaurantOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantTicket_tbl_RestaurantStation_StationId",
                        column: x => x.StationId,
                        principalTable: "tbl_RestaurantStation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantTicketItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantTicketItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantTicketItem_tbl_RestaurantOrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "tbl_RestaurantOrderItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantTicketItem_tbl_RestaurantTicket_TicketId",
                        column: x => x.TicketId,
                        principalTable: "tbl_RestaurantTicket",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Bom_RawMaterialId",
                table: "tbl_Bom",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuItem_CategoryId",
                table: "tbl_RestaurantMenuItem",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuItem_ProductId",
                table: "tbl_RestaurantMenuItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuItem_StationId",
                table: "tbl_RestaurantMenuItem",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrder_SalesMasterId",
                table: "tbl_RestaurantOrder",
                column: "SalesMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrder_TableId",
                table: "tbl_RestaurantOrder",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrder_TableSessionId",
                table: "tbl_RestaurantOrder",
                column: "TableSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItem_MenuItemId",
                table: "tbl_RestaurantOrderItem",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItem_OrderId",
                table: "tbl_RestaurantOrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItem_ProductId",
                table: "tbl_RestaurantOrderItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItem_StationId",
                table: "tbl_RestaurantOrderItem",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItem_TaxId",
                table: "tbl_RestaurantOrderItem",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItem_UnitId",
                table: "tbl_RestaurantOrderItem",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSyncUpload_DeviceId",
                table: "tbl_RestaurantSyncUpload",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantTable_AreaId",
                table: "tbl_RestaurantTable",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantTableSession_TableId",
                table: "tbl_RestaurantTableSession",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantTicket_OrderId",
                table: "tbl_RestaurantTicket",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantTicket_StationId",
                table: "tbl_RestaurantTicket",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantTicketItem_OrderItemId",
                table: "tbl_RestaurantTicketItem",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantTicketItem_TicketId",
                table: "tbl_RestaurantTicketItem",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_Bom_tbl_Product_RawMaterialId",
                table: "tbl_Bom",
                column: "RawMaterialId",
                principalTable: "tbl_Product",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_Bom_tbl_Product_RawMaterialId",
                table: "tbl_Bom");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantSyncUpload");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantTicketItem");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantDevice");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantOrderItem");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantTicket");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantMenuItem");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantOrder");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantMenuCategory");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantStation");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantTableSession");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantTable");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantArea");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Bom_RawMaterialId",
                table: "tbl_Bom");

            migrationBuilder.DropColumn(
                name: "CostRate",
                table: "tbl_Bom");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "tbl_Bom");

            migrationBuilder.DropColumn(
                name: "WastagePercentage",
                table: "tbl_Bom");

            migrationBuilder.AlterColumn<string>(
                name: "Parameters",
                table: "AbpAuditLogs",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 4096,
                oldNullable: true);
        }
    }
}
