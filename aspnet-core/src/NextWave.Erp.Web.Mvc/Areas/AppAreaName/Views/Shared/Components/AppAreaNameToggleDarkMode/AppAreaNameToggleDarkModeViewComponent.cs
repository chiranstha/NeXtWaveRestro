using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;
using NextWave.Erp.Web.Views;

namespace NextWave.Erp.Web.Areas.AppAreaName.Views.Shared.Components.AppAreaNameToggleDarkMode;

public class AppAreaNameToggleDarkModeViewComponent : ErpViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(string cssClass, bool isDarkModeActive)
    {
        return Task.FromResult<IViewComponentResult>(View(new ToggleDarkModeViewModel(cssClass, isDarkModeActive)));
    }
}

