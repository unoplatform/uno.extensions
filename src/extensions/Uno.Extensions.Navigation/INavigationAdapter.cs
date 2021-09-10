using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation
{
    public interface INavigationAdapter : INavigationAware, IInjectable
    {
        IServiceProvider Services { get; }

        public string Name { get; set; }

        bool IsCurrentPath(string path);

        NavigationResponse Navigate(NavigationContext context);

        bool CanGoBack { get; }
    }
}
