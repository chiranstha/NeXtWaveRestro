using System;
using System.Diagnostics;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Configuration;

namespace NextWave.Erp.MultiTenancy.Subscription;

public class SubscriptionExpireEmailNotifierWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private const int CheckPeriodAsMilliseconds = 1 * 60 * 60 * 1000 * 24; //1 day

    private readonly IRepository<Tenant> _tenantRepository;
    private readonly UserEmailer _userEmailer;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SubscriptionExpireEmailNotifierWorker(
        AbpTimer timer,
        IRepository<Tenant> tenantRepository,
        UserEmailer userEmailer,
        IUnitOfWorkManager unitOfWorkManager) : base(timer)
    {
        _tenantRepository = tenantRepository;
        _userEmailer = userEmailer;
        _unitOfWorkManager = unitOfWorkManager;

        Timer.Period = CheckPeriodAsMilliseconds;
        Timer.RunOnStart = true;

        LocalizationSourceName = ErpConsts.LocalizationSourceName;
    }

    protected override void DoWork()
    {
        _unitOfWorkManager.WithUnitOfWork(() =>
        {
            var subscriptionRemainingDayCount = Convert.ToInt32(SettingManager.GetSettingValueForApplication(AppSettings.TenantManagement.SubscriptionExpireNotifyDayCount));
            var dateToCheckRemainingDayCount = Clock.Now.AddDays(subscriptionRemainingDayCount).ToUniversalTime();

            var subscriptionExpiredTenants = _tenantRepository.GetAllList(
                tenant => tenant.SubscriptionEndDateUtc != null &&
                          tenant.SubscriptionEndDateUtc.Value.Date == dateToCheckRemainingDayCount.Date &&
                          tenant.IsActive &&
                          tenant.EditionId != null
            );

            foreach (var tenant in subscriptionExpiredTenants)
            {
                Debug.Assert(tenant.EditionId.HasValue);
                try
                {
                    AsyncHelper.RunSync(() => _userEmailer.TryToSendSubscriptionExpiringSoonEmail(tenant.Id, dateToCheckRemainingDayCount));
                }
                catch (Exception exception)
                {
                    Logger.Error(exception.Message, exception);
                }
            }
        });
    }
}

