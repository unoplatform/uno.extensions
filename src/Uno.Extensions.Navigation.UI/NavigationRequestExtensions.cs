using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
	public static NavigationRequest AsRequest(this string path, object sender, object? data, CancellationToken cancellationToken, Type? resultType = null)
	{
		if(resultType is null)
		{
			return AsRequest(path, sender, data, cancellationToken);
		}

		var navMethods = typeof(NavigationRequestExtensions)
					.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
					.Where(m => m.Name == nameof(AsRequest) &&
								m.IsGenericMethodDefinition).ToArray();
		var navMethod = navMethods.First();
		var constructedNavMethod = navMethod.MakeGenericMethod(resultType);
		var nav = constructedNavMethod.Invoke(null, new object[] { path, sender, data, cancellationToken }) as NavigationRequest;
		return nav;
	}

	public static NavigationRequest AsRequest<TResult>(this string path, object sender, object? data = null, CancellationToken cancellationToken = default)
    {
		var request = new NavigationRequest<TResult>(sender, path.AsRoute(data), cancellationToken);
		return request;
	}

	public static NavigationRequest AsRequest(this string path, object sender, object? data = null, CancellationToken cancellationToken = default)
    {
        var request = new NavigationRequest(sender, path.AsRoute(data), cancellationToken);
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
