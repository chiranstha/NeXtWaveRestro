using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;
using NextWave.Erp.Web.Session;
using NextWave.Erp.Web.Views;

namespace NextWave.Erp.Web.Areas.AppAreaName.Views.Shared.Themes.Theme6.Components.AppAreaNameTheme6Brand;

public class AppAreaNameTheme6BrandViewComponent : ErpViewComponent
{
    private readonly IPerRequestSessionCache _sessionCache;

    public AppAreaNameTheme6BrandViewComponent(IPerRequestSessionCache sessionCache)
    {
        _sessionCache = sessionCache;
    }

    public async Task<IViewComponentResult> InvokeAsync(string skin = "dark-sm")
    {
        var headerModel = new HeaderViewModel
        {
            LoginInformations = await _sessionCache.GetCurrentLoginInformationsAsync(),
        };

        ViewBag.BrandLogoSkin = skin;

        return View(headerModel);
    }
}

