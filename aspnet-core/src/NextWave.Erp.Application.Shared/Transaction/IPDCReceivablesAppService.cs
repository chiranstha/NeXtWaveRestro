using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public interface IPDCReceivablesAppService : IApplicationService
    {
        Task<PagedResultDto<GetPDCReceivableForViewDto>> GetAll(GetAllPDCReceivablesInput input);

        Task<GetPDCReceivableForViewDto> GetPDCReceivableForView(Guid id);

        Task<GetPDCReceivableForEditOutput> GetPDCReceivableForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditPDCReceivableDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetPDCReceivablesToExcel(GetAllPDCReceivablesInput input);

        Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllFinancialYearForTableDropdown();
    }
}
