using System.Threading.Tasks;
using Abp.Webhooks;

namespace NextWave.Erp.WebHooks;

public interface IWebhookEventAppService
{
    Task<WebhookEvent> Get(string id);
}

