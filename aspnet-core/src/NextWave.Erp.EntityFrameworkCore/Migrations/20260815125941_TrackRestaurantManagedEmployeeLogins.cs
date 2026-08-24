using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class TrackRestaurantManagedEmployeeLogins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LoginManagedByRestaurant",
                table: "tbl_RestaurantPayrollEmployee",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginManagedByRestaurant",
                table: "tbl_RestaurantPayrollEmployee");
        }
    }
}
