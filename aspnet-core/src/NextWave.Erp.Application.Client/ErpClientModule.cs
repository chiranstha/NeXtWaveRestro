using Abp.Modules;
using Abp.Reflection.Extensions;

namespace NextWave.Erp;

public class ErpClientModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ErpClientModule).GetAssembly());
    }
}

