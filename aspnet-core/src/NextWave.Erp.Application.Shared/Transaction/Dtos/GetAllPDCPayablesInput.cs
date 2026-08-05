using Abp.Application.Services.Dto;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetAllPDCPayablesInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}