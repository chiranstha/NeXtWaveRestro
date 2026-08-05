using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NextWave.Erp.Configure;
using NextWave.Erp.Startup;
using NextWave.Erp.Test.Base;

namespace NextWave.Erp.GraphQL.Tests
{
    [DependsOn(
        typeof(ErpGraphQLModule),
        typeof(ErpTestBaseModule))]
    public class ErpGraphQLTestModule : AbpModule
    {
        public override void PreInitialize()
        {
            IServiceCollection services = new ServiceCollection();
            
            services.AddAndConfigureGraphQL();

            WindsorRegistrationHelper.CreateServiceProvider(IocManager.IocContainer, services);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(ErpGraphQLTestModule).GetAssembly());
        }
    }
}