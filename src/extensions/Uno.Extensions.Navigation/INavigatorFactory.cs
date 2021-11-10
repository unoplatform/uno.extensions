using System;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public interface INavigatorFactory
{
    void RegisterNavigator<TNavigator>(params string[] names)
        where TNavigator : INavigator;

    INavigator? CreateService(IRegion region);

    INavigator? CreateService(IRegion region, NavigationRequest request);
}
