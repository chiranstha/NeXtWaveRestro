using System;
using System.Transactions;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Uow;
using Abp.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.EntityFrameworkCore;
using NextWave.Erp.Migrations.Seed.Host;
using NextWave.Erp.Migrations.Seed.Tenants;

namespace NextWave.Erp.Migrations.Seed;

public static class SeedHelper
{
    public static void SeedHostDb(IIocResolver iocResolver)
    {
        WithDbContext<ErpDbContext>(iocResolver, SeedHostDb);
    }

    public static void SeedHostDb(ErpDbContext context)
    {
        context.SuppressAutoSetTenantId = true;

        //Host seed
        new InitialHostDbBuilder(context).Create();

        //Default tenant seed (in host database).
        new DefaultTenantBuilder(context).Create();
        var tenantIds = context.Tenants.IgnoreQueryFilters()
            .Select(tenant => tenant.Id)
            .ToList();
        foreach (var tenantId in tenantIds)
            new TenantRoleAndUserBuilder(context, tenantId).Create();
    }

    private static void WithDbContext<TDbContext>(IIocResolver iocResolver, Action<TDbContext> contextAction)
        where TDbContext : DbContext
    {
        using (var uowManager = iocResolver.ResolveAsDisposable<IUnitOfWorkManager>())
        {
            using (var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress))
            {
                var context = uowManager.Object.Current.GetDbContext<TDbContext>(MultiTenancySides.Host);

                contextAction(context);

                uow.Complete();
            }
        }
    }
}

