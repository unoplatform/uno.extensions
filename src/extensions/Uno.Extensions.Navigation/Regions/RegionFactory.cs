using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation.Regions;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RegionFactory<TControl, TRegion> : IRegionFactory
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    where TRegion : IRegion
{
    private ILogger Logger { get; }

    public RegionFactory(ILogger<RegionFactory<TControl, TRegion>> logger)
    {
        Logger = logger;
    }

    public Type ControlType => typeof(TControl);

    public IRegion Create(IServiceProvider services)
    {
        Logger.LazyLogDebug(() => $"Creating region manager '{typeof(TRegion).Name}' for control '{typeof(TControl).Name}'");
        return services.GetService<TRegion>();
    }
}
