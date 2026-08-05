using Abp.Application.Services.Dto;

namespace NextWave.Erp.Dto
{
    public class GetAllUniversalInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}
