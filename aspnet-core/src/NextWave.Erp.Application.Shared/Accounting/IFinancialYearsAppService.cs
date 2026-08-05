using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting
{
    public interface IFinancialYearsAppService : IApplicationService
    {
        Task<PagedResultDto<GetFinancialYearForViewDto>> GetAll(GetAllUniversalInput input);

        Task<GetFinancialYearForViewDto> GetFinancialYearForView(Guid id);

        Task<GetFinancialYearForEditOutput> GetFinancialYearForEdit(EntityDto<Guid> input);

        Task CreateOrEdit(CreateOrEditFinancialYearDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetFinancialYearsToExcel(GetAllUniversalInput input);
    }
}
