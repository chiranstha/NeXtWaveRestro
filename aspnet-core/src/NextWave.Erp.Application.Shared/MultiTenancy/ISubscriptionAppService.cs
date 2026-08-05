using System.Threading.Tasks;
using Abp.Application.Services;
using NextWave.Erp.MultiTenancy.Dto;
using NextWave.Erp.MultiTenancy.Payments.Dto;

namespace NextWave.Erp.MultiTenancy;

public interface ISubscriptionAppService : IApplicationService
{
    Task DisableRecurringPayments();

    Task EnableRecurringPayments();

    Task<long> StartExtendSubscription(StartExtendSubscriptionInput input);

    Task<StartUpgradeSubscriptionOutput> StartUpgradeSubscription(StartUpgradeSubscriptionInput input);

    Task<long> StartTrialToBuySubscription(StartTrialToBuySubscriptionInput input);
}

