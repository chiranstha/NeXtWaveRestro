using System.Collections.Generic;
using System.Linq;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Authorization.Permissions;
using NextWave.Erp.Authorization.Permissions.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common.Modals;
using NextWave.Erp.Web.Controllers;

namespace NextWave.Erp.Web.Areas.AppAreaName.Controllers;

[Area("AppAreaName")]
[AbpMvcAuthorize]
public class CommonController : ErpControllerBase
{
    private readonly IPermissionAppService _permissionAppService;

    public CommonController(IPermissionAppService permissionAppService)
    {
        _permissionAppService = permissionAppService;
    }

    public PartialViewResult LookupModal(LookupModalViewModel model)
    {
        return PartialView("Modals/_LookupModal", model);
    }

    public PartialViewResult EntityTypeHistoryModal(EntityHistoryModalViewModel input)
    {
        return PartialView("Modals/_EntityTypeHistoryModal", ObjectMapper.Map<EntityHistoryModalViewModel>(input));
    }

    public PartialViewResult PermissionTreeModal(List<string> grantedPermissionNames = null)
    {
        var permissions = _permissionAppService.GetAllPermissions().Items.ToList();

        var model = new PermissionTreeModalViewModel
        {
            Permissions = ObjectMapper.Map<List<FlatPermissionDto>>(permissions).OrderBy(p => p.DisplayName).ToList(),
            GrantedPermissionNames = grantedPermissionNames
        };

        return PartialView("Modals/_PermissionTreeModal", model);
    }

    public PartialViewResult InactivityControllerNotifyModal()
    {
        return PartialView("Modals/_InactivityControllerNotifyModal");
    }
}

