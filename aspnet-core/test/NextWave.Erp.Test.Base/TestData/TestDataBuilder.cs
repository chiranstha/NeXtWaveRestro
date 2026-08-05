using NextWave.Erp.EntityFrameworkCore;

namespace NextWave.Erp.Test.Base.TestData
{
    public class TestDataBuilder
    {
        private readonly ErpDbContext _context;
        private readonly int _tenantId;

        public TestDataBuilder(ErpDbContext context, int tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            new TestOrganizationUnitsBuilder(_context, _tenantId).Create();
            new TestSubscriptionPaymentBuilder(_context, _tenantId).Create();
            new TestEditionsBuilder(_context).Create();

            _context.SaveChanges();
        }
    }
}
