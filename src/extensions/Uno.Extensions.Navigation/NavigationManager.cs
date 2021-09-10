using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Adapters;

namespace Uno.Extensions.Navigation
{
    public class NavigationManager : INavigationManager
    {
        private ActiveNavigationService RootAdapter { get; set; }

        private IServiceProvider Services { get; }

        private IDictionary<Type,IAdapterFactory> Factories { get;  }

        public NavigationManager(IServiceProvider services)
        {
            Services = services;
            var factories = services.GetServices<IAdapterFactory>();
            Factories = factories.ToDictionary(x => x.ControlType);
        }

        public INavigationService AddAdapter(INavigationService parentAdapter, string routeName, object control, INavigationService existingAdapter)
        {
            var ans = existingAdapter as ActiveNavigationService;
            var parent = parentAdapter as ActiveNavigationService;
            if (ans is null)
            {
               

                var factory = Factories[control.GetType()];

                var adapter = factory.Create();
                adapter.Name = routeName;
                adapter.Inject(control);

                ans = new ActiveNavigationService(this, adapter, parent);
            }

            if (parent is null)
            {
#if DEBUG
                if (RootAdapter is not null) throw new Exception("Null root adapter expected");
#endif
                RootAdapter = ans;
            }
            else
            {
                parent.NestedAdapters[routeName + string.Empty] = ans;
            }

            if (ans.Adapter is INavigationAware navAware)
            {
                navAware.Navigation = ans;
            }

            //AdapterLookup[control] = ans;
            return ans;
        }

        public void RemoveAdapter(INavigationService adapter)
        {
            var ans = adapter as ActiveNavigationService;
            if (ans is null)
            {
                return;
            }

            // Detach adapter from parent
            var parent = adapter.ParentNavigation() as ActiveNavigationService;
            if (parent is not null)
            {
                parent.NestedAdapters.Remove(kvp => kvp.Value == ans);
            }

            //// Remove adapter from control lookup
            //AdapterLookup.Remove(kvp => kvp.Value == ans);
        }


        //public INavigationService ScopedServiceForControl(object control)
        //{
        //    if (control is null)
        //    {
        //        return null;
        //    }

        //    if (AdapterLookup.TryGetValue(control, out var adapter))
        //    {
        //        return adapter;
        //    }

        //    return null;
        //}

        public NavigationResponse Navigate(NavigationRequest request)
        {
            return NavigateWithAdapter(request, RootAdapter);
        }

        public NavigationResponse NavigateWithAdapter(NavigationRequest request, ActiveNavigationService navService)
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
                navService = navService.ParentAdapter;
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
                        if (navService.Adapter.IsCurrentPath(navPath))
                        {
                            navService = navService.NestedAdapters[string.Empty];
                        }
                        else
                        {
                            navService = navService.NestedAdapters[navPath];
                        }
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
            navWrapper.Navigation = navService;

            var context = new NavigationContext(services, request, navPath, isRooted, numberOfPagesToRemove, paras, new CancellationTokenSource(), new TaskCompletionSource<object>());
            return navService.Adapter.Navigate(context);
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
            return RootAdapter;
        }

        //public INavigationService ParentNavigation(INavigationAdapter adapter)
        //{
        //    var idx = Adapters.IndexOf(adapter);
        //    if (idx < 0)
        //    {
        //        return null;
        //    }

        //    if (idx < Adapters.Count - 1)
        //    {
        //        idx++;
        //    }

        //    return new ActiveNavigationService(this, Adapters[idx]);
        //}

        public INavigationService NestedNavigation(string routeName = null)
        {
            return RootAdapter.NestedNavigation(routeName);
        }

        //public INavigationService NestedNavigation(INavigationAdapter adapter, string routeName = null)
        //{
        //    var idx = Adapters.IndexOf(adapter);
        //    if (idx < 0)
        //    {
        //        return null;
        //    }

        //    while (idx > 0 && (string.IsNullOrWhiteSpace(routeName) || Adapters[idx].Name != routeName))
        //    {
        //        idx--;
        //    }

        //    return new ActiveNavigationService(this, Adapters[idx]);
        //}
    }
}
