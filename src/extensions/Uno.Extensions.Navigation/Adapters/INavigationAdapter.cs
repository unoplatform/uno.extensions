using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters;

public interface INavigationAdapter : INavigationAware, IInjectable
{
    IServiceProvider Services { get; }

    bool IsCurrentPath(string path);

    bool CanGoBack { get; }

    NavigationResponse Navigate(NavigationContext context);
}
