
using IdentityModel.OidcClient.Browser;
using System.Diagnostics;

namespace Uno.Extensions.Authentication.Oidc;

internal record OidcAuthenticationProvider(
		ILogger<OidcAuthenticationProvider> ProviderLogger,
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
		config.Browser = new WebAuthenticatorBrowser();
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

		if (token is not null)
		{
			var creds = new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, token } };
			if (refreshToken is not null)
			{
				creds[TokenCacheExtensions.RefreshTokenKey] = refreshToken;
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

		if (token is not null)
		{
			var creds = new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, accessToken } };
			if (refreshToken is not null)
			{
				creds[TokenCacheExtensions.RefreshTokenKey] = refreshToken;
			}

			return creds;
		}
		return default;
	}


}




public class WebAuthenticatorBrowser : IBrowser
{
	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		try
		{
#if WINDOWS
		var userResult = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(options.StartUrl), new Uri(options.EndUrl));
		var callbackurl = $"{options.EndUrl}/?{string.Join("&", userResult.Properties.Select(x => $"{x.Key}={x.Value}"))}";
		return new BrowserResult
		{
			Response = callbackurl
		};
#else
			var userResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(options.StartUrl), new Uri(options.EndUrl));

			return new BrowserResult
			{
				Response = userResult.ResponseData
			};
#endif
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
			return new BrowserResult()
			{
				ResultType = BrowserResultType.UnknownError,
				Error = ex.ToString()
			};
		}
	}


}


