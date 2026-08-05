using Abp.Application.Services.Dto;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetAllPDCClearancesInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}