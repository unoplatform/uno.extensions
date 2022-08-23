using Uno.Extensions.Navigation;

namespace Uno.Extensions.Authentication;

public static class AuthenticationFlowExtensions
{
	internal static Task<NavigationResponse?> AuthenticatedNavigateRouteHintAsync(
	this IAuthenticationFlow service, INavigator? navigator, RouteHint routeHint, object sender, object? data, CancellationToken cancellation)
	{
		navigator ??= service.Navigator;
		if(navigator is null)
		{
			return Task.FromResult<NavigationResponse?>(default);
		}
		var resolver = navigator.GetResolver();
		var request = routeHint.ToRequest(navigator, resolver, sender, data, cancellation);
		return service.AuthenticatedNavigateAsync(request, navigator, cancellation);
	}


	public static Task<NavigationResponse?> AuthenticatedNavigateRouteAsync(
		this IAuthenticationFlow service, object sender, string route, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { Route = route, Qualifier = qualifier, Data = data?.GetType() };
		return service.AuthenticatedNavigateRouteHintAsync(navigator, hint, sender, data, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewAsync<TView>(
		this IAuthenticationFlow service, object sender, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		return service.AuthenticatedNavigateViewAsync(sender, typeof(TView), navigator, qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewAsync(
		this IAuthenticationFlow service, object sender, Type viewType, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { View = viewType, Qualifier = qualifier, Data = data?.GetType() };
		return service.AuthenticatedNavigateRouteHintAsync(navigator, hint, sender, data, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewModelAsync<TViewViewModel>(
		this IAuthenticationFlow service, object sender, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		return service.AuthenticatedNavigateViewModelAsync(sender, typeof(TViewViewModel), navigator, qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewModelAsync(
		this IAuthenticationFlow service, object sender, Type viewModelType, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		var hint = new RouteHint { ViewModel= viewModelType, Qualifier = qualifier, Data = data?.GetType() };
		return service.AuthenticatedNavigateRouteHintAsync(navigator, hint, sender, data, cancellation);
	}
}
