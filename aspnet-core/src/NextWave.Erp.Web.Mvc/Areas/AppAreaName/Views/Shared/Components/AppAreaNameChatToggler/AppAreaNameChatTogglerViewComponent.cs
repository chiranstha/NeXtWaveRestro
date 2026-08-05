using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;
using NextWave.Erp.Web.Views;

namespace NextWave.Erp.Web.Areas.AppAreaName.Views.Shared.Components.AppAreaNameChatToggler;

public class AppAreaNameChatTogglerViewComponent : ErpViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(string cssClass, string iconClass = "flaticon-chat-2 fs-4")
    {
        return Task.FromResult<IViewComponentResult>(View(new ChatTogglerViewModel
        {
            CssClass = cssClass,
            IconClass = iconClass
        }));
    }
}

