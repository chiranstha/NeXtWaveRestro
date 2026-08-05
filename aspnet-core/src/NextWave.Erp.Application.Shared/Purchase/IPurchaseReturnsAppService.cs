using Abp.Application.Services.Dto;
using Abp.Application.Services;
using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase
{
    public interface IPurchaseReturnsAppService : IApplicationService
    {
        Task<PagedResultDto<GetPurchaseReturnForViewDto>> GetAll(GetAllUniversalMastersInput input);

        Task<GetPurchaseReturnForEditOutput> GetPurchaseReturnForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditPurchaseReturnDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetPurchaseReturnsToExcel(GetAllUniversalMastersInput input);


        Task<List<PurchaseReturnAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown();
    }
}
