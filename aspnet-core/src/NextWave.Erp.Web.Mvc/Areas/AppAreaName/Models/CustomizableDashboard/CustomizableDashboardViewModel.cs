using NextWave.Erp.DashboardCustomization;
using NextWave.Erp.DashboardCustomization.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.CustomizableDashboard;

public class CustomizableDashboardViewModel
{
    public DashboardOutput DashboardOutput { get; }

    public Dashboard UserDashboard { get; }

    public CustomizableDashboardViewModel(
        DashboardOutput dashboardOutput,
        Dashboard userDashboard)
    {
        DashboardOutput = dashboardOutput;
        UserDashboard = userDashboard;
    }
}

