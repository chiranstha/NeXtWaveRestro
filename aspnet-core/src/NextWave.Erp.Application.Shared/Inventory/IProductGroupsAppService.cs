using Abp.Application.Services.Dto;
using Abp.Application.Services;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory
{
    public interface IProductGroupsAppService : IApplicationService
    {
        Task<PagedResultDto<ProductGroupTreeDto>> GetAll(GetAllUniversalInput input);

        Task<GetProductGroupForViewDto> GetProductGroupForView(Guid id);

        Task<GetProductGroupForEditOutput> GetProductGroupForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditProductGroupDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetProductGroupsToExcel(GetAllUniversalInput input);
    }
}
