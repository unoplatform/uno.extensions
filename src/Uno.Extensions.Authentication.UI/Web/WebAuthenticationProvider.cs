using Microsoft.Extensions.Options;

namespace Uno.Extensions.Authentication.Web;

internal record WebAuthenticationProvider
(
	ILogger<WebAuthenticationProvider> ProviderLogger,
	IOptionsSnapshot<WebConfiguration> Configuration,
	IServiceProvider Services,
	ITokenCache Tokens
) : BaseAuthenticationProvider(ProviderLogger, DefaultName, Tokens)
{
	private const string OAuthRedirectUriParameter = "redirect_uri";

	public WebAuthenticationSettings? Settings { get; init; }

	public const string DefaultName = "Web";

	private WebAuthenticationSettings? _internalSettings;
	private WebAuthenticationSettings InternalSettings
	{
		get
		{
			if (_internalSettings is null)
			{
				_internalSettings = Settings ?? new WebAuthenticationSettings();
				var config = Configuration.Get(Name);
				if (config is not null)
				{
					_internalSettings = _internalSettings with
					{
						PrefersEphemeralWebBrowserSession = _internalSettings.PrefersEphemeralWebBrowserSession || config.PrefersEphemeralWebBrowserSession,
						LoginStartUri = !string.IsNullOrWhiteSpace(config.LoginStartUri) ? config.LoginStartUri : _internalSettings.LoginStartUri,
						LoginCallbackUri = !string.IsNullOrWhiteSpace(config.LoginCallbackUri) ? config.LoginCallbackUri : _internalSettings.LoginCallbackUri,
						AccessTokenKey = config.AccessTokenKey is not null && !string.IsNullOrWhiteSpace(config.AccessTokenKey) ? config.AccessTokenKey : _internalSettings.AccessTokenKey,
						RefreshTokenKey = config.RefreshTokenKey is not null && !string.IsNullOrWhiteSpace(config.RefreshTokenKey) ? config.RefreshTokenKey : _internalSettings.RefreshTokenKey,
						LogoutStartUri = !string.IsNullOrWhiteSpace(config.LogoutStartUri) ? config.LogoutStartUri : _internalSettings.LogoutStartUri,
						LogoutCallbackUri = !string.IsNullOrWhiteSpace(config.LogoutCallbackUri) ? config.LogoutCallbackUri : _internalSettings.LogoutCallbackUri,
					};
				}
			}
			return _internalSettings;
		}
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		// An already-cancelled login must not open the sign-in UI at all (spec 012 F4).
		cancellationToken.ThrowIfCancellationRequested();

		var loginStartUri = InternalSettings.LoginStartUri;
		loginStartUri = await PrepareLoginStartUri(credentials, loginStartUri, cancellationToken);

		if (loginStartUri is null ||
			string.IsNullOrWhiteSpace(loginStartUri))
		{
			if (ProviderLogger.IsEnabled(LogLevel.Warning))
			{
				ProviderLogger.LogWarning($"{nameof(InternalSettings.LoginStartUri)} not specified, unable to start login flow");
			}
			return default;
		}

		var loginCallbackUri = InternalSettings.LoginCallbackUri;

		if (string.IsNullOrWhiteSpace(loginCallbackUri) &&
			loginStartUri.Contains(OAuthRedirectUriParameter))
		{
			var args = AuthHttpUtility.ExtractArguments(loginStartUri);
			loginCallbackUri = args[OAuthRedirectUriParameter];
		}


		loginCallbackUri = await PrepareLoginCallbackUri(credentials, loginCallbackUri, cancellationToken);

		if (string.IsNullOrWhiteSpace(loginCallbackUri))
		{
			if (ProviderLogger.IsEnabled(LogLevel.Warning))
			{
				ProviderLogger.LogWarning($"{nameof(InternalSettings.LoginCallbackUri)} not specified and {OAuthRedirectUriParameter} not set in {nameof(InternalSettings.LoginStartUri)}, unable to start login flow");
			}
			return default;
		}

		ApplyPrefersEphemeralWebBrowserSession();

#if WINDOWS
		var userResult = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(loginStartUri), new Uri(loginCallbackUri), cancellationToken);
		var authData = string.Join("&", userResult.Properties.Select(x => $"{x.Key}={x.Value}"))??string.Empty;
#else
		var userResult = await WebAuthenticationBroker
			.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(loginStartUri), new Uri(loginCallbackUri))
			.AsTask(cancellationToken);
		if (userResult?.ResponseStatus == WebAuthenticationStatus.UserCancel)
		{
			// Surfacing cancellation (instead of returning a result) keeps AuthenticationService
			// from saving over - and thereby clearing - the previously cached tokens: a login the
			// user backed out of must not sign them out (spec 012 F5).
			throw new OperationCanceledException("The user cancelled the sign-in flow.");
		}
		if (userResult?.ResponseStatus is { } responseStatus && responseStatus != WebAuthenticationStatus.Success)
		{
			ProviderLogger.LogError("Error signing in: {Status} (error detail {ErrorDetail})", responseStatus, userResult.ResponseErrorDetail);
			return default;
		}
		var authData = userResult?.ResponseData ?? string.Empty;

#endif
		var query = authData.StartsWith(loginCallbackUri) ?
			AuthHttpUtility.ExtractArguments(authData) : // authData is a fully qualified url, so need to extract query or fragment
			AuthHttpUtility.ParseQueryString(authData.TrimStart('#').TrimStart('?')); // authData isn't full url, so just process as query or fragment


		var tokens = new Dictionary<string, string>();
		if (query is null)
		{
			return tokens;
		}

		var accessToken = query.Get(InternalSettings.AccessTokenKey ?? TokenCacheExtensions.AccessTokenKey);
		if (!string.IsNullOrWhiteSpace(accessToken))
		{
			tokens[TokenCacheExtensions.AccessTokenKey] = accessToken;
		}
		var refreshToken = query.Get(InternalSettings.RefreshTokenKey ?? TokenCacheExtensions.RefreshTokenKey);
		if (!string.IsNullOrWhiteSpace(refreshToken))
		{
			tokens[TokenCacheExtensions.RefreshTokenKey] = refreshToken;
		}

		return await PostLogin(credentials, authData, tokens, cancellationToken);
	}

	/// <summary>
	/// Applies <see cref="WebAuthenticationSettings.PrefersEphemeralWebBrowserSession"/> to Uno's
	/// broker configuration - a setting that only exists on Apple targets.
	/// </summary>
	/// <remarks>
	/// Spec 012 F7. On Skia iOS heads, Uno's runtime-asset selector substitutes this assembly's
	/// plain-TFM build (spec 010's mechanism) while the WinRT layer stays native - so
	/// <c>WinRTFeatureConfiguration.WebAuthenticationBroker.PrefersEphemeralWebBrowserSession</c>
	/// exists in the loaded Uno.dll but not on the plain reference surface this build compiles
	/// against. Runtime dispatch therefore has to go through reflection on that branch; the
	/// property is public, stable API. Not exercisable by this repo's CI lanes (project references
	/// are never substituted) - see the spec's exceptions note.
	/// </remarks>
	private void ApplyPrefersEphemeralWebBrowserSession()
	{
#if __IOS__
		WinRTFeatureConfiguration.WebAuthenticationBroker.PrefersEphemeralWebBrowserSession = InternalSettings.PrefersEphemeralWebBrowserSession;
#elif !WINDOWS
		if (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst())
		{
			var property = Type.GetType("Uno.WinRTFeatureConfiguration+WebAuthenticationBroker, Uno")
				?.GetProperty("PrefersEphemeralWebBrowserSession");
			if (property is null)
			{
				if (ProviderLogger.IsEnabled(LogLevel.Debug))
				{
					ProviderLogger.LogDebug("PrefersEphemeralWebBrowserSession is not available on the loaded Uno runtime; the setting is ignored");
				}
				return;
			}

			property.SetValue(null, InternalSettings.PrefersEphemeralWebBrowserSession);
		}
#endif
	}

	protected async virtual Task<string?> PrepareLoginStartUri(IDictionary<string, string>? credentials, string? loginStartUri, CancellationToken cancellationToken)
	{
		if (InternalSettings.PrepareLoginStartUri is not null)
		{
			return await InternalSettings.PrepareLoginStartUri(Services, Tokens, credentials, loginStartUri, cancellationToken);
		}
		return loginStartUri;
	}

	protected async virtual Task<string?> PrepareLoginCallbackUri(IDictionary<string, string>? credentials, string? loginCallbackUri, CancellationToken cancellationToken)
	{
		if (InternalSettings.PrepareLoginCallbackUri is not null)
		{
			return await InternalSettings.PrepareLoginCallbackUri(Services, Tokens, credentials, loginCallbackUri, cancellationToken);
		}
		return loginCallbackUri;
	}

	protected async virtual ValueTask<IDictionary<string, string>?> PostLogin(IDictionary<string, string>? credentials, string redirectUri, IDictionary<string, string> tokens, CancellationToken cancellationToken)
	{
		if (InternalSettings.PostLoginCallback is not null)
		{
			return await InternalSettings.PostLoginCallback(Services, Tokens, credentials, redirectUri, tokens, cancellationToken);
		}
		return tokens;
	}


	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (InternalSettings.RefreshCallback is not null)
		{
			return await InternalSettings.RefreshCallback(Services, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
		}
		return await base.InternalRefreshAsync(cancellationToken);
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		// An already-cancelled logout must not open the end-session UI at all (spec 012 F4).
		cancellationToken.ThrowIfCancellationRequested();

		var logoutStartUri = InternalSettings.LogoutStartUri;
		logoutStartUri = await PrepareLogoutStartUri(await Tokens.GetAsync(cancellationToken), logoutStartUri, cancellationToken);

		if (logoutStartUri is null ||
			string.IsNullOrWhiteSpace(logoutStartUri))
		{
			if (ProviderLogger.IsEnabled(LogLevel.Warning))
			{
				ProviderLogger.LogWarning($"{nameof(InternalSettings.LogoutStartUri)} not specified, unable to start logout flow");
			}
			return false;
		}

		var logoutCallbackUri = InternalSettings.LogoutCallbackUri ?? InternalSettings.LoginCallbackUri;

		if (string.IsNullOrWhiteSpace(logoutCallbackUri) &&
			logoutStartUri.Contains(OAuthRedirectUriParameter))
		{
			var args = AuthHttpUtility.ExtractArguments(logoutStartUri);
			logoutCallbackUri = args[OAuthRedirectUriParameter];
		}

		if (string.IsNullOrWhiteSpace(logoutCallbackUri) &&
			InternalSettings.LoginStartUri is { } loginStartUri &&
			loginStartUri.Contains(OAuthRedirectUriParameter))
		{
			var args = AuthHttpUtility.ExtractArguments(loginStartUri);
			logoutCallbackUri = args[OAuthRedirectUriParameter];
		}

		logoutCallbackUri = await PrepareLogoutCallbackUri(await Tokens.GetAsync(cancellationToken), logoutCallbackUri, cancellationToken);

		if (string.IsNullOrWhiteSpace(logoutCallbackUri))
		{
			if (ProviderLogger.IsEnabled(LogLevel.Warning))
			{
				ProviderLogger.LogWarning($"{nameof(InternalSettings.LogoutCallbackUri)} not specified and {OAuthRedirectUriParameter} not set in {nameof(InternalSettings.LogoutStartUri)}, unable to start logout flow");
			}
			return false;
		}

#if WINDOWS
		await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(logoutStartUri), new Uri(logoutCallbackUri), cancellationToken);
#else
		var userResult = await WebAuthenticationBroker
			.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(logoutStartUri), new Uri(logoutCallbackUri))
			.AsTask(cancellationToken);
		if (userResult?.ResponseStatus is { } responseStatus && responseStatus != WebAuthenticationStatus.Success)
		{
			// Reporting failure keeps the local token cache intact - the user backed out of (or the
			// IdP failed) the end-session flow, so they are still signed in (spec 012 F6).
			ProviderLogger.LogError("Error signing out: {Status} (error detail {ErrorDetail})", responseStatus, userResult.ResponseErrorDetail);
			return false;
		}
#endif
		return true;

	}

	protected async virtual Task<string?> PrepareLogoutStartUri(IDictionary<string, string>? credentials, string? logoutStartUri, CancellationToken cancellationToken)
	{
		if (InternalSettings.PrepareLogoutStartUri is not null)
		{
			return await InternalSettings.PrepareLogoutStartUri(Services, Tokens, credentials, logoutStartUri, cancellationToken);
		}
		return logoutStartUri;
	}

	protected async virtual Task<string?> PrepareLogoutCallbackUri(IDictionary<string, string>? credentials, string? logoutCallbackUri, CancellationToken cancellationToken)
	{
		if (InternalSettings.PrepareLogoutCallbackUri is not null)
		{
			return await InternalSettings.PrepareLogoutCallbackUri(Services, Tokens, credentials, logoutCallbackUri, cancellationToken);
		}
		return logoutCallbackUri;
	}

}

internal record WebAuthenticationProvider<TService>
(
	ILogger<WebAuthenticationProvider<TService>> ServiceLogger,
	IOptionsSnapshot<WebConfiguration> Configuration,
	IServiceProvider Services,
	ITokenCache Tokens
) : WebAuthenticationProvider(ServiceLogger, Configuration, Services, Tokens)
	where TService : notnull
{
	public WebAuthenticationSettings<TService>? TypedSettings
	{
		get => base.Settings as WebAuthenticationSettings<TService>;
		init => base.Settings = value;
	}

	protected async override Task<string?> PrepareLoginStartUri(IDictionary<string, string>? credentials, string? loginStartUri, CancellationToken cancellationToken)
	{
		if (TypedSettings?.PrepareLoginStartUri is not null)
		{
			return await TypedSettings.PrepareLoginStartUri(Services.GetRequiredService<TService>(), Services, Tokens, credentials, loginStartUri, cancellationToken);
		}
		return await base.PrepareLoginStartUri(credentials, loginStartUri, cancellationToken);
	}

	protected async override Task<string?> PrepareLoginCallbackUri(IDictionary<string, string>? credentials, string? loginCallbackUri, CancellationToken cancellationToken)
	{
		if (TypedSettings?.PrepareLoginCallbackUri is not null)
		{
			return await TypedSettings.PrepareLoginCallbackUri(Services.GetRequiredService<TService>(), Services, Tokens, credentials, loginCallbackUri, cancellationToken);
		}
		return await base.PrepareLoginCallbackUri(credentials, loginCallbackUri, cancellationToken);
	}


	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{

		if (TypedSettings?.RefreshCallback is not null)
		{
			return await TypedSettings.RefreshCallback(Services.GetRequiredService<TService>(), Services, Tokens, await Tokens.GetAsync(cancellationToken), cancellationToken);
		}
		return await base.InternalRefreshAsync(cancellationToken);
	}

	protected async override ValueTask<IDictionary<string, string>?> PostLogin(IDictionary<string, string>? credentials, string redirectUri, IDictionary<string, string> tokens, CancellationToken cancellationToken)
	{
		if (TypedSettings?.PostLoginCallback is not null)
		{
			return await TypedSettings.PostLoginCallback(Services.GetRequiredService<TService>(), Services, Tokens, credentials, redirectUri, tokens, cancellationToken);
		}
		return await base.PostLogin(credentials, redirectUri, tokens, cancellationToken);
	}

	protected async override Task<string?> PrepareLogoutStartUri(IDictionary<string, string>? credentials, string? logoutStartUri, CancellationToken cancellationToken)
	{
		if (TypedSettings?.PrepareLogoutStartUri is not null)
		{
			return await TypedSettings.PrepareLogoutStartUri(Services.GetRequiredService<TService>(), Services, Tokens, credentials, logoutStartUri, cancellationToken);
		}
		return await base.PrepareLogoutStartUri(credentials, logoutStartUri, cancellationToken);
	}

	protected async override Task<string?> PrepareLogoutCallbackUri(IDictionary<string, string>? credentials, string? logoutCallbackUri, CancellationToken cancellationToken)
	{
		if (TypedSettings?.PrepareLogoutCallbackUri is not null)
		{
			return await TypedSettings.PrepareLogoutCallbackUri(Services.GetRequiredService<TService>(), Services, Tokens, credentials, logoutCallbackUri, cancellationToken);
		}
		return await base.PrepareLogoutCallbackUri(credentials, logoutCallbackUri, cancellationToken);
	}
}
