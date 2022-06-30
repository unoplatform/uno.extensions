namespace Uno.Extensions.Authentication;

internal record AuthenticationService
(
	IEnumerable<IProviderFactory> ProviderFactories,
	ITokenCache Tokens
) : IAuthenticationService
{
	private readonly IDictionary<string, IAuthenticationProvider> _providers = new Dictionary<string, IAuthenticationProvider>();

	public string[] Providers => _providers.Keys.ToArray();

	public async ValueTask<bool> CanRefresh(CancellationToken? cancellationToken = default) => await Tokens.HasTokenAsync(cancellationToken) && await AuthenticationProvider().CanRefresh(cancellationToken ?? CancellationToken.None);

	public async ValueTask<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default)
	{
		var authProvider = AuthenticationProvider(provider);

		var tokens = await authProvider.LoginAsync(dispatcher, credentials, cancellationToken ?? CancellationToken.None);
		if (!await Tokens.SaveAsync(authProvider.Name, tokens, cancellationToken))
		{
			return false;
		}
		return await Tokens.HasTokenAsync(cancellationToken);
	}

	public async ValueTask<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken? cancellationToken = default)
	{
		var authProvider = AuthenticationProvider();
		if (!await authProvider.LogoutAsync(dispatcher, cancellationToken ?? CancellationToken.None))
		{
			return false;
		}

		return await Tokens.ClearAsync(cancellationToken);
	}

	public async ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default)
	{
		var authProvider = AuthenticationProvider();
		if (await CanRefresh())
		{
			var tokens = await authProvider.RefreshAsync(cancellationToken ?? CancellationToken.None);
			if (!await Tokens.SaveAsync(authProvider.Name, tokens, cancellationToken))
			{
				return false;
			}

			// Successful refresh requires there to be tokens stored
			return await Tokens.HasTokenAsync(cancellationToken);
		}

		// If not able to refresh, either no tokens, or some other provider specific reason,
		// return false to indicate the user should not be treated as authenticated.
		return false;
	}

	private IAuthenticationProvider AuthenticationProvider(string? provider = null)
	{
		if (provider is null)
		{
			provider = Tokens.CurrentProvider;
		}

		if (_providers.Count == 0)
		{
			BuildProviders();
		}

		return _providers.TryGetValue(provider ?? string.Empty, out var authProvider) ? authProvider : _providers.First().Value;
	}

	private void BuildProviders()
	{
		foreach (var factory in ProviderFactories)
		{
			_providers[factory.Name] = factory.AuthenticationProvider;
		}
	}
}
