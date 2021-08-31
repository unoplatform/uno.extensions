using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation
{
    public class NavigationService : INavigationManager
    {
        private IList<INavigationService> ActiveAdapters { get; } = new List<INavigationService>();
        private IServiceProvider Services { get; }
        public NavigationService(IServiceProvider services)
        {
            Services = services;
        }

        public INavigationService ActivateAdapter<TControl>(TControl control )
        {
            var adapter = Services.GetService<INavigationAdapter<TControl>>();
            adapter.Inject(control);
            ActiveAdapters.Insert(0,adapter);
            return adapter;
        }

        public void DeactivateAdapter(INavigationService adapter)
        {
            ActiveAdapters.Remove(adapter);
        }

        public bool CanNavigate(NavigationRequest request)
        {
            return true;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            foreach (var adapter in ActiveAdapters)
            {
                if (adapter.CanNavigate(request))
                {
                    return adapter.Navigate(request);
                }
            }

            return default;
        }
    }
}
