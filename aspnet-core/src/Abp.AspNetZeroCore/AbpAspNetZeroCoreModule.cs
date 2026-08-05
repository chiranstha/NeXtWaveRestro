using Abp.Modules;
using Abp.Reflection.Extensions;

namespace Abp.AspNetZeroCore
{
    public class AbpAspNetZeroCoreModule : AbpModule
    {
        public override void Initialize() =>
            this.IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetZeroCoreModule).GetAssembly());
    }
}