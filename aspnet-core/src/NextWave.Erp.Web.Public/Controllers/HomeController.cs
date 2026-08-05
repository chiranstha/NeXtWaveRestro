using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Web.Controllers;

namespace NextWave.Erp.Web.Public.Controllers;

public class HomeController : ErpControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}

