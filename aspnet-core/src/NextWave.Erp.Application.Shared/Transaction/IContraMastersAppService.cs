using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public interface IContraMastersAppService : IApplicationService
    {
        Task<PagedResultDto<GetContraMasterForViewDto>> GetAll(GetAllContraMastersInput input);

        Task<GetContraMasterForViewDto> GetContraMasterForView(Guid id);

        Task<GetContraMasterForEditOutput> GetContraMasterForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditContraMasterDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetContraMastersToExcel(GetAllContraMastersForExcelInput input);

        Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown();

    }
}