using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.EntityChanges.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.EntityChanges;

public interface IEntityChangeAppService : IApplicationService
{
    Task<ListResultDto<EntityAndPropertyChangeListDto>> GetEntityChangesByEntity(GetEntityChangesByEntityInput input);
}

