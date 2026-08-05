using Abp.Application.Services.Dto;
using Abp.Application.Services;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextWave.Erp.Accounting.Dtos;

namespace NextWave.Erp.Accounting
{
    public interface IAccountLedgersAppService : IApplicationService
    {
        Task<PagedResultDto<GetAccountLedgerForViewDto>> GetAll(GetAllUniversalInput input);

        Task<GetAccountLedgerForViewDto> GetAccountLedgerForView(Guid id);

        Task<GetAccountLedgerForEditOutput> GetAccountLedgerForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditAccountLedgerDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetAccountLedgersToExcel(GetAllUniversalInput input);

        Task<List<AccountLedgerAccountGroupTableDto>> GetAllAccountGroupForTableDropdown();
    }
}
