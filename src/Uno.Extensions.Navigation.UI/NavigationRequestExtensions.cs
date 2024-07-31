namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
	public static object? RouteResourceView(this NavigationRequest request, IRegion region)
	{
		object? resource;
		if ((request.Sender is FrameworkElement senderElement &&
			senderElement.Resources.TryGetValue(request.Route.Base, out resource)) ||

			(region.View is FrameworkElement regionElement &&
			regionElement.Resources.TryGetValue(request.Route.Base, out resource)) ||

			(Application.Current.Resources.TryGetValue(request.Route.Base, out resource)))
		{
			return resource;
		}

		return default;
	}

	public static NavigationRequest AsInternal(this NavigationRequest request)
	{
		return request with { Route = request.Route.AsInternal() };
	}

	public static NavigationRequest WithData(this NavigationRequest request, object? data)
		=> data is null ?
				request :
				request with
				{
					Route = request.Route with
					{
						Data = request.Route.Data.Combine(new Dictionary<string, object>() { { string.Empty, data } })
					}
				};

	public static NavigationRequest IncludeDependentRoutes(this NavigationRequest request, IRouteResolver resolver)
	{
		if (request.Route.Base is not { Length: > 0 })
		{
			return request;
		}

		var rm = resolver.FindByPath(request.Route.Base);
		while (rm?.DependsOnRoute is not null)
		{
			request = request with { Route = (request.Route with { Base = rm.DependsOnRoute.Path, Path = null }).Append(request.Route) };
			rm = rm.DependsOnRoute;
		}
		return request;
	}
}
