using Duende.IdentityModel.OidcClient.Browser;

namespace Uno.Extensions.Authentication.Oidc;

internal record OidcAuthenticationProvider(
		ILogger<OidcAuthenticationProvider> ProviderLogger,
		IBrowser Browser,
		IOptionsSnapshot<OidcClientOptions> Configuration,
		ITokenCache Tokens,
		OidcAuthenticationSettings? Settings = null) : BaseAuthenticationProvider(ProviderLogger, DefaultName, Tokens)
{
	public const string DefaultName = "Oidc";

	private OidcClient? _client;

	public void Build()
	{
		var config = Settings?.Options ?? Configuration.Get(Name) ?? new OidcClientOptions();

		if (Settings is { AutoRedirectUri: true })
		{
			config.RedirectUri = config.PostLogoutRedirectUri = WebAuthenticationBroker
				.GetCurrentApplicationCallbackUri().OriginalString;
		}

		config.Browser = Browser;
		_client = new OidcClient(config);
	}

	protected override async ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (_client is null)
		{
			ProviderLogger.LogError("Client is not initialized.");
			return default;
		}

		var authenticationResult = await _client.LoginAsync(cancellationToken: cancellationToken);

		if (authenticationResult.IsError)
		{
			if (authenticationResult.Error == nameof(BrowserResultType.UserCancel))
			{
				// OidcClient reports a browser result type by its name when the IBrowser supplies
				// no error text (WebAuthenticatorBrowser leaves it null on purpose). Surfacing
				// cancellation instead of returning null keeps AuthenticationService from clearing
				// the previously cached tokens: a login the user backed out of must not sign them
				// out (spec 013 F5) - the same contract as the Web and MSAL providers.
				if (ProviderLogger.IsEnabled(LogLevel.Information))
				{
					ProviderLogger.LogInformation("Sign-in flow was cancelled by the user; the previous session is kept");
				}

				throw new OperationCanceledException("The user cancelled the sign-in flow.");
			}

			ProviderLogger.LogError("Error logging in: {Error} - {ErrorDescription}", authenticationResult.Error, authenticationResult.ErrorDescription);
			return default;
		}

		var token = authenticationResult.AccessToken;
		var refreshToken = authenticationResult.RefreshToken;
		var idToken = authenticationResult.IdentityToken;

		if (token is not null)
		{
			var creds = new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, token } };
			if (refreshToken is not null)
			{
				creds[TokenCacheExtensions.RefreshTokenKey] = refreshToken;
			}

			if (idToken is not null)
			{
				creds[TokenCacheExtensions.IdTokenKey] = idToken;
			}

			return creds;
		}
		return default;
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		if (_client is null)
		{
			return true;
		}

		// Pass the cached id_token as the end-session hint: without it the identity provider
		// cannot trust the post-logout redirect, so it prompts the user for confirmation and never
		// redirects back to the app - on desktop the loopback listener then waits until the broker
		// timeout with the UI stuck (spec 013 F11).
		var idToken = await Tokens.TokenAsync(TokenCacheExtensions.IdTokenKey, cancellationToken);
		var result = await _client.LogoutAsync(
			new LogoutRequest { IdTokenHint = string.IsNullOrWhiteSpace(idToken) ? null : idToken },
			cancellationToken);
		if (result.IsError)
		{
			// Reporting failure keeps the local token cache intact - the user backed out of (or the
			// IdP failed) the end-session flow, so they are still signed in.
			ProviderLogger.LogError("Error logging out: {Error} - {ErrorDescription}", result.Error, result.ErrorDescription);
			return false;
		}

		return true;
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		var token = await Tokens.RefreshTokenAsync(cancellationToken);
		if (_client is null || string.IsNullOrWhiteSpace(token))
		{
			return default;
		}

		var result = await _client.RefreshTokenAsync(token, cancellationToken: cancellationToken);
		if (result.IsError && !IsTokenEndpointError(result.Error))
		{
			// The token endpoint unreachable, a 5xx, a throttled request: none of these says the
			// session is over, so the cached tokens stand - signing the user out over a network
			// blip is the wrong answer, and a startup RefreshAsync must not sign out an offline
			// user. Same rule as the MSAL provider. OidcClient folds the transport error into the
			// Error text, so "not a token-endpoint error code" is the test.
			if (ProviderLogger.IsEnabled(LogLevel.Warning))
			{
				ProviderLogger.LogWarning("Silent token refresh failed for a reason other than a rejected refresh token ({Error}); keeping the current tokens", result.Error);
			}

			return await Tokens.GetAsync(cancellationToken);
		}

		if (result.IsError || string.IsNullOrWhiteSpace(result.AccessToken))
		{
			// The identity provider answered and rejected the refresh token: the session is over.
			ProviderLogger.LogError("Error refreshing tokens: {Error} - {ErrorDescription}", result.Error, result.ErrorDescription);
			return default;
		}

		var creds = new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, result.AccessToken } };
		if (result.RefreshToken is not null)
		{
			creds[TokenCacheExtensions.RefreshTokenKey] = result.RefreshToken;
		}

		if (result.IdentityToken is not null)
		{
			creds[TokenCacheExtensions.IdTokenKey] = result.IdentityToken;
		}

		return creds;
	}

	/// <summary>
	/// Whether <paramref name="error"/> is an error code the token endpoint itself returns
	/// (RFC 6749 §5.2) - a verdict on the refresh token - as opposed to the transport or exception
	/// text OidcClient reports when no such verdict was reached.
	/// </summary>
	private static bool IsTokenEndpointError(string? error) =>
		error is "invalid_request" or "invalid_client" or "invalid_grant" or "unauthorized_client" or "unsupported_grant_type" or "invalid_scope";
}
