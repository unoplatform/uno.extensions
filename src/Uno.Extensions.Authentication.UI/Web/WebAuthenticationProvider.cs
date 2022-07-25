
using System.Web;

namespace Uno.Extensions.Authentication.Web;

internal record WebAuthenticationProvider
(
	IServiceProvider Services,
	ITokenCache Tokens
) : BaseAuthenticationProvider(DefaultName, Tokens)
{
	public WebAuthenticationSettings? Settings { get; init; }

	public const string DefaultName = "Web";
	public async override ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		var loginStartUri = Settings?.LoginStartUri;
		if (Settings?.PrepareLoginStartUri is not null)
		{
			loginStartUri = await Settings.PrepareLoginStartUri(loginStartUri, credentials, cancellationToken);
		}

		if (string.IsNullOrWhiteSpace(loginStartUri))
		{
			return default;
		}

		var loginCallbackUri = Settings?.LoginCallbackUri;
		if (Settings?.PrepareLoginCallbackUri is not null)
		{
			loginCallbackUri = await Settings.PrepareLoginCallbackUri(loginCallbackUri, credentials, cancellationToken);
		}

		if (string.IsNullOrWhiteSpace(loginCallbackUri))
		{
			return default;
		}

#if WINDOWS
		var userResult = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(loginStartUri), new Uri(loginCallbackUri));
		var authData = string.Join("&", userResult.Properties.Select(x => $"{x.Key}={x.Value}"))??string.Empty;
#else
		var userResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(loginStartUri), new Uri(loginCallbackUri));
		var authData = userResult?.ResponseData??string.Empty;

#endif
		var idx = authData.IndexOf("?");
		if (idx>=0 && idx<authData.Length-1)
		{
			authData = authData.Substring(idx+1);
		}

		if (string.IsNullOrWhiteSpace(authData))
		{
			return default;
		}

		var query = AuthHttpUtility.ParseQueryString(authData ?? string.Empty);

		var tokens = new Dictionary<string, string>();
		if (query is null)
		{
			return tokens;
		}

		var accessToken = query.Get(Settings?.AccessTokenKey ?? TokenCacheExtensions.AccessTokenKey);
		if (!string.IsNullOrWhiteSpace(accessToken))
		{
			tokens[TokenCacheExtensions.AccessTokenKey] = accessToken;
		}
		var refreshToken = query.Get(Settings?.RefreshTokenKey ?? TokenCacheExtensions.RefreshTokenKey);
		if (!string.IsNullOrWhiteSpace(refreshToken))
		{
			tokens[TokenCacheExtensions.RefreshTokenKey] = refreshToken;
		}
		if (Settings?.IdTokenKey is not null)
		{
			var idToken = query.Get(Settings.IdTokenKey);
			if (!string.IsNullOrWhiteSpace(idToken))
			{
				tokens[Settings.IdTokenKey] = idToken;
			}
		}
		if (Settings?.OtherTokenKeys is not null)
		{
			foreach (var key in Settings.OtherTokenKeys)
			{
				var token = query.Get(key.Key);
				if (!string.IsNullOrWhiteSpace(token))
				{
					tokens[key.Value] = token;
				}
			}
		}

		return await PostLogin(credentials, tokens, cancellationToken);
	}

	protected async virtual ValueTask<IDictionary<string, string>?> PostLogin(IDictionary<string, string>? credentials, IDictionary<string,string> tokens, CancellationToken cancellationToken)
	{
		if (Settings?.PostLoginCallback is not null)
		{
			return await Settings.PostLoginCallback(Services, credentials, tokens, cancellationToken);
		}
		return tokens;
	}


	public async override ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings?.RefreshCallback is null)
		{
			return default;
		}
		return await Settings.RefreshCallback(Services, await Tokens.GetAsync(cancellationToken), cancellationToken);
	}

	public async override ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		var logoutStartUri = Settings?.LogoutStartUri;
		if (Settings?.PrepareLogoutStartUri is not null)
		{
			logoutStartUri = await Settings.PrepareLogoutStartUri(logoutStartUri, await Tokens.GetAsync(), cancellationToken);
		}

		if (string.IsNullOrWhiteSpace(logoutStartUri))
		{
			return true;
		}

		var logoutCallbackUri = Settings?.LogoutCallbackUri ?? Settings?.LoginCallbackUri;
		if (Settings?.PrepareLogoutCallbackUri is not null)
		{
			logoutCallbackUri = await Settings.PrepareLogoutCallbackUri(logoutCallbackUri, await Tokens.GetAsync(), cancellationToken);
		}

		if (string.IsNullOrWhiteSpace(logoutCallbackUri))
		{
			return false;
		}

#if WINDOWS
		var userResult = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(logoutStartUri), new Uri(logoutCallbackUri));
		var authData = string.Join("&", userResult.Properties.Select(x => $"{x.Key}={x.Value}"));
#else
		var userResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(logoutStartUri), new Uri(logoutCallbackUri));
		var authData = userResult?.ResponseData;

#endif
		return true;

	}

}

internal record WebAuthenticationProvider<TService>
(
	IServiceProvider Services,
	ITokenCache Tokens
) : WebAuthenticationProvider(Services, Tokens)
	where TService : notnull
{
	public WebAuthenticationSettings<TService>? TypedSettings {
		get => base.Settings as WebAuthenticationSettings<TService>;
		init => base.Settings = value;
	}


	public async override ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{

		if (TypedSettings?.RefreshCallback is not null)
		{
			return await TypedSettings.RefreshCallback(Services.GetRequiredService<TService>(), await Tokens.GetAsync(cancellationToken), cancellationToken);
		}
		return await base.RefreshAsync(cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> PostLogin(IDictionary<string, string>? credentials, IDictionary<string, string> tokens, CancellationToken cancellationToken)
	{
		if (TypedSettings?.PostLoginCallback is not null)
		{
			return await TypedSettings.PostLoginCallback(Services.GetRequiredService<TService>(), credentials, tokens, cancellationToken);
		}
		return await base.PostLogin(credentials, tokens, cancellationToken);
	}
}
