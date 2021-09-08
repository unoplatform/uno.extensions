using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;

namespace Uno.Extensions.Navigation
{
    public class NavigationService : INavigationManager
    {
        private IList<INavigationAdapter> Adapters { get; } = new List<INavigationAdapter>();

        private IDictionary<object, INavigationAdapter> AdapterLookup { get; } = new Dictionary<object, INavigationAdapter>();

        private IList<bool> ActiveAdapters { get; } = new List<bool>();

        private IServiceProvider Services { get; }

        public NavigationService(IServiceProvider services)
        {
            Services = services;
        }

        public INavigationAdapter AddAdapter<TControl>(string adapterName, TControl control, bool enabled)
        {
            var scope = Services.CreateScope();
            var services = scope.ServiceProvider;

            var adapter = services.GetService<INavigationAdapter<TControl>>();
            adapter.Name = adapterName;
            if (adapter is INavigationAware navAware)
            {
                navAware.Navigation = new ActiveNavigationService(this, adapter);
            }
            adapter.Inject(control);
            Adapters.Insert(0, adapter);
            // Default the first adapter to true
            // This is to capture the initial navigation on the system which could be attempted
            // before the frame has been loaded
            ActiveAdapters.Insert(0, enabled || ActiveAdapters.Count == 0);
            AdapterLookup[control] = adapter;
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
                AdapterLookup.Remove(kvp => kvp.Value == adapter);
            }
            else
            {
                ActiveAdapters[index] = false;
            }
        }

        public INavigationService ScopedServiceForControl(object control)
        {
            if (control is null)
            {
                return null;
            }

            if (AdapterLookup.TryGetValue(control, out var adapter))
            {
                return new ActiveNavigationService(this, adapter);
            }

            return null;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            return NavigateWithAdapter(request, Adapters.Last());
        }

        public NavigationResult NavigateWithAdapter(NavigationRequest request, INavigationAdapter adapter)
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
                if (request.Route.Data is IDictionary<string, object> paraDict)
                {
                    paras.AddRange(paraDict);
                }
                else
                {
                    paras[string.Empty] = request.Route.Data;
                }
            }

            while (path.StartsWith("//"))
            {
                adapter = (ParentNavigation(adapter) as ActiveNavigationService).Adapter;
                path = path.Length > 2 ? path.Substring(2) : string.Empty;
            }

            var isRooted = path.StartsWith("/");

            var segments = path.Split('/');
            var numberOfPagesToRemove = 0;
            var navPath = string.Empty;
            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    continue;
                }
                if (segments[i] == FrameNavigationAdapter.PreviousViewUri)
                {
                    numberOfPagesToRemove++;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(navPath))
                    {
                        adapter = (ChildNavigation(adapter, navPath) as ActiveNavigationService).Adapter;
                    }
                    navPath = segments[i];
                }
            }

            if (navPath == string.Empty)
            {
                navPath = BaseNavigationAdapter<object>.PreviousViewUri;
                numberOfPagesToRemove--;
            }

            var scope = Services.CreateScope();
            var services = scope.ServiceProvider;
            var dataFactor = services.GetService<ViewModelDataProvider>();
            dataFactor.Parameters = paras; // request.Route.Data as IDictionary<string, object>;
            var navWrapper = services.GetService<NavigationServiceProvider>();
            navWrapper.Navigation = new ActiveNavigationService(this, adapter);

            var context = new NavigationContext(services, request, navPath, isRooted, numberOfPagesToRemove, paras, new CancellationTokenSource(), new TaskCompletionSource<object>());
            return adapter.Navigate(context);
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

        public INavigationService ParentNavigation()
        {
            return ParentNavigation(Adapters.First());
        }

        public INavigationService ParentNavigation(INavigationAdapter adapter)
        {
            var idx = Adapters.IndexOf(adapter);
            if (idx < 0)
            {
                return null;
            }

            if (idx < Adapters.Count - 1)
            {
                idx++;
            }

            return new ActiveNavigationService(this, Adapters[idx]);
        }

        public INavigationService ChildNavigation(string adapterName = null)
        {
            return ChildNavigation(Adapters.First(), adapterName = null);
        }

        public INavigationService ChildNavigation(INavigationAdapter adapter, string adapterName = null)
        {
            var idx = Adapters.IndexOf(adapter);
            if (idx < 0)
            {
                return null;
            }

            while (idx > 0 && (string.IsNullOrWhiteSpace(adapterName) || Adapters[idx].Name != adapterName))
            {
                idx--;
            }

            return new ActiveNavigationService(this, Adapters[idx]);
        }
    }
}
