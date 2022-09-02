namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationProvider
(
	ILogger<CustomAuthenticationProvider> ProviderLogger,
	IServiceProvider Services,
	ITokenCache Tokens,
	CustomAuthenticationSettings? Settings = null
) : BaseAuthenticationProvider(ProviderLogger, DefaultName, Tokens)
{
	public const string DefaultName = "Custom";
	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (Settings?.LoginCallback is null)
		{
			return default;
		}
		return await Settings.LoginCallback(Services, dispatcher, Tokens, credentials!, cancellationToken);
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		if (Settings?.LogoutCallback is null)
		{
			return true;
		}
		return await Settings.LogoutCallback(Services, dispatcher, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings?.RefreshCallback is null)
		{
			return default;
		}
		return await Settings.RefreshCallback(Services, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}
}


internal record CustomAuthenticationProvider<TService>
(
	ILogger<CustomAuthenticationProvider<TService>> ProviderLogger,
	IServiceProvider Services,
	ITokenCache Tokens,
	CustomAuthenticationSettings<TService>? Settings=null
) : BaseAuthenticationProvider(ProviderLogger, CustomAuthenticationProvider.DefaultName, Tokens)
	where TService: class
{
	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (Settings?.LoginCallback is null)
		{
			return default;
		}
		var service = Services.GetRequiredService<TService>();
		return await Settings.LoginCallback(service, Services, dispatcher, Tokens, credentials!, cancellationToken);
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		if (Settings?.LogoutCallback is null)
		{
			return true;
		}
		var service = Services.GetRequiredService<TService>();
		return await Settings.LogoutCallback(service, Services, dispatcher, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings?.RefreshCallback is null)
		{
			return default;
		}
		var service = Services.GetRequiredService<TService>();
		return await Settings.RefreshCallback(service, Services, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}
}
