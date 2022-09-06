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
		if (Settings?.LoginCallback is not null)
		{
			return await Settings.LoginCallback(Services, dispatcher, Tokens, credentials!, cancellationToken);
		}

		return await base.InternalLoginAsync(dispatcher, credentials, cancellationToken);
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		if (Settings?.LogoutCallback is not null)
		{
			return await Settings.LogoutCallback(Services, dispatcher, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
		}
		return await base.InternalLogoutAsync(dispatcher, cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings?.RefreshCallback is not null)
		{
			return await Settings.RefreshCallback(Services, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
		}
		return await base.InternalRefreshAsync(cancellationToken);
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
		if (Settings?.LoginCallback is not null)
		{
			var service = Services.GetRequiredService<TService>();
			return await Settings.LoginCallback(service, Services, dispatcher, Tokens, credentials!, cancellationToken);
		}

		return await base.InternalLoginAsync(dispatcher,credentials,cancellationToken);
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		if (Settings?.LogoutCallback is not null)
		{
			var service = Services.GetRequiredService<TService>();
			return await Settings.LogoutCallback(service, Services, dispatcher, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
		}
		return await base.InternalLogoutAsync(dispatcher,cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings?.RefreshCallback is not null)
		{
			var service = Services.GetRequiredService<TService>();
			return await Settings.RefreshCallback(service, Services, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
		}
		return await base.InternalRefreshAsync(cancellationToken);
	}
}
