using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using NextWave.Erp.Authorization;

namespace NextWave.Erp;

/// <summary>
/// Application layer module of the application.
/// </summary>
[DependsOn(
    typeof(ErpApplicationSharedModule),
    typeof(ErpCoreModule)
    )]
public class ErpApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        //Adding authorization providers
        Configuration.Authorization.Providers.Add<AppAuthorizationProvider>();

        //Adding custom AutoMapper configuration
        Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpApplicationModule).GetAssembly());
    }
}
