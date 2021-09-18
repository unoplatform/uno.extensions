using System;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation.Regions;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RegionManagerFactory<TControl, TRegionManager> : IRegionManagerFactory
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    where TRegionManager : IRegionManager
{
    public Type ControlType => typeof(TControl);

    public IRegionManager Create(IServiceProvider services)
    {
        return services.GetService<TRegionManager>();
    }
}
