using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Authorization;
using NextWave.Erp.Caching;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Maintenance;
using NextWave.Erp.Web.Controllers;

namespace NextWave.Erp.Web.Areas.AppAreaName.Controllers;

[Area("AppAreaName")]
[AbpMvcAuthorize(AppPermissions.Pages_Administration_Host_Maintenance)]
public class MaintenanceController : ErpControllerBase
{
    private readonly ICachingAppService _cachingAppService;

    public MaintenanceController(ICachingAppService cachingAppService)
    {
        _cachingAppService = cachingAppService;
    }

    public ActionResult Index()
    {
        var model = new MaintenanceViewModel
        {
            Caches = _cachingAppService.GetAllCaches().Items,
            CanClearAllCaches = _cachingAppService.CanClearAllCaches()
        };

        return View(model);
    }
}

