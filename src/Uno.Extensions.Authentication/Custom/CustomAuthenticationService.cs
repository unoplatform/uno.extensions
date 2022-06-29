namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationService
(
	IServiceProvider Services,
	ITokenCache Tokens,
	CustomAuthenticationSettings Settings
) : BaseAuthenticationService(Tokens)
{
	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (Settings.LoginCallback is null)
		{
			return default;
		}
		return await Settings.LoginCallback(Services, dispatcher, credentials!, cancellationToken);
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		if (Settings.LogoutCallback is null)
		{
			return true;
		}
		return await Settings.LogoutCallback(Services, dispatcher, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings.RefreshCallback is null)
		{
			return default;
		}
		return await Settings.RefreshCallback(Services, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}
}


internal record CustomAuthenticationService<TService>
(
	IServiceProvider Services,
	ITokenCache Tokens,
	CustomAuthenticationSettings<TService> Settings
) : BaseAuthenticationService(Tokens)
	where TService: class
{
	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (Settings.LoginCallback is null)
		{
			return default;
		}
		return await Settings.LoginCallback(Services.GetRequiredService<TService>(), dispatcher, credentials!, cancellationToken);
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		if (Settings.LogoutCallback is null)
		{
			return true;
		}
		return await Settings.LogoutCallback(Services.GetRequiredService<TService>(), dispatcher, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings.RefreshCallback is null)
		{
			return default;
		}
		return await Settings.RefreshCallback(Services.GetRequiredService<TService>(), await Tokens.GetAsync(cancellationToken), cancellationToken);
	}
}
