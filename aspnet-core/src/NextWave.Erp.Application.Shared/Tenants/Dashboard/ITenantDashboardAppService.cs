using Abp.Application.Services;
using NextWave.Erp.Tenants.Dashboard.Dto;
using System.Threading.Tasks;

namespace NextWave.Erp.Tenants.Dashboard;

public interface ITenantDashboardAppService : IApplicationService
{
    GetMemberActivityOutput GetMemberActivity();

    GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input);

    GetDailySalesOutput GetDailySales();

    GetProfitShareOutput GetProfitShare();

    GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input);

    GetTopStatsOutput GetTopStats();

    GetRegionalStatsOutput GetRegionalStats();

    GetGeneralStatsOutput GetGeneralStats();

    Task<GetRealDashboardDataOutput> GetRealDashboardData();
}

