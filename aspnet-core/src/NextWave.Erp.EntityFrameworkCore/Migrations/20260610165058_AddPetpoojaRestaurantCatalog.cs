using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddPetpoojaRestaurantCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FireSequence",
                table: "tbl_RestaurantOrderItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ItemNameSnapshot",
                table: "tbl_RestaurantOrderItem",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ModifierTotal",
                table: "tbl_RestaurantOrderItem",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StationTypeSnapshot",
                table: "tbl_RestaurantOrderItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceSnapshot",
                table: "tbl_RestaurantOrderItem",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "tbl_RestaurantOrderItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameSnapshot",
                table: "tbl_RestaurantOrderItem",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "tbl_RestaurantMenuItem",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "tbl_RestaurantMenuItem",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasModifiers",
                table: "tbl_RestaurantMenuItem",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasVariants",
                table: "tbl_RestaurantMenuItem",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "tbl_RestaurantMenuItem",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "tbl_RestaurantMenuItem",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "tbl_RestaurantMenuItem",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVeg",
                table: "tbl_RestaurantMenuItem",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "tbl_RestaurantMenuItem",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpiceLevel",
                table: "tbl_RestaurantMenuItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnavailableUntil",
                table: "tbl_RestaurantMenuItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantMenuItemTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ColorHex = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantMenuItemTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuItemTag_tbl_RestaurantMenuItem_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "tbl_RestaurantMenuItem",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantMenuVariant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PriceDelta = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    IsAbsolutePrice = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantMenuVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuVariant_tbl_RestaurantMenuItem_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "tbl_RestaurantMenuItem",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantModifierGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    MinSelect = table.Column<int>(type: "int", nullable: false),
                    MaxSelect = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantModifierGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantMenuItemModifierGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifierGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantMenuItemModifierGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuItemModifierGroup_tbl_RestaurantMenuItem_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "tbl_RestaurantMenuItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuItemModifierGroup_tbl_RestaurantModifierGroup_ModifierGroupId",
                        column: x => x.ModifierGroupId,
                        principalTable: "tbl_RestaurantModifierGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifierGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PriceDelta = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantModifier_tbl_RestaurantModifierGroup_ModifierGroupId",
                        column: x => x.ModifierGroupId,
                        principalTable: "tbl_RestaurantModifierGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantOrderItemModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifierNameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PriceDelta = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantOrderItemModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItemModifier_tbl_RestaurantModifier_ModifierId",
                        column: x => x.ModifierId,
                        principalTable: "tbl_RestaurantModifier",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantOrderItemModifier_tbl_RestaurantOrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "tbl_RestaurantOrderItem",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItem_VariantId",
                table: "tbl_RestaurantOrderItem",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuItemModifierGroup_MenuItemId",
                table: "tbl_RestaurantMenuItemModifierGroup",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuItemModifierGroup_ModifierGroupId",
                table: "tbl_RestaurantMenuItemModifierGroup",
                column: "ModifierGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuItemTag_MenuItemId",
                table: "tbl_RestaurantMenuItemTag",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuVariant_MenuItemId",
                table: "tbl_RestaurantMenuVariant",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantModifier_ModifierGroupId",
                table: "tbl_RestaurantModifier",
                column: "ModifierGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItemModifier_ModifierId",
                table: "tbl_RestaurantOrderItemModifier",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrderItemModifier_OrderItemId",
                table: "tbl_RestaurantOrderItemModifier",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_RestaurantOrderItem_tbl_RestaurantMenuVariant_VariantId",
                table: "tbl_RestaurantOrderItem",
                column: "VariantId",
                principalTable: "tbl_RestaurantMenuVariant",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_RestaurantOrderItem_tbl_RestaurantMenuVariant_VariantId",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantMenuItemModifierGroup");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantMenuItemTag");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantMenuVariant");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantOrderItemModifier");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantModifier");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantModifierGroup");

            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantOrderItem_VariantId",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "FireSequence",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "ItemNameSnapshot",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "ModifierTotal",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "StationTypeSnapshot",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "UnitPriceSnapshot",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "VariantNameSnapshot",
                table: "tbl_RestaurantOrderItem");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "HasModifiers",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "HasVariants",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "IsVeg",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "SpiceLevel",
                table: "tbl_RestaurantMenuItem");

            migrationBuilder.DropColumn(
                name: "UnavailableUntil",
                table: "tbl_RestaurantMenuItem");
        }
    }
}
