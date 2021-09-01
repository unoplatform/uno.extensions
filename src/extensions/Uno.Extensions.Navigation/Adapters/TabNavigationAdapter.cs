﻿using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
using System;
#if WINDOWS_UWP
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public class TabNavigationAdapter : INavigationAdapter<TabView>
    {
        private INavigationMapping Mapping { get; }

        private IServiceProvider Services { get; }

        private ITabWrapper Tabs { get; }

        public void Inject(TabView control)
        {
            Tabs.Inject(control);
        }

        public TabNavigationAdapter(
            IServiceProvider services,
            INavigationMapping navigationMapping,
            ITabWrapper tabWrapper)
        {
            Services = services;
            Mapping = navigationMapping;
            Tabs = tabWrapper;
        }

        public bool CanNavigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            return Tabs.ContainsTab(path);
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            var map = Mapping.LookupByPath(path);

            Func<object> creator = () => map.ViewModel is not null ? Services.GetService(map.ViewModel) : null;

            Tabs.ActivateTab(path, map.ViewModel, creator);

            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
