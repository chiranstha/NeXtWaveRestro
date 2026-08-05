using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Authorization.Permissions.Dto;

namespace NextWave.Erp.Authorization.Permissions;

public interface IPermissionAppService : IApplicationService
{
    ListResultDto<FlatPermissionWithLevelDto> GetAllPermissions();
}

