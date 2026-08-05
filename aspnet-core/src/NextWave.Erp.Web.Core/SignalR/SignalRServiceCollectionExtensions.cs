using System;
using Abp.AspNetCore.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextWave.Erp.Authorization.QrLogin;
using NextWave.Erp.Web.Chat.SignalR;

namespace NextWave.Erp.Web.SignalR;

public static class SignalRServiceCollectionExtensions
{
    private static readonly Type HubContextImplementationType =
        typeof(Hub).Assembly.GetType("Microsoft.AspNetCore.SignalR.Internal.HubContext`1", throwOnError: true)!;

    private static readonly Type HubDispatcherServiceType =
        typeof(Hub).Assembly.GetType("Microsoft.AspNetCore.SignalR.Internal.HubDispatcher`1", throwOnError: true)!;

    public static IServiceCollection AddCastleSafeHubActivators(this IServiceCollection services)
    {
        services.RemoveAll(typeof(HubConnectionHandler<>));
        services.RemoveAll(typeof(HubLifetimeManager<>));
        services.RemoveAll(typeof(IHubContext<>));
        services.RemoveAll(typeof(IHubContext<,>));
        services.RemoveAll(typeof(IHubActivator<>));
        services.RemoveAll(HubDispatcherServiceType);

        AddClosedHubServices<AbpCommonHub>(services);
        AddClosedHubServices<ChatHub>(services);
        AddClosedHubServices<QrLoginHub>(services);

        return services;
    }

    private static void AddClosedHubServices<THub>(IServiceCollection services)
        where THub : Hub
    {
        services.AddSingleton<HubLifetimeManager<THub>, DefaultHubLifetimeManager<THub>>();
        services.AddSingleton<IHubContext<THub>>(CreateHubContext<THub>);
        services.AddSingleton<HubConnectionHandler<THub>>();
        services.AddScoped<IHubActivator<THub>, CastleSafeHubActivator<THub>>();
    }

    private static IHubContext<THub> CreateHubContext<THub>(IServiceProvider serviceProvider)
        where THub : Hub
    {
        var hubContextType = HubContextImplementationType.MakeGenericType(typeof(THub));
        return (IHubContext<THub>)ActivatorUtilities.CreateInstance(serviceProvider, hubContextType);
    }
}
