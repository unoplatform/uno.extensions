
namespace Uno.Extensions.Authentication;

internal record AuthenticationFlow : IAuthenticationFlow
{
	public IAuthenticationService AuthenticationService { get; init; }
	public INavigator? Navigator { get; private set; }
	public IDispatcher? Dispatcher { get; private set; }
	public ITokenCache TokenCache { get; init; }
	public AuthenticationFlowSettings Settings { get; init; }

	private TaskCompletionSource<NavigationResponse?>? _loginTask;
	private Func<INavigator, IDispatcher, Task>? _loginCompletedOverride;

	public AuthenticationFlow(
		IAuthenticationService authenticationService,
		ITokenCache tokenCache,
		AuthenticationFlowSettings settings)
	{
		AuthenticationService = authenticationService;
		TokenCache = tokenCache;
		Settings = settings;

		TokenCache.Cleared += CacheCleared;
	}

	public void Initialize(IDispatcher dispatcher, INavigator navigator)
	{
		Dispatcher = dispatcher;
		Navigator = navigator;
	}

	private async void CacheCleared(object? sender, EventArgs e)
	{
		if (Settings.LogoutCallback is not null)
		{
			await Settings.LogoutCallback(Navigator!, Dispatcher!);
		}
	}

	public async Task<NavigationResponse?> AuthenticatedNavigateAsync(NavigationRequest request, INavigator? navigator=default, CancellationToken ct = default)
	{
		// Make sure we have valid navigator
		navigator = navigator ?? Navigator;
		if(navigator is null)
		{
			return default;
		}

		var authenticated = await AuthenticationService.RefreshAsync(ct);
		if (authenticated)
		{
			return await navigator.NavigateAsync(request);
		}
		else
		{
			_loginTask = new TaskCompletionSource<NavigationResponse?>();
			_loginCompletedOverride = async (nav, dispatcher) =>
			{
				var response = await navigator.NavigateAsync(request);
				_loginCompletedOverride = null;
				_loginTask.TrySetResult(response);
				_loginTask = null;
			};

			if (Settings.LoginRequiredCallback is not null)
			{
				await Settings.LoginRequiredCallback(Navigator!, Dispatcher!);
			}
			else
			{
				return default;
			}

			return await _loginTask.Task;
		}
	}


	public async Task<bool> EnsureAuthenticatedAsync(CancellationToken ct = default)
	{
		if(_loginTask is not null)
		{
			_loginTask.TrySetCanceled();
			_loginTask = null;
			_loginCompletedOverride = null;
		}

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


	public async Task<bool> LoginAsync(IDictionary<string, string>? credentials, CancellationToken ct = default)
	{
		var loginResult = await AuthenticationService.LoginAsync(Dispatcher!, credentials, ct);
		if (loginResult)
		{
			if(_loginCompletedOverride is not null)
			{
				await _loginCompletedOverride(Navigator!, Dispatcher!);
			}
			else if (Settings.LoginCompletedCallback is not null)
			{
				await Settings.LoginCompletedCallback(Navigator!, Dispatcher!);
			}
			return true;
		}
		return false;
	}

	public async Task<bool> LogoutAsync(CancellationToken ct = default)
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
