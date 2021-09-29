using System;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionFactory
{
    Type ControlType { get; }

    IRegion Create(IServiceProvider services);
}
