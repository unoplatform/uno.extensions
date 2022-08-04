namespace Uno.Extensions.Authentication;

public static class AuthenticationFlowExtensions
{
	public static Task<NavigationResponse?> AuthenticatedNavigateRouteAsync(
		this IAuthenticationFlow service, object sender, string route, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = (navigator ?? (service as AuthenticationFlow)?.Navigator)?.GetResolver();
		if (string.IsNullOrWhiteSpace(route))
		{
			var map = (data is not null) ?
							resolver?.FindByData(data.GetType()) :
							resolver?.Find(default!);
			if (map is null)
			{
				return Task.FromResult<NavigationResponse?>(null);
			}

			route = map.Path;
		}

		return service.AuthenticatedNavigateAsync(route.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation), navigator, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewAsync<TView>(
		this IAuthenticationFlow service, object sender, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		return service.AuthenticatedNavigateViewAsync(sender, typeof(TView), navigator, qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewAsync(
		this IAuthenticationFlow service, object sender, Type viewType, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = (navigator ?? (service as AuthenticationFlow)?.Navigator)?.GetResolver();
		var map = resolver?.FindByView(viewType);
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.AuthenticatedNavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation), navigator, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewModelAsync<TViewViewModel>(
		this IAuthenticationFlow service, object sender, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		return service.AuthenticatedNavigateViewModelAsync(sender, typeof(TViewViewModel), navigator, qualifier, data, cancellation);
	}

	public static Task<NavigationResponse?> AuthenticatedNavigateViewModelAsync(
		this IAuthenticationFlow service, object sender, Type viewModelType, INavigator? navigator = default, string qualifier = Qualifiers.ClearBackStack, object? data = null, CancellationToken cancellation = default)
	{
		var resolver = (navigator ?? (service as AuthenticationFlow)?.Navigator)?.GetResolver();
		var map = resolver?.FindByViewModel(viewModelType);
		if (map is null)
		{
			return Task.FromResult<NavigationResponse?>(null);
		}
		return service.AuthenticatedNavigateAsync(map.Path.WithQualifier(qualifier).AsRequest(resolver, sender, data, cancellation), navigator, cancellation);
	}
}
