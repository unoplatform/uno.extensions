using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters;

public interface INavigationAdapter : INavigationAware, IInjectable
{
    bool IsCurrentPath(string path);

    NavigationResponse Navigate(NavigationContext context);
}
