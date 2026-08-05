using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NextWave.Erp.EntityFrameworkCore;

namespace NextWave.Erp.HealthChecks;

public class ErpDbContextHealthCheck : IHealthCheck
{
    private readonly DatabaseCheckHelper _checkHelper;

    public ErpDbContextHealthCheck(DatabaseCheckHelper checkHelper)
    {
        _checkHelper = checkHelper;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_checkHelper.Exist("db"))
        {
            return Task.FromResult(HealthCheckResult.Healthy("ErpDbContext connected to database."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("ErpDbContext could not connect to database"));
    }
}
