using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;
using NextWave.Erp.Web.Session;
using NextWave.Erp.Web.Views;

namespace NextWave.Erp.Web.Areas.AppAreaName.Views.Shared.Components.AppAreaNameLogo;

public class AppAreaNameLogoViewComponent : ErpViewComponent
{
    private readonly IPerRequestSessionCache _sessionCache;

    public AppAreaNameLogoViewComponent(
        IPerRequestSessionCache sessionCache
    )
    {
        _sessionCache = sessionCache;
    }

    public async Task<IViewComponentResult> InvokeAsync(string logoSkin = null, string logoClass = "")
    {
        var headerModel = new LogoViewModel
        {
            LoginInformations = await _sessionCache.GetCurrentLoginInformationsAsync(),
            LogoSkinOverride = logoSkin,
            LogoClassOverride = logoClass
        };

        return View(headerModel);
    }
}

