using System.Threading.Tasks;
using NextWave.Erp.Authorization.Users;

namespace NextWave.Erp.WebHooks;

public interface IAppWebhookPublisher
{
    Task PublishTestWebhook();
}

