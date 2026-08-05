using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public interface IPaymentMastersAppService : IApplicationService
    {
        Task<PagedResultDto<GetPaymentMasterForViewDto>> GetAll(GetAllPaymentMastersInput input);

        Task<CreateOrEditPaymentMasterDto> GetPaymentMasterForEdit(Guid id);

        Task<Guid> CreateOrEdit(CreateOrEditPaymentMasterDto input);

        Task Delete(EntityDto<Guid> input);

        Task<FileDto> GetPaymentMastersToExcel(GetAllPaymentMastersInput input);

    }
}