using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialPostingTraceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceDetailId",
                table: "tbl_StockPosting",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceDocumentId",
                table: "tbl_SalesMaster",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceModule",
                table: "tbl_SalesMaster",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceDetailId",
                table: "tbl_StockPosting");

            migrationBuilder.DropColumn(
                name: "SourceDocumentId",
                table: "tbl_SalesMaster");

            migrationBuilder.DropColumn(
                name: "SourceModule",
                table: "tbl_SalesMaster");
        }
    }
}
