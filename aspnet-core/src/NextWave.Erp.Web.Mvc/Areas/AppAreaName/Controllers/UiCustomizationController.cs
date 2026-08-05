using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration;
using NextWave.Erp.Web.Areas.AppAreaName.Models.UiCustomization;
using NextWave.Erp.Web.Controllers;

namespace NextWave.Erp.Web.Areas.AppAreaName.Controllers;

[Area("AppAreaName")]
[AbpMvcAuthorize]
public class UiCustomizationController : ErpControllerBase
{
    private readonly IUiCustomizationSettingsAppService _uiCustomizationAppService;

    public UiCustomizationController(IUiCustomizationSettingsAppService uiCustomizationAppService)
    {
        _uiCustomizationAppService = uiCustomizationAppService;
    }

    public async Task<ActionResult> Index()
    {
        var model = new UiCustomizationViewModel
        {
            Theme = await SettingManager.GetSettingValueAsync(AppSettings.UiManagement.Theme),
            Settings = await _uiCustomizationAppService.GetUiManagementSettings(),
            HasUiCustomizationPagePermission = await PermissionChecker.IsGrantedAsync(AppPermissions.Pages_Administration_UiCustomization)
        };

        return View(model);
    }
}

