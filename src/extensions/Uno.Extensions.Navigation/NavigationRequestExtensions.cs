using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
    public static bool RequiresResponse(this NavigationRequest request)
    {
        return request.Result is not null;
    }

    public static NavigationRequest AsRequest<TResult>(this RouteMap map, object sender, object data = null, CancellationToken cancellationToken = default)
    {
        return map.AsRequest(sender, data, cancellationToken, typeof(TResult));
    }

    public static NavigationRequest AsRequest(this RouteMap map, object sender, object data = null, CancellationToken cancellationToken = default, Type resultType = null)
    {
        return map.Path.AsRequest(sender, data, cancellationToken, resultType);
    }

    public static NavigationRequest AsRequest<TResult>(this string path, object sender, object data = null, CancellationToken cancellationToken = default)
    {
        return path.AsRequest(sender, data, cancellationToken, typeof(TResult));
    }

    public static NavigationRequest AsRequest(this string path, object sender, object data = null, CancellationToken cancellationToken = default, Type resultType = null)
    {
        var request = new NavigationRequest(sender, path.AsRoute(data), cancellationToken, resultType);
        return request;
    }

    public static NavigationContext BuildNavigationContext(
        this NavigationRequest request,
        IServiceProvider services,
        TaskCompletionSource<Options.Option> resultTask = default)
    {
        var scopedServices = services;//.CloneNavigationScopedServices();

        var mapping = scopedServices.GetService<IRouteMappings>().FindByPath(request.Route.Base);

        var dataFactor = scopedServices.GetService<ViewModelDataProvider>();
        dataFactor.Parameters = request.Route.Data;
        var cancel = (request.Cancellation is not null) ?
                                CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation.Value) :
                                new CancellationTokenSource();
        var context = new NavigationContext(
                            scopedServices,
                            request with
                            {
                                Cancellation = cancel.Token,
                                Route = request.Route with
                                {
                                    Data = request.Route.Data.AsParameters(mapping)
                                }
                            },
                            cancel,
                            mapping);

        if (request.RequiresResponse())
        {
            var innerService = context.Services.GetInstance<INavigator>();
            _ = new ResponseNavigator(innerService, request.Result, resultTask);
        }

        return context;
    }
}
