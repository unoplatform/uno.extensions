using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public class NavigationService : INavigationService
{
    public INavigationManager Navigation { get; }

    public INavigationAdapter Adapter { get; set; }

    public INavigationService Parent { get; }

    public NavigationService(INavigationManager manager, INavigationService parent)
    {
        Navigation = manager;
        Parent = parent;
    }

    public IDictionary<string, INavigationService> NestedAdapters { get; } = new Dictionary<string, INavigationService>();

    public NavigationResponse Navigate(NavigationRequest request)
    {
        return NavigateWithAdapter(request, this);
    }

    public INavigationService Nested(string routeName = null)
    {
        return NestedAdapters.TryGetValue(routeName + string.Empty, out var service) ? service : null;
    }

    private NavigationResponse NavigateWithAdapter(NavigationRequest request, NavigationService navService)
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
            navService = navService.Parent as NavigationService;
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
            if (segments[i] == NavigationConstants.PreviousViewUri)
            {
                numberOfPagesToRemove++;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(navPath))
                {
                    if (navService.Adapter.IsCurrentPath(navPath))
                    {
                        navService = navService.Nested(string.Empty) as NavigationService;
                    }
                    else
                    {
                        navService = navService.Nested(navPath) as NavigationService;
                    }
                }
                navPath = segments[i];
            }
        }

        if (navPath == string.Empty)
        {
            navPath = NavigationConstants.PreviousViewUri;
            numberOfPagesToRemove--;
        }

        var scope = navService.Adapter.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dataFactor = services.GetService<ViewModelDataProvider>();
        dataFactor.Parameters = paras;
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
}
