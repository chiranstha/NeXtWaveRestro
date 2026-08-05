using Abp.Application.Services.Dto;
using Abp.Application.Services;
using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales
{
    public interface ISalesMastersAppService : IApplicationService
    {
        Task<PagedResultDto<GetSalesMasterForGetAllDto>> GetAll(GetAllUniversalMastersInput input);

        Task<CreateOrEditSalesMasterDto> GetSalesMasterForEdit(Guid input);

        Task<Guid> CreateOrEdit(CreateOrEditSalesMasterDto input);

        Task Delete(Guid input);

        Task<FileDto> GetSalesMastersToExcel(GetAllUniversalMastersInput input);

        Task<List<SalesMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown();

    }
}
