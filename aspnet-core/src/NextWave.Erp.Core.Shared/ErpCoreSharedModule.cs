using Abp.Modules;
using Abp.Reflection.Extensions;

namespace NextWave.Erp;

public class ErpCoreSharedModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpCoreSharedModule).GetAssembly());
    }
}

