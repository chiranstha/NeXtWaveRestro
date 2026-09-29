using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales
{
    public interface ISalesReturnMastersAppService : IApplicationService
    {
        Task<PagedResultDto<GetSalesReturnMasterForViewDto>> GetAll(GetAllUniversalMastersInput input);

        Task<GetSalesReturnMasterForViewNewDto> GetSalesReturnMasterForView(Guid id);

        Task<GetSalesReturnMasterForEditOutput> GetSalesReturnMasterForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditSalesReturnMasterDto input);
        Task<Guid> CreateRestaurantRefundReturn(CreateOrEditSalesReturnMasterDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetSalesReturnMastersToExcel(GetAllUniversalMastersInput input);

        Task<List<SalesReturnMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown();

    }
}
