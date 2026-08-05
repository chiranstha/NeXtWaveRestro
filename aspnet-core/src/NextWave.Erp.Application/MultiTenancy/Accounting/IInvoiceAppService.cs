using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using NextWave.Erp.MultiTenancy.Accounting.Dto;

namespace NextWave.Erp.MultiTenancy.Accounting;

public interface IInvoiceAppService
{
    Task<InvoiceDto> GetInvoiceInfo(EntityDto<long> input);

    Task CreateInvoice(CreateInvoiceDto input);
}
