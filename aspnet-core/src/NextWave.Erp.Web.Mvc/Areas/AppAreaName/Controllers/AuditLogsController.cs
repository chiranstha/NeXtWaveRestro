using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Auditing;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Auditing;
using NextWave.Erp.Authorization;
using NextWave.Erp.EntityChanges.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.AuditLogs;
using NextWave.Erp.Web.Controllers;

namespace NextWave.Erp.Web.Areas.AppAreaName.Controllers;

[Area("AppAreaName")]
[DisableAuditing]
[AbpMvcAuthorize(AppPermissions.Pages_Administration_AuditLogs)]
public class AuditLogsController : ErpControllerBase
{
    private readonly IAuditLogAppService _auditLogAppService;

    public AuditLogsController(IAuditLogAppService auditLogAppService)
    {
        _auditLogAppService = auditLogAppService;
    }

    public ActionResult Index()
    {
        return View();
    }

    public async Task<PartialViewResult> EntityChangeDetailModal(EntityChangeListDto entityChangeListDto)
    {
        var output = await _auditLogAppService.GetEntityPropertyChanges(entityChangeListDto.Id);

        var viewModel = new EntityChangeDetailModalViewModel(output, entityChangeListDto);

        return PartialView("_EntityChangeDetailModal", viewModel);
    }
}

