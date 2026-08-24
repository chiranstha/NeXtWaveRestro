using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextWave.Erp.EntityFrameworkCore;

#nullable disable

namespace NextWave.Erp.Migrations
{
    [DbContext(typeof(ErpDbContext))]
    [Migration("20260817123000_RemoveReportFinancialYearSelectionPermission")]
    public partial class RemoveReportFinancialYearSelectionPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM AbpPermissions
WHERE [Name] = N'Pages.Reporting.SelectFinancialYear';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The removed permission is obsolete and must not be recreated.
        }
    }
}
