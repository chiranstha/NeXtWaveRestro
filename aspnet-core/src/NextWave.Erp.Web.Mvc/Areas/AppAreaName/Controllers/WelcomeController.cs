using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Web.Controllers;

namespace NextWave.Erp.Web.Areas.AppAreaName.Controllers;

[Area("AppAreaName")]
[AbpMvcAuthorize]
public class WelcomeController : ErpControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}

