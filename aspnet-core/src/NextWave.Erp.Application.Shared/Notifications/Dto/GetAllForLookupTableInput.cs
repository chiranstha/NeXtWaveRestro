using Abp.Application.Services.Dto;

namespace NextWave.Erp.Notifications.Dto;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}

