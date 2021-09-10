using System;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation
{
    public interface INavigationAdapter : INavigationAware, IInjectable
    {
        public string Name { get; set; }

        bool IsCurrentPath(string path);

        NavigationResponse Navigate(NavigationContext context);
    }
}
