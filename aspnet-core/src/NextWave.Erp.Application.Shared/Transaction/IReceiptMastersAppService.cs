using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public interface IReceiptMastersAppService : IApplicationService
    {
        Task<PagedResultDto<GetReceiptMasterForViewDto>> GetAll(GetAllReceiptMastersInput input);

        Task<CreateOrEditReceiptMasterDto> GetReceiptMasterForEdit(Guid id);

        Task<Guid> CreateOrEdit(CreateOrEditReceiptMasterDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetReceiptMastersToExcel(GetAllReceiptMastersForExcelInput input);

        Task<List<ReceiptMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown();

    }
}
