using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public interface IPDCPayablesAppService : IApplicationService
    {
        Task<PagedResultDto<GetPDCPayableForViewDto>> GetAll(GetAllPDCPayablesInput input);

        Task<GetPDCPayableForViewDto> GetPDCPayableForView(Guid id);

        Task<GetPDCPayableForEditOutput> GetPDCPayableForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditPDCPayableDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetPDCPayablesToExcel(GetAllPDCPayablesForExcelInput input);
    }
}
