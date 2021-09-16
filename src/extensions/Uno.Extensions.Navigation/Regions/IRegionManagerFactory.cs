using System;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionManagerFactory
{
    Type ControlType { get; }

    IRegionManager Create(IServiceProvider services);
}
