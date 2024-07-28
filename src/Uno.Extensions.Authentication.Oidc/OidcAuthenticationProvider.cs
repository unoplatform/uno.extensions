using IdentityModel.OidcClient.Browser;

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

		if (PlatformHelper.IsWebAssembly)
		{
			config.RedirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString;
			config.PostLogoutRedirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString;
		}
		config.Browser = Browser;
		_client = new OidcClient(config);
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (_client is null)
		{
			return default;
		}

		var authenticationResult = await _client.LoginAsync();

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

		await _client.LogoutAsync();
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
