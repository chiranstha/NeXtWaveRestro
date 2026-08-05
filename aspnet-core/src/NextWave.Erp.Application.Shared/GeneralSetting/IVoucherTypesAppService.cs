using Abp.Application.Services.Dto;
using Abp.Application.Services;
using NextWave.Erp.GeneralSetting.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextWave.Erp.Dto;

namespace NextWave.Erp.GeneralSetting
{
    public interface IVoucherTypesAppService : IApplicationService
    {
        Task<PagedResultDto<GetVoucherTypeForViewDto>> GetAll(GetAllUniversalInput input);

        Task<GetVoucherTypeForViewDto> GetVoucherTypeForView(Guid id);

        Task<GetVoucherTypeForEditOutput> GetVoucherTypeForEdit(EntityDto<Guid> input);

        Task CreateOrEdit(CreateOrEditVoucherTypeDto input);

        Task Delete(EntityDto<Guid> input);

        Task<List<UniversalDropdownDto>> GetAllBranchForTableDropdown();
    }
}
