
namespace Uno.Extensions.Authentication;

public record AuthenticationFlow : IAuthenticationFlow
{
	public IAuthenticationService AuthenticationService { get; init; }
	public INavigator Navigator { get; init; }
	public IDispatcher Dispatcher { get; init; }
	public ITokenRepository TokenCache { get; init; }
	public AuthenticationFlowSettings Settings { get; init; }

	public AuthenticationFlow(
		IAuthenticationService authenticationService,
		INavigator navigator,
		IDispatcher dispatcher,
		ITokenRepository tokenCache,
		AuthenticationFlowSettings settings)
	{
		AuthenticationService = authenticationService;
		Navigator = navigator;
		Dispatcher = dispatcher;
		TokenCache = tokenCache;
		Settings = settings;

		TokenCache.Cleared += TokenCache_Cleared;
	}

	private void TokenCache_Cleared(object sender, EventArgs e)
	{
		_ = Launch();
	}

	public async Task Launch()
	{
		var authenticated = await EnsureAuthenticated();
		if (authenticated)
		{
			await NavigateToHome();
		}
		else
		{
			await NavigateToError();
		}
	}

	public async Task<bool> EnsureAuthenticated()
	{
		var refreshed = await AuthenticationService.Refresh();
		if (refreshed)
		{
			return true;
		}

		var tokenResponse = await NavigateToLogin();
		return tokenResponse.IsSome(out _);
	}

	private async ValueTask<Option<ITokenRepository>> NavigateToLogin()
	{
		if (Settings.LoginViewModel is not null)
		{
			return await Navigator.NavigateViewModelForResultAsync<ITokenRepository>(this, Settings.LoginViewModel, qualifier: Qualifiers.Root).AsResult();
		}
		else if (Settings.LoginView is not null)
		{
			return await Navigator.NavigateViewForResultAsync<ITokenRepository>(this, Settings.LoginView, qualifier: Qualifiers.Root).AsResult();
		}
		else if (Settings.LoginRoute is not null)
		{
			return await Navigator.NavigateRouteForResultAsync<ITokenRepository>(this, Settings.LoginRoute ?? string.Empty).AsResult();
		}
		return Option<ITokenRepository>.None();
	}
	private Task<NavigationResponse?> NavigateToHome()
	{
		return Navigate(Settings.HomeViewModel, Settings.HomeView, Settings.HomeRoute);
	}
	private Task<NavigationResponse?> NavigateToError()
	{
		return Navigate(Settings.ErrorViewModel, Settings.ErrorView, Settings.ErrorRoute);
	}

	private Task<NavigationResponse?> Navigate(Type? viewModel = null, Type? view = null, string? route = null)
	{
		if (viewModel is not null)
		{
			return Navigator.NavigateViewModelAsync(this, viewModel, qualifier: Qualifiers.Root);
		}
		else if (view is not null)
		{
			return Navigator.NavigateViewAsync(this, view, qualifier: Qualifiers.Root);
		}
		else if (route is not null)
		{
			return Navigator.NavigateRouteAsync(this, route ?? string.Empty);
		}
		return Task.FromResult(default(NavigationResponse?));
	}
	public async Task<bool> Login(IDictionary<string, string>? credentials = null)
	{
		var loginResult = await AuthenticationService.Login(Dispatcher, credentials);
		if (loginResult)
		{
			await Navigator.NavigateBackWithResultAsync(this, data: TokenCache);
			return true;
		}
		return false;
	}

	public async Task<bool> Logout()
	{
		var logoutResult = await AuthenticationService.Logout(Dispatcher);
		if (logoutResult)
		{
			_ = Launch();
			return true;
		}
		return false;
	}
}
