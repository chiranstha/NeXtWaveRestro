using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace NextWave.Erp.Tests.General
{
    // ReSharper disable once InconsistentNaming
    public class ConnectionString_Tests
    {
        [Fact]
        public void SqlConnectionStringBuilder_Test()
        {
            var csb = new SqlConnectionStringBuilder("Server=localhost; Database=Erp; Trusted_Connection=True;");
            csb["Database"].ShouldBe("Erp");
        }
    }
}
