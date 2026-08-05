using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using GraphQL.Types.Relay;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextWave.Erp.Debugging;
using NextWave.Erp.Queries;
using NextWave.Erp.Queries.Container;
using NextWave.Erp.Schemas;

namespace NextWave.Erp.Configure;

public static class ServiceCollectionExtensions
{
    public static void AddAndConfigureGraphQL(this IServiceCollection services)
    {
        services
            .AddGraphQL(x => x.AddSystemTextJson()
                .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = DebugHelper.IsDebug)
                .AddGraphTypes()
                .AddDataLoader()
                .AddUserContextBuilder(httpContext => new Dictionary<string, object>
                {
                        {"user", httpContext.User}
                })
            );

        AddSchemaServices(services);

        RemoveUnusedOpenGraphTypes(services);

        AllowSynchronousIo(services);
    }

    private static void AddSchemaServices(IServiceCollection services)
    {
        services.TryAddTransient<MainSchema>();
        services.TryAddTransient<ISchema, MainSchema>();
        services.TryAddTransient<QueryContainer>();
        services.TryAddTransient<RoleQuery>();
        services.TryAddTransient<UserQuery>();
        services.TryAddTransient<OrganizationUnitQuery>();
    }

    private static void RemoveUnusedOpenGraphTypes(IServiceCollection services)
    {
        services.RemoveAll(typeof(EnumerationGraphType<>));
        services.RemoveAll(typeof(ConnectionType<>));
        services.RemoveAll(typeof(ConnectionType<,>));
        services.RemoveAll(typeof(EdgeType<>));
        services.RemoveAll(typeof(InputObjectGraphType<>));
        services.RemoveAll(typeof(AutoRegisteringInputObjectGraphType<>));
        services.RemoveAll(typeof(AutoRegisteringObjectGraphType<>));
    }

    //https://github.com/graphql-dotnet/graphql-dotnet/issues/1326
    private static void AllowSynchronousIo(IServiceCollection services)
    {
        // kestrel
        services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });

        // IIS
        services.Configure<IISServerOptions>(options => { options.AllowSynchronousIO = true; });
    }
}

