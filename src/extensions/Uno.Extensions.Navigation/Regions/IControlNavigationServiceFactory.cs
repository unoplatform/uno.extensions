using System;

namespace Uno.Extensions.Navigation.Regions;

public interface IControlNavigationServiceFactory
{
    Type ControlType { get; }

    IRegionNavigationService Create(IServiceProvider services);
}
