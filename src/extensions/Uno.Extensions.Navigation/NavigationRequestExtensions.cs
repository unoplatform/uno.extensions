using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
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
}
