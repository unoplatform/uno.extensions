using System;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public interface IRegionNavigationServiceFactory
{
    INavigationService CreateService(IRegion region);

    INavigationService CreateService(IServiceProvider services, NavigationRequest request);
}
