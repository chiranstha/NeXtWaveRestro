using System.Threading.Tasks;
using Abp.Configuration;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Configuration;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;
using NextWave.Erp.Web.Session;
using NextWave.Erp.Web.Views;

namespace NextWave.Erp.Web.Areas.AppAreaName.Views.Shared.Components.AppAreaNameSubscriptionBar;

public class AppAreaNameSubscriptionBarViewComponent : ErpViewComponent
{
    private readonly IPerRequestSessionCache _sessionCache;

    public AppAreaNameSubscriptionBarViewComponent(
        IPerRequestSessionCache sessionCache)
    {
        _sessionCache = sessionCache;
    }

    public async Task<IViewComponentResult> InvokeAsync(string cssClass = "btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px w-md-40px h-md-40px position-relative")
    {
        var model = new SubscriptionBarViewModel
        {
            LoginInformations = await _sessionCache.GetCurrentLoginInformationsAsync(),
            SubscriptionExpireNotifyDayCount = SettingManager.GetSettingValue<int>(AppSettings.TenantManagement.SubscriptionExpireNotifyDayCount),
            CssClass = cssClass
        };

        return View(model);
    }

}

