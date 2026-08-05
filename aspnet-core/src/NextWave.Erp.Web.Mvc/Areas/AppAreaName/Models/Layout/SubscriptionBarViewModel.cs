using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;

public class SubscriptionBarViewModel
{
    public int SubscriptionExpireNotifyDayCount { get; set; }

    public GetCurrentLoginInformationsOutput LoginInformations { get; set; }

    public string CssClass { get; set; }
}

