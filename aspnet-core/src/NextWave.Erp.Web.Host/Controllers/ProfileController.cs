using Abp.AspNetCore.Mvc.Authorization;
using NextWave.Erp.Authorization.Users.Profile;
using NextWave.Erp.Storage;

namespace NextWave.Erp.Web.Controllers;

[AbpMvcAuthorize]
public class ProfileController : ProfileControllerBase
{
    public ProfileController(
        ITempFileCacheManager tempFileCacheManager,
        IProfileAppService profileAppService) :
        base(tempFileCacheManager, profileAppService)
    {
    }
}

