using Abp.Application.Services.Dto;
using Abp.Webhooks;
using NextWave.Erp.WebHooks.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Webhooks;

public class CreateOrEditWebhookSubscriptionViewModel
{
    public WebhookSubscription WebhookSubscription { get; set; }

    public ListResultDto<GetAllAvailableWebhooksOutput> AvailableWebhookEvents { get; set; }
}

