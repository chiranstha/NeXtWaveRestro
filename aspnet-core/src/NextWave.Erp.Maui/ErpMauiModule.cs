using Abp.AutoMapper;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Reflection.Extensions;
using NextWave.Erp.ApiClient;
using NextWave.Erp.Maui.Core;

namespace NextWave.Erp.Maui;

[DependsOn(typeof(ErpClientModule), typeof(AbpAutoMapperModule))]
public class ErpMauiModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Localization.IsEnabled = false;
        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

        Configuration.ReplaceService<IApplicationContext, MauiApplicationContext>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpMauiModule).GetAssembly());
    }
}