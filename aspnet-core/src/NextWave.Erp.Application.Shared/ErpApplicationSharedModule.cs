using Abp.Modules;
using Abp.Reflection.Extensions;

namespace NextWave.Erp;

[DependsOn(typeof(ErpCoreSharedModule))]
public class ErpApplicationSharedModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpApplicationSharedModule).GetAssembly());
    }
}

