using Abp.AspNetZeroCore;
using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;
using NextWave.Erp.Configuration;
using NextWave.Erp.EntityFrameworkCore;
using NextWave.Erp.Migrator.DependencyInjection;

namespace NextWave.Erp.Migrator;

[DependsOn(typeof(ErpEntityFrameworkCoreModule))]
public class ErpMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public ErpMigratorModule(ErpEntityFrameworkCoreModule abpZeroTemplateEntityFrameworkCoreModule)
    {
        abpZeroTemplateEntityFrameworkCoreModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(ErpMigratorModule).GetAssembly().GetDirectoryPathOrNull(),
            addUserSecrets: true
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            ErpConsts.ConnectionStringName
            );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(typeof(IEventBus), () =>
        {
            IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            );
        });
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}

