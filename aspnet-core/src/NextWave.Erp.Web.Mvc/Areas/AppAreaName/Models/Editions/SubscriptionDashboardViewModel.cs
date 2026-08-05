using NextWave.Erp.MultiTenancy.Dto;
using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Editions;

public class SubscriptionDashboardViewModel
{
    public GetCurrentLoginInformationsOutput LoginInformations { get; set; }

    public EditionsSelectOutput Editions { get; set; }
}

