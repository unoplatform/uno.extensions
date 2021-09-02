using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation
{
    public class NavigationService : INavigationManager
    {
        private IList<INavigationAdapter> Adapters { get; } = new List<INavigationAdapter>();

        private IList<bool> ActiveAdapters { get; } = new List<bool>();

        private IServiceProvider Services { get; }

        public NavigationService(IServiceProvider services)
        {
            Services = services;
        }

        public INavigationAdapter AddAdapter<TControl>(TControl control, bool enabled)
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

        public void ActivateAdapter(INavigationAdapter adapter)
        {
            var index = Adapters.IndexOf(adapter);
            ActiveAdapters[index] = true;
        }

        public void DeactivateAdapter(INavigationAdapter adapter, bool cleanup)
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

        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;

            var queryIdx = path.IndexOf('?');
            var query = string.Empty;
            if (queryIdx >= 0)
            {
                queryIdx++; // Step over the ?
                query = queryIdx < path.Length ? path.Substring(queryIdx) : string.Empty;
                path = path.Substring(0, queryIdx - 1);
            }

            var paras = ParseQueryParameters(query);
            if (request.Route.Data is not null)
            {
                paras[string.Empty] = request.Route.Data;
            }
            request = request with { Route = request.Route with { Path = new Uri(path, UriKind.Relative), Data = paras } };


            var scope = Services.CreateScope();
            var services = scope.ServiceProvider;
            var dataFactor = services.GetService<ViewModelDataProvider>();
            dataFactor.Parameters = request.Route.Data as IDictionary<string, object>;

            var context = new NavigationContext(services, request, new CancellationTokenSource(), true);
            for (int i = 0; i < Adapters.Count; i++)
            {
                var adapter = Adapters[i];
                if (ActiveAdapters[i] && adapter.CanNavigate(context))
                {
                    return adapter.Navigate(context);
                }
            }

            return default;
        }

        private IDictionary<string, object> ParseQueryParameters(string queryString)
        {
            return (from pair in (queryString + string.Empty).Split('&')
                    where pair is not null
                    let bits = pair.Split('=')
                    where bits.Length == 2
                    let key = bits[0]
                    let val = bits[1]
                    where key is not null && val is not null
                    select new { key, val })
                    .ToDictionary(x => x.key, x => (object)x.val);
        }
    }
}
