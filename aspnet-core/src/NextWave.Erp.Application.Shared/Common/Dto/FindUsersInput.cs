using NextWave.Erp.Dto;

namespace NextWave.Erp.Common.Dto;

public class FindUsersInput : PagedAndFilteredInputDto
{
    public int? TenantId { get; set; }

    public bool ExcludeCurrentUser { get; set; }
}

