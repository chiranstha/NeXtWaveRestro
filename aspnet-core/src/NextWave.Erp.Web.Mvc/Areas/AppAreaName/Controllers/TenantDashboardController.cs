using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Authorization;
using NextWave.Erp.DashboardCustomization;
using System.Threading.Tasks;
using NextWave.Erp.Web.Areas.AppAreaName.Startup;

namespace NextWave.Erp.Web.Areas.AppAreaName.Controllers;

[Area("AppAreaName")]
[AbpMvcAuthorize(AppPermissions.Pages_Tenant_Dashboard)]
public class TenantDashboardController : CustomizableDashboardControllerBase
{
    public TenantDashboardController(DashboardViewConfiguration dashboardViewConfiguration,
        IDashboardCustomizationAppService dashboardCustomizationAppService)
        : base(dashboardViewConfiguration, dashboardCustomizationAppService)
    {

    }

    public async Task<ActionResult> Index()
    {
        return await GetView(ErpDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard);
    }
}

