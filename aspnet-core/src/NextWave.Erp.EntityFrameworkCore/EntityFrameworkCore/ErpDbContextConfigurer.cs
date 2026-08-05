using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace NextWave.Erp.EntityFrameworkCore;

public static class ErpDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<ErpDbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<ErpDbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}

