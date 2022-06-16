
namespace Uno.Extensions.Authentication;

internal record AuthenticationFlow : IAuthenticationFlow
{
	public IAuthenticationService AuthenticationService { get; init; }
	public INavigator? Navigator { get; private set; }
	public IDispatcher? Dispatcher { get; private set; }
	public ITokenCache TokenCache { get; init; }
	public AuthenticationFlowSettings Settings { get; init; }

	public AuthenticationFlow(
		IAuthenticationService authenticationService,
		ITokenCache tokenCache,
		AuthenticationFlowSettings settings)
	{
		AuthenticationService = authenticationService;
		TokenCache = tokenCache;
		Settings = settings;

		TokenCache.Cleared += TokenCache_Cleared;
	}

	public void Initialize(IDispatcher dispatcher, INavigator navigator)
	{
		Dispatcher = dispatcher;
		Navigator = navigator;
	}

	private async void TokenCache_Cleared(object? sender, EventArgs e)
	{
		if (Settings.LogoutCallback is not null)
		{
			await Settings.LogoutCallback(Navigator!, Dispatcher!);
		}
	}


	public async Task<bool> EnsureAuthenticatedAsync(CancellationToken ct)
	{
		var refreshed = await AuthenticationService.RefreshAsync(ct);
		if (refreshed)
		{
			return true;
		}

		if (Settings.LoginRequiredCallback is not null)
		{
			await Settings.LoginRequiredCallback(Navigator!, Dispatcher!);
		}
		return false;
	}


	public async Task<bool> LoginAsync(IDictionary<string, string>? credentials, CancellationToken ct)
	{
		var loginResult = await AuthenticationService.LoginAsync(Dispatcher!, credentials, ct);
		if (loginResult && Settings.LoginCompletedCallback is not null)
		{
			await Settings.LoginCompletedCallback(Navigator!, Dispatcher!);
			return true;
		}
		return false;
	}

	public async Task<bool> LogoutAsync(CancellationToken ct)
	{
		var logoutResult = await AuthenticationService.LogoutAsync(Dispatcher!,ct);
		if (logoutResult)
		{
			// We don't need to do anything else here because the Token Cleared event
			// will be raised, forcing the LogoutCallback to be invoked
			return true;
		}
		return false;
	}
}
