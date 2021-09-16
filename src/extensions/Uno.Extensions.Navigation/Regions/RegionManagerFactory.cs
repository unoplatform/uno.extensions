using System;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation.Regions;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RegionManagerFactory<TControl, TAdapter> : IRegionManagerFactory
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    where TAdapter : IRegionManager
{
    public Type ControlType => typeof(TControl);

    public IRegionManager Create(IServiceProvider services)
    {
        return services.GetService<TAdapter>();
    }
}
