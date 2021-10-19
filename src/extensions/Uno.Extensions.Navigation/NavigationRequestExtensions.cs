using System;
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

    public static NavigationRequest AsRequest(this RouteMap map, object sender)
    {
        var request = new NavigationRequest(sender, new Uri(map.Path, UriKind.Relative).AsRoute());
        return request;
    }

    public static NavigationContext BuildNavigationContext(this NavigationRequest request, IServiceProvider services, TaskCompletionSource<Options.Option> resultTask = default)
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

        if (request.RequiresResponse())
        {
            var innerService = context.Services.GetInstance<INavigator>();
            var responseNav = new ResponseNavigator(innerService, resultTask);
            context.Services.AddInstance<INavigator>(responseNav);
        }

        return context;
    }
}
