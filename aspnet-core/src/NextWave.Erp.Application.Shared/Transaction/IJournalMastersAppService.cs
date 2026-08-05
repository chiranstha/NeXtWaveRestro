using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public interface IJournalMastersAppService : IApplicationService
    {
        Task<PagedResultDto<GetJournalMasterForViewDto>> GetAll(GetAllJournalMastersInput input);

        Task<GetJournalMasterForViewDto> GetJournalMasterForView(Guid id);

        Task<CreateOrEditJournalMasterDto> GetJournalMasterForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditJournalMasterDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetJournalMastersToExcel(GetAllJournalMastersForExcelInput input);

    }
}
