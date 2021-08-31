using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation
{
    public class NavigationService : INavigationManager
    {
        private IList<INavigationService> Adapters { get; } = new List<INavigationService>();

        private IList<bool> ActiveAdapters { get; } = new List<bool>();

        private IServiceProvider Services { get; }

        public NavigationService(IServiceProvider services)
        {
            Services = services;
        }

        public INavigationService AddAdapter<TControl>(TControl control, bool enabled)
        {
            var adapter = Services.GetService<INavigationAdapter<TControl>>();
            adapter.Inject(control);
            Adapters.Insert(0, adapter);
            // Default the first adapter to true
            // This is to capture the initial navigation on the system which could be attempted
            // before the frame has been loaded
            ActiveAdapters.Insert(0, enabled || ActiveAdapters.Count == 0);
            return adapter;
        }

        public void ActivateAdapter(INavigationService adapter)
        {
            var index = Adapters.IndexOf(adapter);
            ActiveAdapters[index] = true;
        }

        public void DeactivateAdapter(INavigationService adapter, bool cleanup)
        {
            var index = Adapters.IndexOf(adapter);
            if (index < 0)
            {
                return;
            }
            if (cleanup)
            {
                Adapters.RemoveAt(index);
                ActiveAdapters.RemoveAt(index);
            }
            else
            {
                ActiveAdapters[index] = false;
            }
        }

        public bool CanNavigate(NavigationRequest request)
        {
            return true;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            for (int i = 0; i < Adapters.Count; i++)
            {
                var adapter = Adapters[i];
                if (ActiveAdapters[i] && adapter.CanNavigate(request))
                {
                    return adapter.Navigate(request);
                }
            }

            return default;
        }
    }
}
