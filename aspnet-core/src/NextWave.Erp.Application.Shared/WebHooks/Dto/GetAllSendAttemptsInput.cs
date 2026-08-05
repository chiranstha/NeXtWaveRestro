using NextWave.Erp.Dto;

namespace NextWave.Erp.WebHooks.Dto;

public class GetAllSendAttemptsInput : PagedInputDto
{
    public string SubscriptionId { get; set; }
}

