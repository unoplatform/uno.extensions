using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation.Services;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ControlNavigationServiceFactory<TControl, TRegion> : IControlNavigationServiceFactory
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    where TRegion : IRegionNavigationService
{
    private ILogger Logger { get; }

    public ControlNavigationServiceFactory(ILogger<ControlNavigationServiceFactory<TControl, TRegion>> logger)
    {
        Logger = logger;
    }

    public Type ControlType => typeof(TControl);

    public IRegionNavigationService Create(IServiceProvider services)
    {
        Logger.LazyLogDebug(() => $"Creating region manager '{typeof(TRegion).Name}' for control '{typeof(TControl).Name}'");
        return services.GetService<TRegion>();
    }
}
