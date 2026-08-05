using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace NextWave.Erp.Web.SignalR;

public class CastleSafeHubActivator<THub> : IHubActivator<THub>
    where THub : Hub
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HashSet<THub> _createdHubs = new();

    public CastleSafeHubActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public THub Create()
    {
        var hub = _serviceProvider.GetService<THub>();
        if (hub != null)
        {
            return hub;
        }

        hub = ActivatorUtilities.CreateInstance<THub>(_serviceProvider);
        _createdHubs.Add(hub);
        return hub;
    }

    public void Release(THub hub)
    {
        if (!_createdHubs.Remove(hub))
        {
            return;
        }

        switch (hub)
        {
            case IAsyncDisposable asyncDisposable:
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }
}
