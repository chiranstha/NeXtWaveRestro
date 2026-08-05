using Abp.Application.Services.Dto;
using Abp.Application.Services;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextWave.Erp.Inventory.Dtos;

namespace NextWave.Erp.Inventory
{
    public interface IProductsAppService : IApplicationService
    {
        Task<PagedResultDto<GetProductForGetAllView>> GetAll(GetAllUniversalInput input);

        Task<GetProductForViewDto> GetProductForView(Guid id);

        Task<GetProductForEditOutput> GetProductForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditProductDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetProductsToExcel(GetAllUniversalInput input);

        Task<List<UniversalDropdownDto>> GetAllBranchForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllProductForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllRawMaterialForTableDropdown();


        Task<List<UniversalDropdownDto>> GetAllUnitForTableDropdown();
    }
}
