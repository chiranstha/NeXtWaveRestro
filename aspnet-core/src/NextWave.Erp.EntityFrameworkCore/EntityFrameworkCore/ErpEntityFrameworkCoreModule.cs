using Abp.Dependency;
using Abp.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Configuration;
using Abp.EntityFrameworkCore.Uow;
using Abp.Modules;
using Abp.OpenIddict.EntityFrameworkCore;
using Abp.Reflection.Extensions;
using Abp.Zero.EntityFrameworkCore;
using Castle.Core;
using Castle.MicroKernel.Registration;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Configuration;
using NextWave.Erp.Migrations.Seed;
using NextWave.Erp.MultiTenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NextWave.Erp.EntityFrameworkCore;

[DependsOn(
    typeof(AbpZeroCoreEntityFrameworkCoreModule),
    typeof(ErpCoreModule),
    typeof(AbpZeroCoreOpenIddictEntityFrameworkCoreModule)
)]
public class ErpEntityFrameworkCoreModule : AbpModule
{
    /* Used it tests to skip DbContext registration, in order to use in-memory database of EF Core */
    public bool SkipDbContextRegistration { get; set; }

    public bool SkipDbSeed { get; set; }

    public override void PreInitialize()
    {
        if (!SkipDbContextRegistration)
        {
            Configuration.Modules.AbpEfCore().AddDbContext<ErpDbContext>(options =>
            {
                if (options.ExistingConnection != null)
                {
                    ErpDbContextConfigurer.Configure(options.DbContextOptions,
                        options.ExistingConnection);
                }
                else
                {
                    ErpDbContextConfigurer.Configure(options.DbContextOptions,
                        options.ConnectionString);
                }
            });
        }

        // Set this setting to true for enabling entity history.
        Configuration.EntityHistory.IsEnabled = false;

        // Uncomment below line to write change logs for the entities below:
        //Configuration.EntityHistory.Selectors.Add("ErpEntities", EntityHistoryHelper.TrackedTypes);
        //Configuration.CustomConfigProviders.Add(new EntityHistoryConfigProvider(Configuration));
    }

    public override void Initialize()
    {
        RegisterCastleSafeDbContextProvider();
        IocManager.RegisterAssemblyByConvention(typeof(ErpEntityFrameworkCoreModule).GetAssembly());
    }

    private void RegisterCastleSafeDbContextProvider()
    {
        RegisterDbContextProvider<ErpDbContext>("ErpDbContextProvider");
        RegisterDbContextProvider<AbpZeroCommonDbContext<Role, User, ErpDbContext>>("ErpZeroCommonDbContextProvider");
        RegisterDbContextProvider<AbpZeroDbContext<Tenant, Role, User, ErpDbContext>>("ErpZeroDbContextProvider");

        HideOpenGenericDbContextProviderFromAssignableLookup();
    }

    private void RegisterDbContextProvider<TDbContext>(string name)
        where TDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        IocManager.IocContainer.Register(
            Component.For<IDbContextProvider<TDbContext>>()
                .ImplementedBy<UnitOfWorkDbContextProvider<TDbContext>>()
                .Named(name)
                .LifestyleTransient()
                .IsDefault()
        );
    }

    private void HideOpenGenericDbContextProviderFromAssignableLookup()
    {
        var openDbContextProviderType = typeof(IDbContextProvider<>);
        var handlers = IocManager.IocContainer.Kernel.GetHandlers()
            .Where(handler => handler.ComponentModel.Services.Contains(openDbContextProviderType))
            .ToList();

        foreach (var handler in handlers)
        {
            RemoveService(handler.ComponentModel, openDbContextProviderType);
        }

        InvalidateCastleHandlerCaches();
    }

    private static void RemoveService(ComponentModel componentModel, Type serviceType)
    {
        var services = GetPrivateField<List<Type>>(componentModel, "services");
        var servicesLookup = GetPrivateField<HashSet<Type>>(componentModel, "servicesLookup");

        services.Remove(serviceType);
        servicesLookup.Remove(serviceType);
    }

    private void InvalidateCastleHandlerCaches()
    {
        var kernel = IocManager.IocContainer.Kernel;
        var namingSubSystem = kernel.GetType()
            .GetProperty("NamingSubSystem", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(kernel);

        namingSubSystem?.GetType()
            .GetMethod("InvalidateCache", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(namingSubSystem, null);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        return (T)instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(instance);
    }

    public override void PostInitialize()
    {
        var configurationAccessor = IocManager.Resolve<IAppConfigurationAccessor>();

        using (var scope = IocManager.CreateScope())
        {
            if (!SkipDbSeed && scope.Resolve<DatabaseCheckHelper>()
                    .Exist(configurationAccessor.Configuration["ConnectionStrings:Default"]))
            {
                SeedHelper.SeedHostDb(IocManager);
            }
        }
    }
}
