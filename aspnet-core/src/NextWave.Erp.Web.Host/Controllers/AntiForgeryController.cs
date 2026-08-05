using Microsoft.AspNetCore.Antiforgery;

namespace NextWave.Erp.Web.Controllers;

public class AntiForgeryController : ErpControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiForgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public void GetToken()
    {
        _antiforgery.SetCookieTokenAndHeader(HttpContext);
    }
}

