using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting
{
    public interface IAccountGroupsAppService : IApplicationService
    {
        Task<PagedResultDto<GetAccountGroupForViewDto>> GetAll(GetAllUniversalInput input);

        Task<GetAccountGroupForViewDto> GetAccountGroupForView(Guid id);

        Task<GetAccountGroupForEditOutput> GetAccountGroupForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditAccountGroupDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetAccountGroupsToExcel(GetAllUniversalInput input);

        Task<List<UniversalDropdownDto>> GetAllExplicitAccountGroupForTableDropdown(
            Guid groupId);

        Task<List<AccountGroupsWithNatureDto>> GetAllAccountGroupForTableDropdown();

    }
}
