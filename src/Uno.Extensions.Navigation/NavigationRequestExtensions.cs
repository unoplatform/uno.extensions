namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
	internal static Route ToRoute(
		this RouteHint hint,
		INavigator navigator,
		IRouteResolver resolver,
		object? data)
	{
		var path = hint.Route;

		if (path is null ||
			string.IsNullOrWhiteSpace(path))
		{
			RouteInfo? maps = default;
			if (hint.View is not null)
			{
				maps = resolver.FindByView(hint.View, navigator);
			}
			if (maps is null &&
				hint.ViewModel is not null)
			{
				maps = resolver.FindByViewModel(hint.ViewModel, navigator);
			}
			if (maps is null &&
				hint.Result is not null)
			{
				maps = resolver.FindByResultData(hint.Result, navigator);
			}

			if (maps is null &&
				data is not null)
			{
				maps = resolver.FindByData(data.GetType(), navigator);
			}

			if (maps is not null)
			{
				path = maps.Path;
			}
		}


		// Apply any qualifier specified in the hint
		path = path?.WithQualifier(hint.Qualifier) ?? hint.Qualifier;

		if ((path is null ||
			string.IsNullOrWhiteSpace(path)) &&
			data is null)
		{
			return Route.Empty;
		}

		var queryIdx = path?.IndexOf('?') ?? -1;
		var query = string.Empty;
		if (queryIdx >= 0 && path is not null)
		{
			queryIdx++; // Step over the ?
			query = queryIdx < path.Length ? path.Substring(queryIdx) : string.Empty;
			path = path.Substring(0, queryIdx - 1);
		}

		var paras = RouteExtensions.ParseQueryParameters(query);
		if (data is not null)
		{
			if (data is IDictionary<string, object> paraDict)
			{
				foreach (var p in paraDict)
				{
					paras.Add(p);
				}
			}
			else
			{
				paras[string.Empty] = data;
			}
		}

		var routeBase = RouteExtensions.ExtractBase(path, out var qualifier, out path);

		if (resolver is not null &&
			!string.IsNullOrWhiteSpace(routeBase) &&
			string.IsNullOrWhiteSpace(qualifier))
		{
			var map = resolver.FindByPath(routeBase);
			if (map?.IsDialogViewType?.Invoke() ?? false)
			{
				qualifier = Qualifiers.Dialog;
			}
		}

		var route = new Route(qualifier, routeBase, path, paras);
		return route;
	}

	internal static NavigationRequest ToRequest(
		this RouteHint hint,
		INavigator navigator,
		IRouteResolver resolver,
		object sender,
		object? data,
		CancellationToken cancellation)
	{
		if (hint.Result is null)
		{
			var request = new NavigationRequest(sender, hint.ToRoute(navigator, resolver, data), cancellation);
			return request;
		}
		var navMethods = typeof(NavigationRequestExtensions)
					.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(m => m.Name == nameof(ToRequest) &&
								m.IsGenericMethodDefinition).ToArray();
		var navMethod = navMethods.First();
		var constructedNavMethod = navMethod.MakeGenericMethod(hint.Result);
		var nav = (NavigationRequest)constructedNavMethod.Invoke(null, new object?[] { hint, navigator, resolver, sender, data, cancellation })!;
		return nav;

	}

	internal static NavigationRequest<TResult> ToRequest<TResult>(
		this RouteHint hint,
		INavigator navigator,
		IRouteResolver resolver,
		object sender,
		object? data,
		CancellationToken cancellation)
	{
		var request = new NavigationRequest<TResult>(sender, hint.ToRoute(navigator, resolver, data), cancellation);
		return request;
	}

	public static bool SameRouteBase(this NavigationRequest request, NavigationRequest newRequest)
	{
		if (request?.Route is null || newRequest?.Route is null)
		{
			return false;
		}

		return request.Route.Qualifier == newRequest.Route.Qualifier &&
			request.Route.Base == newRequest.Route.Base;
	}
}
