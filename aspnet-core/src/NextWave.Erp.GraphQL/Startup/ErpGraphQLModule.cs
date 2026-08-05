using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace NextWave.Erp.Startup;

[DependsOn(typeof(ErpCoreModule))]
public class ErpGraphQLModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpGraphQLModule).GetAssembly());
    }

    public override void PreInitialize()
    {
        base.PreInitialize();

        //Adding custom AutoMapper configuration
        Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
    }
}

