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
    public interface IPurchaseMastersAppService : IApplicationService
    {
        Task<PagedResultDto<PurchaseMastersForViewDto>> GetAll(GetAllUniversalMastersInput input);

        Task<CreateOrEditPurchaseMasterDto> GetPurchaseMasterForEdit(EntityDto<Guid> input);

        Task<Guid> CreateOrEdit(CreateOrEditPurchaseMasterDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetPurchaseMastersToExcel(GetAllUniversalMastersInput input);

        Task<List<PurchaseMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown();

        Task<List<UniversalDropdownDto>> GetAllPurchaseOrderMasterForTableDropdown();


    }
}
