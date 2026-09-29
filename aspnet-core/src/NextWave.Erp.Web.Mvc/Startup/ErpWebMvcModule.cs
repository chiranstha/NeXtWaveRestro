using Abp.AspNetZeroCore;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Threading.BackgroundWorkers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NextWave.Erp.Auditing;
using NextWave.Erp.Authorization.Users.Password;
using NextWave.Erp.Configuration;
using NextWave.Erp.EntityFrameworkCore;
using NextWave.Erp.MultiTenancy;
using NextWave.Erp.MultiTenancy.Subscription;
using NextWave.Erp.Restaurant;
using NextWave.Erp.Web.Areas.AppAreaName.Startup;

namespace NextWave.Erp.Web.Startup;

[DependsOn(
    typeof(ErpWebCoreModule)
)]
public class ErpWebMvcModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public ErpWebMvcModule(IWebHostEnvironment env)
    {
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Modules.AbpWebCommon().MultiTenancy.DomainFormat = _appConfiguration["App:WebSiteRootAddress"] ?? "https://localhost:44302/";
        
        Configuration.Navigation.Providers.Add<AppAreaNameNavigationProvider>();

        IocManager.Register<DashboardViewConfiguration>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpWebMvcModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        if (!IocManager.Resolve<IMultiTenancyConfig>().IsEnabled)
        {
            return;
        }

        using (var scope = IocManager.CreateScope())
        {
            if (!scope.Resolve<DatabaseCheckHelper>().Exist(_appConfiguration["ConnectionStrings:Default"]))
            {
                return;
            }
        }

        var workManager = IocManager.Resolve<IBackgroundWorkerManager>();
        workManager.Add(IocManager.Resolve<SubscriptionExpirationCheckWorker>());
        workManager.Add(IocManager.Resolve<SubscriptionExpireEmailNotifierWorker>());
        workManager.Add(IocManager.Resolve<SubscriptionPaymentNotCompletedEmailNotifierWorker>());

        var expiredAuditLogDeleterWorker = IocManager.Resolve<ExpiredAuditLogDeleterWorker>();
        if (Configuration.Auditing.IsEnabled && expiredAuditLogDeleterWorker.IsEnabled)
        {
            workManager.Add(expiredAuditLogDeleterWorker);
        }

        workManager.Add(IocManager.Resolve<PasswordExpirationBackgroundWorker>());
        workManager.Add(IocManager.Resolve<RestaurantSmsOutboxWorker>());
    }
}

