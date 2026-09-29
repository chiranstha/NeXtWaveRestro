using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantPosOrderRetryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosClientRequestId",
                table: "tbl_RestaurantOrder",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantOrder_TenantId_PosClientRequestId",
                table: "tbl_RestaurantOrder",
                columns: new[] { "TenantId", "PosClientRequestId" },
                unique: true,
                filter: "[PosClientRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_RestaurantOrder_TenantId_PosClientRequestId",
                table: "tbl_RestaurantOrder");

            migrationBuilder.DropColumn(
                name: "PosClientRequestId",
                table: "tbl_RestaurantOrder");
        }
    }
}
