using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase
{
    public interface IPurchaseOrderMastersAppService : IApplicationService
    {
        Task<PagedResultDto<GetPurchaseOrderMasterForViewDto>> GetAll(GetAllUniversalMastersInput input);

        Task<GetPurchaseOrderMasterForViewDto> GetPurchaseOrderMasterForView(Guid id);

        Task<GetPurchaseOrderMasterForEditOutput> GetPurchaseOrderMasterForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditPurchaseOrderMasterDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetPurchaseOrderMastersToExcel(GetAllUniversalMastersInput input);


        Task<List<PurchaseOrderMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown();
    }
}
