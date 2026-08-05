using Abp.Application.Services.Dto;
using Abp.Application.Services;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextWave.Erp.GeneralSetting.Dtos;

namespace NextWave.Erp.FinancialStatement
{
    public interface ITaxesAppService : IApplicationService
    {
        Task<PagedResultDto<GetTaxForViewDto>> GetAll(GetAllUniversalInput input);

        Task<GetTaxForViewDto> GetTaxForView(Guid id);

        Task<GetTaxForEditOutput> GetTaxForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditTaxDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetTaxesToExcel(GetAllUniversalInput input);

        Task<List<UniversalDropdownDto>> GetAllBranchForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown();
    }
}
