using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
#if !WINUI
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
    public static NavigationRequest AsRequest<TResult>(this RouteMap map, object sender, object? data = null, CancellationToken cancellationToken = default)
    {
        return map.AsRequest(sender, data, cancellationToken, typeof(TResult));
    }

    public static NavigationRequest AsRequest(this RouteMap map, object sender, object? data = null, CancellationToken cancellationToken = default, Type? resultType = null)
    {
        return map.Path.AsRequest(sender, data, cancellationToken, resultType);
    }

    public static NavigationRequest AsRequest<TResult>(this string path, object sender, object? data = null, CancellationToken cancellationToken = default)
    {
        return path.AsRequest(sender, data, cancellationToken, typeof(TResult));
    }

    public static NavigationRequest AsRequest(this string path, object sender, object? data = null, CancellationToken cancellationToken = default, Type? resultType = null)
    {
        var request = new NavigationRequest(sender, path.AsRoute(data), cancellationToken, resultType);
        return request;
    }

    public static object? RouteResourceView(this NavigationRequest request, IRegion region)
    {
        object resource;
        if ((request.Sender is FrameworkElement senderElement &&
            senderElement.Resources.TryGetValue(request.Route.Base, out resource)) ||

            (region.View is FrameworkElement regionElement &&
            regionElement.Resources.TryGetValue(request.Route.Base, out resource)) ||

            (Application.Current.Resources.TryGetValue(request.Route.Base, out resource)))
        {
            return resource;

        }

        return null;
    }

	public static bool SameRouteBase(this NavigationRequest request, NavigationRequest newRequest)
	{
		if(request?.Route is null || newRequest?.Route is null)
		{
			return false;
		}

		return request.Route.Scheme == newRequest.Route.Scheme &&
			request.Route.Base == newRequest.Route.Base;
	}
}
