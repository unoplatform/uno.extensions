using System;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionFactory
{
    Type ControlType { get; }

    IRegionNavigationService Create(IServiceProvider services);
}
