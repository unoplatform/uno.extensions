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

		config.Browser = Browser;
		_client = new OidcClient(config);
		ApplyAutoRedirectUri();
	}

	/// <summary>
	/// With <see cref="OidcAuthenticationSettings.AutoRedirectUri"/>, the redirect follows the broker's
	/// current callback URI. Re-read before every flow rather than captured once in <see cref="Build"/>:
	/// the desktop loopback broker moves to another port when its default one could not be bound,
	/// and a redirect captured at startup would keep sending the identity provider to the dead one.
	/// </summary>
	private void ApplyAutoRedirectUri()
	{
		if (_client is not null && Settings is { AutoRedirectUri: true })
		{
			_client.Options.RedirectUri = _client.Options.PostLogoutRedirectUri = WebAuthenticationBroker
				.GetCurrentApplicationCallbackUri().OriginalString;
		}
	}

	protected override async ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (_client is null)
		{
			ProviderLogger.LogError("Client is not initialized.");
			return default;
		}

		ApplyAutoRedirectUri();
		var authenticationResult = await _client.LoginAsync(cancellationToken: cancellationToken);

		if(authenticationResult.IsError)
		{
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

		ApplyAutoRedirectUri();
		// Pass the cached id_token as the end-session hint: without it the identity provider
		// cannot trust the post-logout redirect, so it stops on its own signed-out page and never
		// redirects back to the app - on desktop the loopback broker then waits out its timeout.
		var idToken = await Tokens.TokenAsync(TokenCacheExtensions.IdTokenKey, cancellationToken);
		await _client.LogoutAsync(
			new LogoutRequest { IdTokenHint = string.IsNullOrWhiteSpace(idToken) ? null : idToken },
			cancellationToken);
		return true;
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		var token = await Tokens.RefreshTokenAsync(cancellationToken);
		if (_client is null || string.IsNullOrWhiteSpace(token))
		{
			return default;
		}

		var result = await _client.RefreshTokenAsync(token);
		var accessToken = result.AccessToken;
		var refreshToken = result.RefreshToken;
		var idToken = result.IdentityToken;

		if (token is not null)
		{
			var creds = new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, accessToken } };
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
}
