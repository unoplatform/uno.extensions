using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation.Regions;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RegionManagerFactory<TControl, TRegionManager> : IRegionManagerFactory
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    where TRegionManager : IRegionManager
{
    private ILogger Logger { get; }

    public RegionManagerFactory(ILogger<RegionManagerFactory<TControl, TRegionManager>> logger)
    {
        Logger = logger;
    }

    public Type ControlType => typeof(TControl);

    public IRegionManager Create(IServiceProvider services)
    {
        Logger.LazyLogDebug(() => $"Creating region manager '{typeof(TRegionManager).Name}' for control '{typeof(TControl).Name}'");
        return services.GetService<TRegionManager>();
    }
}
