using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.Authorization;
using NextWave.Erp.EntityChanges;
using NextWave.Erp.EntityChanges.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.EntityChanges;
using NextWave.Erp.Web.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Web.Areas.AppAreaName.Controllers;

[Area("AppAreaName")]
[AbpMvcAuthorize(AppPermissions.Pages_Administration_EntityChanges_FullHistory)]
public class EntityChangesController : ErpControllerBase
{
    private readonly IEntityChangeAppService _entityChangeAppService;

    public EntityChangesController(IEntityChangeAppService entityChangeAppService)
    {
        _entityChangeAppService = entityChangeAppService;
    }

    [HttpGet]
    [Route("/AppAreaName/EntityChanges/{entityId}/{entityTypeFullName}")]
    public async Task<IActionResult> Index(string entityId, string entityTypeFullName)
    {
        var entityChanges = await _entityChangeAppService.GetEntityChangesByEntity(new GetEntityChangesByEntityInput
        {
            EntityId = entityId,
            EntityTypeFullName = entityTypeFullName,
        });

        ViewBag.ChangesCount = entityChanges.Items.Count;
        ViewBag.EntityTypeShortName = entityTypeFullName.Substring(entityTypeFullName.LastIndexOf('.') + 1);
        ViewBag.EntityId = entityId;

        var viewModel = new EntityChangeListViewModel
        {
            EntityAndPropertyChanges = ObjectMapper.Map<List<EntityAndPropertyChangeListDto>>(entityChanges.Items)
        };

        return View(viewModel);
    }
}

