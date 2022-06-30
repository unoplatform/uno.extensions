namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationProvider
(
	IServiceProvider Services,
	ITokenCache Tokens,
	CustomAuthenticationSettings? Settings = null
) : BaseAuthenticationProvider(DefaultName, Tokens)
{
	public const string DefaultName = "Custom";
	public async override ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (Settings?.LoginCallback is null)
		{
			return default;
		}
		return await Settings.LoginCallback(Services, dispatcher, credentials!, cancellationToken);
	}

	public async override ValueTask<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		if (Settings?.LogoutCallback is null)
		{
			return true;
		}
		return await Settings.LogoutCallback(Services, dispatcher, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}

	public async override ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings?.RefreshCallback is null)
		{
			return default;
		}
		return await Settings.RefreshCallback(Services, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}
}


internal record CustomAuthenticationProvider<TService>
(
	IServiceProvider Services,
	ITokenCache Tokens,
	CustomAuthenticationSettings<TService>? Settings=null
) : BaseAuthenticationProvider(CustomAuthenticationProvider.DefaultName, Tokens)
	where TService: class
{
	public async override ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (Settings?.LoginCallback is null)
		{
			return default;
		}
		return await Settings.LoginCallback(Services.GetRequiredService<TService>(), dispatcher, credentials!, cancellationToken);
	}

	public async override ValueTask<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		if (Settings?.LogoutCallback is null)
		{
			return true;
		}
		return await Settings.LogoutCallback(Services.GetRequiredService<TService>(), dispatcher, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}

	public async override ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings?.RefreshCallback is null)
		{
			return default;
		}
		return await Settings.RefreshCallback(Services.GetRequiredService<TService>(), await Tokens.GetAsync(cancellationToken), cancellationToken);
	}
}
