#if WINDOWS
using Microsoft.Identity.Client.Broker;
#endif
using Uno.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
#if UNO_EXT_MSAL
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
#endif

namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationProvider(
		ILogger<MsalAuthenticationProvider> ProviderLogger,
		IOptionsSnapshot<MsalConfiguration> Configuration,
		ITokenCache Tokens,
		IStorage Storage,
		MsalAuthenticationSettings? Settings = null) : BaseAuthenticationProvider(ProviderLogger, DefaultName, Tokens)
{
	public const string DefaultName = "Msal";
#if UNO_EXT_MSAL
	private const string CacheFileName = "msal.cache";
	// Distinct from CacheFileName so a plaintext fallback cache is never read by (or corrupts)
	// the platform-protected accessor once secure storage becomes available again.
	private const string UnprotectedCacheFileName = "msal.cache.plaintext-fallback";

	// The system-browser flow can't detect an abandoned sign-in (closed browser window), so an
	// unbounded wait leaves the awaiting login command busy forever. Five minutes matches Uno's
	// WebAuthenticationBroker default (WinRTFeatureConfiguration.WebAuthenticationBroker).
	private static readonly TimeSpan DefaultInteractiveTimeout = TimeSpan.FromMinutes(5);

	private IPublicClientApplication? _pca;
	private string[]? _scopes;

	public void Build(Window? window)
	{
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Building MSAL Provider");
		var config = Configuration.Get(Name) ?? new MsalConfiguration();
		var builder = PublicClientApplicationBuilder.CreateWithApplicationOptions(config);

		ApplyPlatformRedirectUri(builder, config);

#if WINDOWS
		if (window is { })
		{
			builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
			builder.WithParentActivityOrWindow(() =>
			{
				IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
				return hwnd;
			});
		}
		else
		{
			Logger.LogError("Error: Passing a Window instance is now required. Ensure a valid Window is provided via the .AddMSal overload that takes a Window parameter. Avoiding passing a Window could cause a MsalClientException (\"Only loopback redirect URIs are supported, but a non - loopback URI was found...\") to be thrown.");
		}
#endif

		builder.WithUnoHelpers();

		// Last, so the app's callback wins over everything above it - the platform redirect URI,
		// the broker, and what WithUnoHelpers sets (on WebAssembly that is the HttpClient factory;
		// an app-supplied one was silently replaced when this ran earlier). Same ordering as
		// InteractiveBuild in AcquireInteractiveTokenAsync.
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Invoking settings Build callback");
		Settings?.Build?.Invoke(builder);

		_scopes = config.Scopes ?? new string[] { };
		if (_scopes.Length == 0 &&
			Settings?.Scopes is not null)
		{
			_scopes = Settings.Scopes;
		}

		_pca = builder.Build();

		// The effective redirect URI (defaults, configuration and the Build callback applied) is
		// the most common sign-in failure point, so surface it above Trace: this exact URI - path
		// included, port ignored for localhost - must be registered on the Entra app registration.
		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Using RedirectUri '{_pca.AppConfig.RedirectUri ?? "(none - platform managed)"}'; sign-in requires a matching redirect URI on the app registration");
		}

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Building MSAL Provider complete");
	}

	/// <summary>
	/// Applies the platform's conventional redirect URI so apps don't have to hand-write a
	/// per-platform <c>#if</c> block in their <c>Builder(...)</c> callback.
	/// </summary>
	/// <remarks>
	/// Precedence, lowest to highest: this default, then <c>RedirectUri</c> from configuration,
	/// then the app's <c>Builder(...)</c> callback (which runs after this and simply overwrites).
	/// Set <see cref="MsalConfiguration.UseDefaultPlatformRedirectUri"/> to <c>false</c> to opt out
	/// entirely and take MSAL's own default.
	/// </remarks>
	private void ApplyPlatformRedirectUri(PublicClientApplicationBuilder builder, MsalConfiguration config)
	{
		var platform = CurrentRedirectPlatform;

		// Package's Apple implementation returns CFBundleIdentifier. It lives in the WinRT layer
		// (Uno.dll), which on Skia mobile heads is always the *native* build (it "follows the
		// WinRT layer" in Uno.Sdk's RuntimeAssetsSelectorTask), so this works from the plain
		// netX.0 build of this assembly too - unlike Foundation.NSBundle, which only compiles on
		// the ios TFM. FamilyName is string.Empty when the plist key is missing, which
		// GetPlatformRedirectUri already treats as "nothing to derive".
		var bundleId = platform == MsalRedirectPlatform.IOS
			? global::Windows.ApplicationModel.Package.Current.Id.FamilyName
			: null;

		var decision = MsalRedirectDefaults.Apply(
			builder,
			config,
			platform,
			bundleId,
			static b => b.WithWebRedirectUri());

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectUri resolution: {decision}");
	}

	/// <summary>
	/// The platform whose redirect-URI convention applies, resolved at runtime.
	/// </summary>
	private static MsalRedirectPlatform CurrentRedirectPlatform
	{
		get
		{
			// Runtime check rather than a symbol: WebAssembly shares the browserwasm TFM with the
			// generic Skia stack, which is how the rest of this provider detects it too.
			if (PlatformHelper.IsWebAssembly)
			{
				return MsalRedirectPlatform.WebAssembly;
			}

#if WINDOWS
			// WinAppSDK head: the WAM broker owns the redirect URI. Compile-time on purpose -
			// OperatingSystem.IsWindows() is also true on Skia desktop, where Desktop/localhost
			// is correct.
			return MsalRedirectPlatform.BrokerManaged;
#else
			// Runtime, not compile-time: on Skia iOS/Android heads Uno.Sdk substitutes the plain
			// netX.0 build of this assembly, so the TFM no longer implies the OS.
			if (OperatingSystem.IsAndroid())
			{
				return MsalRedirectPlatform.Android;
			}
			// IsIOS() is documented to also return true on Mac Catalyst, where the msauth scheme
			// would be wrong: MSAL has no catalyst build, so a Skia Catalyst head loads MSAL's
			// desktop flavor, whose system-browser flow needs the Desktop/localhost convention.
			if (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst())
			{
				return MsalRedirectPlatform.IOS;
			}
			return MsalRedirectPlatform.Desktop;
#endif
		}
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		try
		{
			if (dispatcher is null)
			{
				throw new ArgumentNullException(nameof(dispatcher), "IDispatcher required to call LoginAsync on MSAL provider");
			}

			await SetupStorage(cancellationToken);

			var result = await AcquireTokenAsync(dispatcher, cancellationToken);

			// No token means "not signed in", not "signed in with an empty token": TokenCache keys
			// off the entry's presence, not its value, so storing string.Empty here would leave
			// IsAuthenticated reporting true with nothing to send. Returning default clears the
			// cache instead. See InternalRefreshAsync for the path this actually happens on.
			return TokensOrNull(result);
		}
		catch (OperationCanceledException)
		{
			// Login was cancelled by the caller; not a failure worth logging.
			throw;
		}
		catch (MsalClientException ex)
		{
			// Typically thrown when the user dismisses the sign-in UI before authenticating;
			// rethrow untouched so callers can inspect the MSAL error code.
			if (Logger.IsEnabled(LogLevel.Warning)) Logger.LogWarningMessage($"MSAL login failed [{ex.ErrorCode}] - {ex.Message}");
			throw;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage(ex, $"MSAL login failed - {ex.Message}");
			throw;
		}
	}

	/// <remarks>
	/// <paramref name="dispatcher"/> is not required: nothing here shows UI, and the documented
	/// <c>IAuthenticationService.LogoutAsync(CancellationToken)</c> overload passes none.
	/// </remarks>
	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		await SetupStorage(cancellationToken);

		// Every account: a survivor keeps its refresh token and silent sign-in picks it up again.
		// ToArray first: RemoveAsync mutates the cache this enumerable reads from.
		var accounts = (await _pca!.GetAccountsAsync()).ToArray();
		var removed = 0;
		foreach (var account in accounts)
		{
			await _pca.RemoveAsync(account);
			removed++;
		}

		if (removed == 0)
		{
			Logger.LogInformation("Unable to find any accounts to log out of.");
		}
		else if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Removed {removed} account(s), user successfully logged out");
		}

		return true;
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		await SetupStorage(cancellationToken);

		if (!(await _pca!.GetAccountsAsync()).Any())
		{
			return default;
		}

		try
		{
			// null when the refresh token expired or was revoked (MsalUiRequiredException): the user
			// has to sign in again. An empty access token here would leave TokenCache.HasTokenAsync
			// - which counts keys, not values - reporting authenticated with nothing to send.
			return TokensOrNull(await AcquireSilentTokenAsync(cancellationToken));
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (MsalUiRequiredException)
		{
			return default;
		}
		catch (Exception ex)
		{
			// Anything else - the token endpoint unreachable, a 5xx, a throttled request - says
			// nothing about whether the session is still valid, so the cached tokens stand: signing
			// the user out over a network blip is the wrong answer, and throwing would turn a
			// startup RefreshAsync into a crash when offline.
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarning(ex, "Silent token refresh failed for a reason other than an expired session; keeping the current tokens");
			}
			return await Tokens.GetAsync(cancellationToken);
		}
	}

	/// <summary>
	/// The token dictionary for <paramref name="result"/>, or <c>null</c> when there is no usable
	/// access token - which <c>AuthenticationService</c> turns into a cleared cache and a
	/// not-authenticated result.
	/// </summary>
	private static IDictionary<string, string>? TokensOrNull(AuthenticationResult? result) =>
		result?.AccessToken is { Length: > 0 } accessToken
			? new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, accessToken } }
			: null;


	private Task<bool>? _setupStorageTask;

	private async ValueTask SetupStorage(CancellationToken cancellationToken)
	{
		// Retry on a later call if the previous attempt failed (e.g. the keychain was locked
		// during the first login); latch only a successful setup or a deterministic skip.
		var setup = _setupStorageTask;
		if (setup is null || (setup.IsCompleted && (!setup.IsCompletedSuccessfully || !setup.Result)))
		{
			_setupStorageTask = setup = SetupStorageCore(cancellationToken);
		}
		await setup.ConfigureAwait(false);
	}

	private async Task<bool> SetupStorageCore(CancellationToken cancellationToken)
	{
		try
		{
#if UNO_EXT_MSAL_BROWSER
			// MsalCacheHelper has no browser backend (DPAPI, Keychain, libsecret only); the cache stays
			// in memory for the session.
			return true;
#else
			return await SetupDesktopStorage(cancellationToken).ConfigureAwait(false);
#endif
		}
		catch (OperationCanceledException)
		{
			// Treated as "not set up" so the next login retries; the caller's own
			// cancellation surfaces from its next cancellable operation.
			return false;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error))
			{
				Logger.LogErrorMessage(ex, $"Error setting up storage for MSAL - {ex.Message}; continuing with in-memory token cache (sign-in state won't survive an app restart)");
			}
			return false;
		}
	}

#if !UNO_EXT_MSAL_BROWSER
	/// <summary>
	/// Desktop: registers <see cref="MsalCacheHelper"/> (DPAPI / Keychain / libsecret) over a cache
	/// file, falling back to an unprotected file only when the app opted in. Mobile targets return
	/// immediately: MSAL.NET persists its cache natively there.
	/// </summary>
	private async Task<bool> SetupDesktopStorage(CancellationToken cancellationToken)
	{
		if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"MSAL persists the token cache natively on this platform");
			}

			return true;
		}

		cancellationToken.ThrowIfCancellationRequested();

		var folderPath = await Storage.CreateFolderAsync(Name.ToLower()).ConfigureAwait(false);
		if (folderPath is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Folder should not be null, exiting Msal storage setup; continuing with in-memory token cache (sign-in state won't survive an app restart)");
			}
			return false;
		}

		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"MSAL cache {Path.Combine(folderPath, CacheFileName)}");
		}

		var config = Configuration.Get(Name);
		var builder = new StorageCreationPropertiesBuilder(CacheFileName, folderPath);
		MsalStorageDefaults.ApplyDefaults(
			builder,
			// AppConfig reflects the final builder state, including a ClientId supplied via
			// the Settings.Build callback rather than configuration
			_pca!.AppConfig.ClientId,
			config?.KeychainServiceName,
			config?.KeychainAccountName,
			OperatingSystem.IsMacOS(),
			OperatingSystem.IsLinux());
		Settings?.Store?.Invoke(builder);
		var storage = builder.Build();
		try
		{
			var cacheHelper = await MsalCacheHelper.CreateAsync(storage).ConfigureAwait(false);
			cacheHelper.VerifyPersistence();
			cacheHelper.RegisterCache(_pca!.UserTokenCache);
		}
		catch (MsalCachePersistenceException ex)
		{
			if (config?.AllowUnprotectedTokenCacheFallback != true)
			{
				if (Logger.IsEnabled(LogLevel.Error))
				{
					Logger.LogErrorMessage(ex, $"Secure token-cache storage isn't available; continuing with in-memory token cache (sign-in state won't survive an app restart). Set 'AllowUnprotectedTokenCacheFallback' to true in the Msal configuration to persist tokens in an unprotected file instead, or configure storage explicitly via the Storage() builder extension");
				}
				return false;
			}

			// A distinct file name keeps the plaintext cache apart from the protected one, so a
			// later-recovered secure store never reads plaintext content.
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarning(ex, "Secure token-cache storage isn't available; falling back to an unprotected cache file at {CacheFilePath} (AllowUnprotectedTokenCacheFallback is enabled)", Path.Combine(folderPath, UnprotectedCacheFileName));
			}

			var fallbackBuilder = new StorageCreationPropertiesBuilder(UnprotectedCacheFileName, folderPath);
			Settings?.Store?.Invoke(fallbackBuilder);
			var fallback = fallbackBuilder
				.WithUnprotectedFile()
				.Build();
			var cacheHelper = await MsalCacheHelper.CreateAsync(fallback).ConfigureAwait(false);
			cacheHelper.VerifyPersistence();
			cacheHelper.RegisterCache(_pca!.UserTokenCache);
		}

		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"MSAL storage setup completed");
		}

		return true;
	}
#endif

	private async Task<AuthenticationResult?> AcquireTokenAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		AuthenticationResult? authentication;
		try
		{
			authentication = await AcquireSilentTokenAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (MsalUiRequiredException)
		{
			authentication = null;
		}
		catch (Exception ex)
		{
			// A sign-in is what the caller asked for, so a failed silent attempt - whatever the
			// reason - is answered by prompting rather than by failing the login.
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarning(ex, "Silent sign-in failed; falling back to interactive sign-in");
			}
			authentication = null;
		}

		if (string.IsNullOrEmpty(authentication?.AccessToken))
		{
			authentication = await AcquireInteractiveTokenAsync(dispatcher, cancellationToken);
		}

		return authentication;
	}

	private ValueTask<AuthenticationResult> AcquireInteractiveTokenAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		return dispatcher.ExecuteAsync(async cancellation =>
		{
			var interactive = _pca!
			  .AcquireTokenInteractive(_scopes)
			  .WithUnoHelpers();

			// After WithUnoHelpers so an app can override what the helpers set.
			Settings?.InteractiveBuild?.Invoke(interactive);

			var timeout = InteractiveTimeout;
			if (timeout <= TimeSpan.Zero)
			{
				return await interactive.ExecuteAsync(cancellation);
			}

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			timeoutCts.CancelAfter(timeout);
			try
			{
				return await interactive.ExecuteAsync(timeoutCts.Token);
			}
			// MSAL surfaces a cancelled web UI as MsalClientException (authentication_canceled)
			// rather than OperationCanceledException; match both so the timeout is logged whichever
			// shape this MSAL version produces. A caller-requested cancellation is not logged.
			catch (Exception ex) when (
				timeoutCts.IsCancellationRequested &&
				!cancellation.IsCancellationRequested &&
				ex is OperationCanceledException or MsalClientException)
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarningMessage($"Interactive sign-in did not complete within {timeout} and was treated as cancelled (for example, the browser window was closed). Adjust via 'InteractiveTimeout' in the Msal configuration section");
				}
				throw;
			}
		}, cancellationToken);
	}

	/// <summary>
	/// The interactive sign-in timeout in effect: the configured value, else
	/// <see cref="DefaultInteractiveTimeout"/> on desktop only, else none.
	/// </summary>
	/// <remarks>
	/// Only the desktop system-browser flow cannot tell that the user closed the browser; the
	/// broker, the mobile browsers and the WebAssembly popup all report a dismissed sign-in, so an
	/// unconfigured timeout there would only cut off a slow multi-factor sign-in.
	/// </remarks>
	private TimeSpan InteractiveTimeout =>
		Configuration.Get(Name)?.InteractiveTimeout
			?? (CurrentRedirectPlatform == MsalRedirectPlatform.Desktop ? DefaultInteractiveTimeout : TimeSpan.Zero);

	/// <summary>
	/// MSAL's cached token for the first account, refreshed if needed. Throws
	/// <see cref="MsalUiRequiredException"/> when the session cannot be renewed silently; other
	/// failures propagate for the caller to decide between prompting and keeping state.
	/// </summary>
	private async Task<AuthenticationResult?> AcquireSilentTokenAsync(CancellationToken cancellationToken)
	{
		var accounts = await _pca!.GetAccountsAsync();
		var firstAccount = accounts.FirstOrDefault();

		if (firstAccount == null)
		{
			Logger.LogInformation("Unable to find Account in MSAL.NET cache");
			return default;
		}

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Number of Accounts: {accounts.Count()}");
			Logger.LogInformationMessage($"Authentication Scopes: {ToJson(_scopes)}");
		}

		try
		{
			Logger.LogInformation("Attempting to perform silent sign in . . .");

			return await _pca
			  .AcquireTokenSilent(_scopes, firstAccount)
			  .ExecuteAsync(cancellationToken);
		}
		catch (MsalUiRequiredException ex)
		{
			// The refresh token expired, was revoked, or the tenant now demands interaction.
			// Code, not message: the message can carry correlation and tenant details.
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarning("Silent sign-in requires user interaction [{ErrorCode}]", ex.ErrorCode);
			}
			throw;
		}
	}

	static string? ToJson (string[]? values)
	{
		if (values == null)
		{
			return null;
		}

		return JsonSerializer.Serialize(values, StringArrayJsonSerializerContext.Default.StringArray);
	}

#endif
}

#if UNO_EXT_MSAL
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(string[]))]
internal sealed partial class StringArrayJsonSerializerContext : JsonSerializerContext
{
}
#endif  // UNO_EXT_MSAL
