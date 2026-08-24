using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRestaurantManagedEmployeeUsernames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE employeeUser
SET UserName = LOWER(employeeUser.EmailAddress),
    NormalizedUserName = UPPER(employeeUser.EmailAddress),
    SecurityStamp = CONVERT(nvarchar(40), NEWID())
FROM AbpUsers employeeUser
INNER JOIN tbl_RestaurantPayrollEmployee employee
    ON employee.UserId = employeeUser.Id
   AND employee.TenantId = employeeUser.TenantId
CROSS APPLY (
    SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(employeeUser.UserName, '+', ''), ' ', ''), '-', ''), '(', ''), ')', '') AS PhoneDigits
) normalized
WHERE employee.LoginManagedByRestaurant = 1
  AND employeeUser.IsDeleted = 0
  AND employeeUser.EmailAddress LIKE '%_@_%._%'
  AND employeeUser.UserName NOT LIKE '%_@_%._%'
  AND NOT (
      LEN(normalized.PhoneDigits) BETWEEN 7 AND 15
      AND normalized.PhoneDigits NOT LIKE '%[^0-9]%'
  )
  AND NOT EXISTS (
      SELECT 1
      FROM AbpUsers existingUser
      WHERE existingUser.TenantId = employeeUser.TenantId
        AND existingUser.Id <> employeeUser.Id
        AND existingUser.IsDeleted = 0
        AND existingUser.NormalizedUserName = UPPER(employeeUser.EmailAddress)
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Legacy free-form usernames cannot be reconstructed safely.
        }
    }
}
