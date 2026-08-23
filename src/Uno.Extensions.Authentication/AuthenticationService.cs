namespace Uno.Extensions.Authentication;

internal class AuthenticationService : IAuthenticationService
{
	public event EventHandler? LoggedOut;

	private readonly ILogger _logger;
	private readonly IEnumerable<IProviderFactory> _providerFactories;
	private readonly ITokenCache _tokens;

	/// <summary>
	/// The providers, built once on first use and never mutated afterwards.
	/// </summary>
	/// <remarks>
	/// Was a plain <see cref="Dictionary{TKey, TValue}"/> populated by a <c>Count == 0</c> check.
	/// Two concurrent authentication calls - an HTTP handler refreshing while the UI asks
	/// <c>IsAuthenticated</c>, say - could both see it empty, both populate it, and corrupt it
	/// mid-write; the <c>_providers.First()</c> fallback could throw on a dictionary being written
	/// concurrently. <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> builds it exactly
	/// once, and nothing writes to it after that.
	/// </remarks>
	private readonly Lazy<IDictionary<string, IAuthenticationProvider>> _providers;

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
		_providers = new Lazy<IDictionary<string, IAuthenticationProvider>>(
			BuildProviders,
			LazyThreadSafetyMode.ExecutionAndPublication);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Deliberately does not force the providers to be built: it reports them only once something
	/// else has. Building constructs real clients - MSAL builds an
	/// <c>IPublicClientApplication</c> - and reading a property should not have that side effect,
	/// nor start throwing where it used to return empty. Consumers bind to this during page
	/// construction (see <c>MsalAuthenticationWelcomeViewModel</c>), so this preserves the previous
	/// observable behaviour exactly; making it build on read would be a separate, deliberate change.
	/// </remarks>
	public string[] Providers => _providers.IsValueCreated
		? _providers.Value.Keys.ToArray()
		: Array.Empty<string>();

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

			if (tokens is not { Count: > 0 })
			{
				// The provider could not renew the session (an expired or revoked refresh token, say).
				// Clear rather than save-empty so ITokenCache.Cleared fires: that is what raises
				// LoggedOut and lets providers drop their own persisted state, exactly as an explicit
				// sign-out would. The user was authenticated a moment ago and now is not.
				if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Refresh produced no tokens, clearing token cache");
				await _tokens.ClearAsync(ct);
				return false;
			}

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

	private void TokensCleared(object? sender, EventArgs e)
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

		var providers = _providers.Value;

		if (providers.Count == 0 &&
			_logger.IsEnabled(LogLevel.Error))
		{
			_logger.LogErrorMessage($"No providers specified for the application");
		}

		return providers.TryGetValue(provider ?? string.Empty, out var authProvider) ? authProvider : providers.First().Value;
	}

	private IDictionary<string, IAuthenticationProvider> BuildProviders()
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Building authentication providers");
		var providers = new Dictionary<string, IAuthenticationProvider>();
		foreach (var factory in _providerFactories)
		{
			providers[factory.Name] = factory.AuthenticationProvider;
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Authentication provider '{factory.Name}' created");
		}

		return providers;
	}
}
