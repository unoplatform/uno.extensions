using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionManager : INavigationAware, IInjectable
{
    bool IsCurrentPath(string path);

    NavigationResponse Navigate(NavigationContext context);
}
