namespace Uno.Extensions.Authentication;

internal class AuthenticationService : IAuthenticationService
{
	public event EventHandler? LoggedOut;

	private readonly ILogger _logger;
	private readonly IEnumerable<IProviderFactory> _providerFactories;
	private readonly ITokenCache _tokens;
	private readonly IDictionary<string, IAuthenticationProvider> _providers = new Dictionary<string, IAuthenticationProvider>();

	public AuthenticationService
	(
		ILogger<AuthenticationService> logger,
		IEnumerable<IProviderFactory> providerFactories,
		ITokenCache tokens
	)
	{
		_logger = logger;
		_providerFactories = providerFactories;
		_tokens = tokens;
		_tokens.Cleared += TokensCleared;
	}

	public string[] Providers => _providers.Keys.ToArray();

	public async ValueTask<bool> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default)
	{
		var ct = cancellationToken ?? CancellationToken.None;
		var authProvider = await AuthenticationProvider(provider, ct);

		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Attempting to login");
		var tokens = await authProvider.LoginAsync(dispatcher, credentials, ct);

		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Login complete, saving tokens");
		await _tokens.SaveAsync(authProvider.Name, tokens, ct);

		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Save tokens complete");
		return await IsAuthenticated(ct);
	}

	public async ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken? cancellationToken = default)
	{
		var ct = cancellationToken ?? CancellationToken.None;
		var authProvider = await AuthenticationProvider(default, ct);

		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Attempting to logout");
		if (!await authProvider.LogoutAsync(dispatcher, ct))
		{
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Logout failed (for example logout cancelled)");
			return false;
		}

		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Logout successful, so clear token cache");
		await _tokens.ClearAsync(ct);

		// Don't raise LoggedOut event here - if there were tokens, then the ITokenCache.Cleared event will
		// be raised, which in turn will trigger the LoggedOut event to be raised
		return true;
	}

	public async ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default)
	{
		var ct = cancellationToken ?? CancellationToken.None;
		var authProvider = await AuthenticationProvider(default, ct);
		if (await IsAuthenticated(cancellationToken))
		{
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Attempting to refresh");
			var tokens = await authProvider.RefreshAsync(ct);

			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Refresh complete, saving new tokens");
			await _tokens.SaveAsync(authProvider.Name, tokens, ct);

			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Save tokens complete");
			// Successful refresh requires there to be tokens stored
			return await IsAuthenticated(cancellationToken);
		}

		// If not able to refresh, either no tokens, or some other provider specific reason,
		// return false to indicate the user should not be treated as authenticated.
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Not logged in, so unable to refresh");
		return false;
	}

	public async ValueTask<bool> IsAuthenticated(CancellationToken? cancellationToken = default)
	{
		var ct = cancellationToken ?? CancellationToken.None;
		// Successful refresh requires there to be tokens stored
		var isAuthenticated = await _tokens.HasTokenAsync(ct);
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Is authenticated - {isAuthenticated}");
		return isAuthenticated;
	}

	public async ValueTask<IEnumerable<Claim>> GetClaims(string tokenType = TokenCacheExtensions.IdTokenKey, CancellationToken? cancellationToken = default)
	{
		var ct = cancellationToken ?? CancellationToken.None;

		var tokens = await _tokens.GetAsync(ct);

		if(tokens.TryGetValue(tokenType, out var idToken))
		{
			var jwtToken = new JwtSecurityToken(idToken);
			return jwtToken.Claims;
		}
		return Enumerable.Empty<Claim>();
	}

	private void TokensCleared(object sender, EventArgs e)
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Tokens cleared, raising LoggedOut event");
		LoggedOut?.Invoke(this, EventArgs.Empty);
	}

	private async Task<IAuthenticationProvider> AuthenticationProvider(string? provider, CancellationToken cancellation)
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Retrieving authentication provider '{provider}'");
		if (provider is null)
		{
			provider = await _tokens.GetCurrentProviderAsync(cancellation);
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"No provider specified, so retrieving current provider from token cache '{provider}'");
		}

		if (_providers.Count == 0)
		{
			BuildProviders();
		}

		if (_providers.Count == 0 &&
			_logger.IsEnabled(LogLevel.Error))
		{
			_logger.LogErrorMessage($"No providers specified for the application");
		}

		return _providers.TryGetValue(provider ?? string.Empty, out var authProvider) ? authProvider : _providers.First().Value;
	}

	private void BuildProviders()
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Building authentication providers");
		foreach (var factory in _providerFactories)
		{
			_providers[factory.Name] = factory.AuthenticationProvider;
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Authentication provider '{factory.Name}' created");
		}
	}
}
