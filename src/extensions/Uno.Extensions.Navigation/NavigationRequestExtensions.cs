using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
    public static bool RequiresResponse(this NavigationRequest request)
    {
        return request.Result is not null;
    }

    public static NavigationRequest WithPath(this NavigationRequest request, string path, string queryParameters = "")
    {
        return string.IsNullOrWhiteSpace(path) ? null : request with { Route = new Uri(path + (!string.IsNullOrWhiteSpace(queryParameters) ? $"?{queryParameters}" : string.Empty), UriKind.Relative).BuildRoute() };
    }

    public static NavigationRequest AsRequest(this RouteMap map, object sender)
    {
        var request = new NavigationRequest(sender, new Uri(map.Path, UriKind.Relative).BuildRoute());
        return request;
    }

    public static NavigationRequest AsRequest(this string uri, object sender, object data = null)
    {
        var request = new NavigationRequest(sender, new Uri(uri, UriKind.Relative).BuildRoute(data));
        return request;
    }

    public static NavigationContext BuildNavigationContext(this NavigationRequest request, IServiceProvider services)
    {
        var scopedServices = services.CloneNavigationScopedServices();
        var dataFactor = scopedServices.GetService<ViewModelDataProvider>();
        dataFactor.Parameters = request.Route.Data;

        var mapping = scopedServices.GetService<IRouteMappings>().FindByPath(request.Route.Base);

        var context = new NavigationContext(
                            scopedServices,
                            request,
                            (request.Cancellation is not null) ?
                                CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation.Value) :
                                new CancellationTokenSource(),
                            mapping);
        return context;
    }
}
