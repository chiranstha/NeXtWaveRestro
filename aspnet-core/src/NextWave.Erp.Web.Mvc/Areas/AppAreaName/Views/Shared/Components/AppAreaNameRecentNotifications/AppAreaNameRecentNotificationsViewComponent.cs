using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;
using NextWave.Erp.Web.Views;

namespace NextWave.Erp.Web.Areas.AppAreaName.Views.Shared.Components.AppAreaNameRecentNotifications;

public class AppAreaNameRecentNotificationsViewComponent : ErpViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(string cssClass, string iconClass = "flaticon-alert-2 unread-notification fs-2")
    {
        var model = new RecentNotificationsViewModel
        {
            CssClass = cssClass,
            IconClass = iconClass
        };

        return Task.FromResult<IViewComponentResult>(View(model));
    }
}

