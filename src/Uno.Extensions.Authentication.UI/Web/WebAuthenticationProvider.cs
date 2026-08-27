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

	/// <summary>
	/// The literal token a configured start URI can carry where the redirect belongs; replaced at
	/// sign-in/out time with the URL-encoded effective callback. This is what lets one static
	/// configuration serve every platform: the callback differs per platform (custom scheme,
	/// origin, loopback), and only the provider knows it at runtime (spec 015).
	/// </summary>
	internal const string RedirectUriPlaceholder = "{RedirectUri}";

	/// <summary>
	/// The literal token a configured login start URI can carry where an OAuth <c>state</c> value
	/// belongs; replaced at sign-in time with a fresh random value that the response must echo
	/// back. This is what binds a redirect to the request this provider started: on the desktop
	/// loopback broker the callback is reachable by any page in the system browser, so without it
	/// a foreign navigation to the callback could hand the app someone else's tokens.
	/// </summary>
	internal const string StatePlaceholder = "{State}";

	private const string OAuthStateParameter = "state";

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
		// An already-cancelled login must not open the sign-in UI at all (spec 013 F4).
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
		if (string.IsNullOrWhiteSpace(loginCallbackUri))
		{
			loginCallbackUri = ExtractRedirectUri(loginStartUri);
		}

		loginCallbackUri = await PrepareLoginCallbackUri(credentials, loginCallbackUri, cancellationToken);
		loginCallbackUri = ResolveCallbackUri(loginCallbackUri, nameof(InternalSettings.LoginCallbackUri), nameof(InternalSettings.LoginStartUri), "login");
		if (loginCallbackUri is null)
		{
			return default;
		}

		// A static start URI cannot know the per-platform callback; the {RedirectUri} token
		// carries it in URL-encoded form (spec 015).
		loginStartUri = loginStartUri.Replace(RedirectUriPlaceholder, Uri.EscapeDataString(loginCallbackUri));

		string? expectedState = null;
		if (loginStartUri.Contains(StatePlaceholder))
		{
			expectedState = NewState();
			loginStartUri = loginStartUri.Replace(StatePlaceholder, expectedState);
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
			// user backed out of must not sign them out (spec 013 F5). The desktop broker reports
			// its own timeout the same way, marked by the error detail.
			var timedOut = userResult.ResponseErrorDetail == DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail;
			if (ProviderLogger.IsEnabled(LogLevel.Information))
			{
				ProviderLogger.LogInformation("Sign-in flow {Outcome} before the identity provider redirected back; the previous session is kept", timedOut ? "timed out" : "was cancelled by the user");
			}

			throw new OperationCanceledException(timedOut
				? "The sign-in flow timed out before the identity provider redirected back to the app."
				: "The user cancelled the sign-in flow.");
		}
		if (userResult?.ResponseStatus is { } responseStatus && responseStatus != WebAuthenticationStatus.Success)
		{
			ProviderLogger.LogError("Error signing in: {Status} (error detail {ErrorDetail})", responseStatus, userResult.ResponseErrorDetail);
			return default;
		}
		var authData = userResult?.ResponseData ?? string.Empty;

#endif
		var query = IsCallbackUrl(authData, loginCallbackUri) ?
			AuthHttpUtility.ExtractArguments(authData) : // authData is a fully qualified url, so need to extract query or fragment
			AuthHttpUtility.ParseQueryString(authData.TrimStart('#').TrimStart('?')); // authData isn't full url, so just process as query or fragment

		if (expectedState is not null &&
			!string.Equals(query?.Get(OAuthStateParameter), expectedState, StringComparison.Ordinal))
		{
			// Not the response to the request this provider started: a stale tab, a replay, or a
			// foreign navigation to the callback. Nothing from it may be trusted.
			ProviderLogger.LogError("Rejecting the sign-in response: its state does not match the request this provider issued");
			return default;
		}

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
	/// The <c>redirect_uri</c> value a start URI carries, or <c>null</c> when it has none - or only
	/// the <see cref="RedirectUriPlaceholder"/>, which is not a value but the slot the resolved
	/// callback is written into (spec 015).
	/// </summary>
	private static string? ExtractRedirectUri(string? startUri)
	{
		if (startUri is null || !startUri.Contains(OAuthRedirectUriParameter))
		{
			return null;
		}

		var extracted = AuthHttpUtility.ExtractArguments(startUri)[OAuthRedirectUriParameter];
		return string.IsNullOrWhiteSpace(extracted) || extracted == RedirectUriPlaceholder ? null : extracted;
	}

	/// <summary>
	/// The callback a flow completes on: the value resolved so far, else - off WinAppSDK, where the
	/// WinRT broker answers <c>ms-app://</c>, wrong for the WinUIEx protocol flow - the platform's
	/// own callback (spec 015). <c>null</c>, after a Warning naming every source tried, when
	/// nothing resolves; login and logout share this so they cannot drift apart.
	/// </summary>
	private string? ResolveCallbackUri(string? callbackUri, string callbackSetting, string startUriSetting, string flow)
	{
		string? brokerError = null;
#if !WINDOWS
		if (string.IsNullOrWhiteSpace(callbackUri))
		{
			(callbackUri, brokerError) = TryGetBrokerCallbackUri();
		}
#endif

		if (string.IsNullOrWhiteSpace(callbackUri))
		{
			if (ProviderLogger.IsEnabled(LogLevel.Warning))
			{
				ProviderLogger.LogWarning(
					"Unable to start {Flow} flow: {Reason}",
					flow,
					NoCallbackReason(callbackSetting, startUriSetting, brokerError));
			}

			return null;
		}

		return callbackUri;
	}

	/// <summary>
	/// Whether the broker's response data is the callback URL itself (parameters on its query or
	/// fragment) rather than a bare parameter string. Compared as URIs so a callback configured
	/// with different casing or an explicit default port still matches the normalized form the
	/// broker hands back.
	/// </summary>
	private static bool IsCallbackUrl(string authData, string callbackUri) =>
		Uri.TryCreate(authData, UriKind.Absolute, out var data) && Uri.TryCreate(callbackUri, UriKind.Absolute, out var callback)
			? string.Equals(data.GetLeftPart(UriPartial.Path), callback.GetLeftPart(UriPartial.Path), StringComparison.OrdinalIgnoreCase)
			: authData.StartsWith(callbackUri, StringComparison.OrdinalIgnoreCase);

	/// <summary>A fresh, URL-safe <c>state</c> value: 128 bits from the OS's CSPRNG, hex-encoded.</summary>
	private static string NewState() =>
		Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

#if !WINDOWS
	/// <summary>
	/// The platform's own callback URI, used when configuration supplies none (spec 015): the
	/// custom scheme on Android/iOS, the app origin on WebAssembly, the loopback listener on Skia
	/// Desktop. Returns a null URI plus the broker's own message when it cannot derive one (for
	/// example, no custom scheme registered), which lands on the not-configured warning path.
	/// </summary>
	/// <returns>
	/// The callback URI, or the reason the broker could not supply one - carried back rather than
	/// logged here so the caller can report it at Warning level as the cause of a flow that
	/// cannot start.
	/// </returns>
	private (string? CallbackUri, string? Error) TryGetBrokerCallbackUri()
	{
		try
		{
			return (WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString, null);
		}
		catch (Exception ex)
		{
			return (null, ex.Message);
		}
	}
#endif

	/// <summary>
	/// Why no callback URI could be resolved, naming every source that was tried: configuration,
	/// the start URI's <c>redirect_uri</c>, and - off WinAppSDK - the platform broker (spec 015).
	/// </summary>
	/// <remarks>
	/// Naming all three matters: a message that names only the two configuration sources reads as
	/// "you forgot redirect_uri" when the real cause is a platform with no callback to derive.
	/// </remarks>
	private static string NoCallbackReason(string callbackSetting, string startUriSetting, string? brokerError)
	{
		var reason =
			$"{callbackSetting} is not configured and {startUriSetting} carries no {OAuthRedirectUriParameter} value " +
			$"(the {RedirectUriPlaceholder} placeholder is replaced with the callback, it is not one)";

#if !WINDOWS
		reason += brokerError is { Length: > 0 }
			? $", and WebAuthenticationBroker could not derive one for this platform: {brokerError}"
			: ", and WebAuthenticationBroker derived no callback for this platform";
		reason += ". A broker-derived callback needs a custom scheme registered for the app - CFBundleURLTypes/CFBundleURLSchemes in Info.plist on iOS and Mac Catalyst, an intent filter on Android - or configure the callback explicitly";
#endif

		return reason;
	}

	/// <summary>
	/// Applies <see cref="WebAuthenticationSettings.PrefersEphemeralWebBrowserSession"/> to Uno's
	/// broker configuration - a setting that only exists on Apple targets.
	/// </summary>
	/// <remarks>
	/// Spec 013 F7. On Skia iOS heads, Uno's runtime-asset selector substitutes this assembly's
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
				// Also the outcome when the linker trimmed the setter from a Release build: say so
				// at a level an app runs with, since the setting silently not applying is the bug.
				if (ProviderLogger.IsEnabled(LogLevel.Warning))
				{
					ProviderLogger.LogWarning("PrefersEphemeralWebBrowserSession is not available on the loaded Uno runtime (missing or trimmed); the setting is ignored");
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
		// An already-cancelled logout must not open the end-session UI at all (spec 013 F4).
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
		if (string.IsNullOrWhiteSpace(logoutCallbackUri))
		{
			logoutCallbackUri = ExtractRedirectUri(logoutStartUri) ?? ExtractRedirectUri(InternalSettings.LoginStartUri);
		}

		logoutCallbackUri = await PrepareLogoutCallbackUri(await Tokens.GetAsync(cancellationToken), logoutCallbackUri, cancellationToken);
		logoutCallbackUri = ResolveCallbackUri(logoutCallbackUri, nameof(InternalSettings.LogoutCallbackUri), nameof(InternalSettings.LogoutStartUri), "logout");
		if (logoutCallbackUri is null)
		{
			return false;
		}

		// A static logout URI cannot know the per-platform callback either (spec 015).
		logoutStartUri = logoutStartUri.Replace(RedirectUriPlaceholder, Uri.EscapeDataString(logoutCallbackUri));

#if WINDOWS
		await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(logoutStartUri), new Uri(logoutCallbackUri), cancellationToken);
#else
		var userResult = await WebAuthenticationBroker
			.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(logoutStartUri), new Uri(logoutCallbackUri))
			.AsTask(cancellationToken);
		if (userResult?.ResponseStatus == WebAuthenticationStatus.UserCancel)
		{
			// Reporting failure keeps the local token cache intact - the user backed out of the
			// end-session flow (or it timed out), so they are still signed in (spec 013 F6).
			if (ProviderLogger.IsEnabled(LogLevel.Information))
			{
				ProviderLogger.LogInformation("Sign-out flow {Outcome} before completing; the session is kept", userResult.ResponseErrorDetail == DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail ? "timed out" : "was cancelled by the user");
			}

			return false;
		}
		if (userResult?.ResponseStatus is { } responseStatus && responseStatus != WebAuthenticationStatus.Success)
		{
			// Same outcome for an identity-provider failure: the user is still signed in.
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
