using Jellyfin.Plugin.PausedStreamKiller.Core;
using Jellyfin.Plugin.PausedStreamKiller.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PausedStreamKiller;

/// <summary>Registers plugin services with Jellyfin's dependency injection container.</summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<PausedSessionMonitor>();
        serviceCollection.AddHostedService<PausedStreamService>();
    }
}

