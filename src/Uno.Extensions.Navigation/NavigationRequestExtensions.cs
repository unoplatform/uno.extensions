
namespace Uno.Extensions.Navigation;

public static class NavigationRequestExtensions
{
	public static NavigationRequest? AsRequest(this string path, IRouteResolver? resolver, object sender, object? data, CancellationToken cancellationToken, Type? resultType = null)
	{
		if (resultType is null)
		{
			return AsRequest(path, resolver, sender, data, cancellationToken);
		}

		var navMethods = typeof(NavigationRequestExtensions)
					.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
					.Where(m => m.Name == nameof(AsRequest) &&
								m.IsGenericMethodDefinition).ToArray();
		var navMethod = navMethods.First();
		var constructedNavMethod = navMethod.MakeGenericMethod(resultType);
		var nav = constructedNavMethod.Invoke(null, new object?[] { path, sender, data, cancellationToken }) as NavigationRequest;
		return nav;
	}

	public static NavigationRequest AsRequest<TResult>(this string path, IRouteResolver? resolver, object sender, object? data = null, CancellationToken cancellationToken = default)
	{
		var request = new NavigationRequest<TResult>(sender, path.AsRoute(data, resolver), cancellationToken);
		return request;
	}

	public static NavigationRequest AsRequest(this string path, IRouteResolver? resolver, object sender, object? data = null, CancellationToken cancellationToken = default)
	{
		var request = new NavigationRequest(sender, path.AsRoute(data, resolver), cancellationToken);
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
