using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public interface IPDCClearancesAppService : IApplicationService
    {
        Task<PagedResultDto<GetPDCClearanceForViewDto>> GetAll(GetAllPDCClearancesInput input);

        Task<GetPDCClearanceForViewDto> GetPDCClearanceForView(Guid id);

        Task<GetPDCClearanceForEditOutput> GetPDCClearanceForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditPDCClearanceDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetPDCClearancesToExcel(GetAllPDCClearancesInput input);

        Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown();
        Task<List<UniversalDropdownDto>> GetAllBankForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllPDCPayableForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllPDCReceivableForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllFinancialYearForTableDropdown();
    }
}