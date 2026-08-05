using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextWave.Erp.EntityFrameworkCore;

#nullable disable

namespace NextWave.Erp.Migrations
{
    [DbContext(typeof(ErpDbContext))]
    [Migration("20260610193000_FixRestaurantMenuAvailabilityDefault")]
    public partial class FixRestaurantMenuAvailabilityDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'tbl_RestaurantMenuItem', N'IsAvailable') IS NOT NULL
BEGIN
    UPDATE tbl_RestaurantMenuItem
    SET IsAvailable = 1
    WHERE IsAvailable = 0
      AND (UnavailableUntil IS NULL OR UnavailableUntil <= SYSDATETIME());

    DECLARE @constraintName nvarchar(200);

    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = N'tbl_RestaurantMenuItem'
      AND c.name = N'IsAvailable';

    IF @constraintName IS NOT NULL
        EXEC(N'ALTER TABLE tbl_RestaurantMenuItem DROP CONSTRAINT [' + @constraintName + N']');

    ALTER TABLE tbl_RestaurantMenuItem
    ADD CONSTRAINT DF_tbl_RestaurantMenuItem_IsAvailable DEFAULT(1) FOR IsAvailable;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'tbl_RestaurantMenuItem', N'IsAvailable') IS NOT NULL
BEGIN
    DECLARE @constraintName nvarchar(200);

    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = N'tbl_RestaurantMenuItem'
      AND c.name = N'IsAvailable';

    IF @constraintName IS NOT NULL
        EXEC(N'ALTER TABLE tbl_RestaurantMenuItem DROP CONSTRAINT [' + @constraintName + N']');

    ALTER TABLE tbl_RestaurantMenuItem
    ADD CONSTRAINT DF_tbl_RestaurantMenuItem_IsAvailable DEFAULT(0) FOR IsAvailable;
END");
        }
    }
}
