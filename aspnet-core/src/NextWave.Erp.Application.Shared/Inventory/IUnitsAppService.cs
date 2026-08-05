using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using System;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory
{
    public interface IUnitsAppService : IApplicationService
    {
        Task<PagedResultDto<GetUnitForViewDto>> GetAll(GetAllUniversalInput input);

        Task<GetUnitForViewDto> GetUnitForView(Guid id);

        Task<GetUnitForEditOutput> GetUnitForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditUnitDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetUnitsToExcel();
    }
}
