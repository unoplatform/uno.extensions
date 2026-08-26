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
		MsalKeyValueStorage KeyValueStorage,
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

		// After _pca is assigned, so the handler always has a client id to key the entry with.
		// Relies on Build running once per provider, which ProviderFactory's Lazy guarantees.
		Tokens.Cleared += OnTokensCleared;

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
		try
		{
			foreach (var account in accounts)
			{
				// Cancellation is deliberately not checked between removals: a half-done sign-out
				// leaves accounts, refresh tokens and IsAuthenticated behind, and each removal is a
				// local cache mutation with nothing slow to interrupt.
				await _pca.RemoveAsync(account);
				removed++;
			}
		}
		finally
		{
			// Explicit and in finally: MSAL's after-access callback only deletes the blob when it
			// was registered and the cache changed. CancellationToken.None so an already-cancelled
			// token cannot turn the delete into a no-op - refresh-token material surviving a
			// sign-out is the outcome this guards. Off the browser nothing writes the key, so this
			// is a no-op rather than a special case.
			await ClearTokenCacheStoreAsync(CancellationToken.None);
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

	/// <summary>
	/// Removes the serialized MSAL cache from the default <c>IKeyValueStorage</c>, swallowing failures.
	/// </summary>
	/// <remarks>
	/// Never throws: this runs from logout and from <see cref="ITokenCache.Cleared"/>, and neither
	/// should fail because browser storage was unavailable. A surviving blob is logged so it is
	/// diagnosable rather than silent.
	/// </remarks>
	private async ValueTask ClearTokenCacheStoreAsync(CancellationToken cancellationToken)
	{
		try
		{
			await new MsalTokenCacheStore(KeyValueStorage.Storage, Logger, _pca?.AppConfig.ClientId)
				.ClearAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Unable to remove the stored token cache; sign-in state may survive this sign-out - {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Belt and braces for <see cref="ClearTokenCacheStoreAsync"/>: anything that empties the Uno
	/// token cache must not leave the MSAL cache behind, even when it did not come through
	/// <see cref="InternalLogoutAsync"/>.
	/// </summary>
	/// <remarks>
	/// Fire-and-forget because <see cref="ITokenCache.Cleared"/> is a synchronous event;
	/// <see cref="ClearTokenCacheStoreAsync"/> owns the try/catch so nothing escapes (AGENTS.md §10).
	/// Never unsubscribed - the provider and the token cache are both singletons for the host's
	/// lifetime, so there is nothing to leak into.
	/// </remarks>
	private void OnTokensCleared(object? sender, EventArgs e) =>
		_ = ClearTokenCacheStoreAsync(CancellationToken.None).AsTask();

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
			return SetupBrowserStorage();
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

#if UNO_EXT_MSAL_BROWSER
	/// <summary>
	/// WebAssembly: serializes the MSAL cache through the default <c>IKeyValueStorage</c>.
	/// </summary>
	/// <remarks>
	/// MsalCacheHelper knows only DPAPI, Keychain and libsecret. Whether the blob survives a reload
	/// or a tab close is the storage layer's decision (<c>KeyValueStorageConfiguration:BrowserCacheLocation</c>);
	/// this side only reads and writes it, so <c>MemoryStorage</c> yields in-memory-only for free.
	/// Storage failures never fail a sign-in - an empty cache is the pre-persistence behavior.
	/// </remarks>
	private bool SetupBrowserStorage()
	{
		var cache = new MsalTokenCacheStore(KeyValueStorage.Storage, Logger, _pca!.AppConfig.ClientId);

		_pca.UserTokenCache.SetBeforeAccessAsync(async args =>
		{
			try
			{
				if (await cache.LoadAsync(args.CancellationToken) is { } blob)
				{
					args.TokenCache.DeserializeMsalV3(blob);
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarning(ex, "Unable to read the stored token cache; continuing with an empty cache (the user will be asked to sign in again)");
				}
			}
		});

		_pca.UserTokenCache.SetAfterAccessAsync(async args =>
		{
			if (!args.HasStateChanged)
			{
				return;
			}

			try
			{
				await cache.SaveAsync(args.TokenCache.SerializeMsalV3(), args.CancellationToken);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarning(ex, "Unable to persist the token cache; sign-in state won't survive a page reload");
				}
			}
		});

		// The serialized cache holds the refresh token. On an unprotected store its lifetime is the
		// only bound on the exposure - 24 non-sliding hours only under an Entra 'spa' registration,
		// 90 sliding days under a public-client one, and nothing here can tell which was issued.
		if (!KeyValueStorage.Storage.IsEncrypted && Logger.IsEnabled(LogLevel.Warning))
		{
			Logger.LogWarningMessage($"The MSAL token cache is being persisted to {KeyValueStorage.Storage.GetType().Name}, which does not protect its contents - the serialized cache includes the refresh token. Register the WebAssembly redirect URI under the Entra 'spa' platform so refresh tokens are capped at 24 non-sliding hours, or set KeyValueStorageConfiguration:BrowserCacheLocation to MemoryStorage to keep nothing in browser storage");
		}

		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"MSAL token cache persisted through {KeyValueStorage.Storage.GetType().Name}");
		}

		return true;
	}
#else
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

		var filePath = Path.Combine(folderPath, CacheFileName);
		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"MSAL cache {filePath}");
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
			MsalStorageDefaults.ForCurrentOS());
		Settings?.Store?.Invoke(builder);
		var storage = builder.Build();
		try
		{
			var cacheHelper = await MsalCacheHelper.CreateAsync(storage).ConfigureAwait(false);
			VerifyPersistenceIfNeeded(cacheHelper, config, filePath);
			cacheHelper.RegisterCache(_pca!.UserTokenCache);
			ArmWriteVerification(filePath);
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
			// Always verified, whatever VerifyCachePersistence says: this path only runs because the
			// protected store already failed, and an ordinary file carries none of the cost that
			// makes the check worth skipping on the keychain.
			cacheHelper.VerifyPersistence();
			cacheHelper.RegisterCache(_pca!.UserTokenCache);
			ArmWriteVerification(Path.Combine(folderPath, UnprotectedCacheFileName));
		}

		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"MSAL storage setup completed");
		}

		return true;
	}

	/// <summary>
	/// Runs <see cref="MsalCacheHelper.VerifyPersistence"/> when the configured
	/// <see cref="MsalConfiguration.VerifyCachePersistence"/> mode calls for it.
	/// </summary>
	/// <remarks>
	/// The check costs a write, a read and a delete against the platform's secure store. On macOS it
	/// also cannot ever be granted once and for all: MSAL probes with a keychain entry whose service
	/// name carries a fresh <see cref="Guid"/> every run
	/// (<c>MacKeychainAccessor.CreateForPersistenceValidation</c>), so the OS treats each launch as a
	/// first-ever access and re-prompts no matter how many times the user answers "Always Allow".
	/// Skipping it once the store has demonstrably worked is what keeps a signed-in user from being
	/// asked again on every start.
	/// </remarks>
	private void VerifyPersistenceIfNeeded(MsalCacheHelper cacheHelper, MsalConfiguration? config, string cacheFilePath)
	{
		var mode = config?.VerifyCachePersistence ?? MsalCachePersistenceCheck.Auto;
		if (!MsalStorageDefaults.ShouldVerifyPersistence(mode, cacheAlreadyPersisted: File.Exists(cacheFilePath)))
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Skipping the token-cache persistence check ({mode}); {cacheFilePath} shows the store already accepted a write");
			}

			return;
		}

		cacheHelper.VerifyPersistence();
	}

	/// <summary>
	/// Path of the cache whose first write has yet to be confirmed, or <c>null</c> when nothing is
	/// owed. Taken atomically by <see cref="OnAfterCacheAccessAsync"/> so the check runs once per
	/// storage setup.
	/// </summary>
	private string? _unverifiedCacheFilePath;

	/// <summary>
	/// 1 once <see cref="OnAfterCacheAccessAsync"/> has been attached to the token cache.
	/// </summary>
	private int _writeVerificationRegistered;

	/// <summary>
	/// Arms a one-shot check that the next cache write actually reaches
	/// <paramref name="cacheFilePath"/>.
	/// </summary>
	/// <remarks>
	/// This is what lets <see cref="MsalCachePersistenceCheck.Auto"/> skip the up-front probe without
	/// trading a startup error for silent data loss: <c>MsalCacheHelper</c> logs and swallows write
	/// failures ("Could not write the token cache. Ignoring."), so a store that rejects writes would
	/// otherwise look healthy until the user found themselves signed out after a restart.
	/// <para>
	/// Safe alongside <c>RegisterCache</c>, which occupies the *synchronous*
	/// <c>SetBeforeAccess</c>/<c>SetAfterAccess</c> slots: <c>TokenCache.OnAfterAccessAsync</c>
	/// invokes the synchronous callback and then the asynchronous one, so this observes the state the
	/// helper's own write left behind rather than displacing it.
	/// </para>
	/// </remarks>
	private void ArmWriteVerification(string cacheFilePath)
	{
		Interlocked.Exchange(ref _unverifiedCacheFilePath, cacheFilePath);

		// Registering again would only overwrite the same handler, but the exchange keeps the
		// intent explicit: one handler, re-armed by the field above on every storage setup.
		if (Interlocked.Exchange(ref _writeVerificationRegistered, 1) == 0)
		{
			_pca!.UserTokenCache.SetAfterAccessAsync(OnAfterCacheAccessAsync);
		}
	}

	/// <summary>
	/// Confirms that the first state-changing write reached the store, and schedules another storage
	/// setup when it did not.
	/// </summary>
	private Task OnAfterCacheAccessAsync(TokenCacheNotificationArgs args)
	{
		// Plain read first: once the check is done this is the only cost on every later write.
		if (!args.HasStateChanged || _unverifiedCacheFilePath is null)
		{
			return Task.CompletedTask;
		}

		if (Interlocked.Exchange(ref _unverifiedCacheFilePath, null) is not { } cacheFilePath)
		{
			// Another access won the race and already checked.
			return Task.CompletedTask;
		}

		// Every accessor touches the cache file when it writes - including the macOS one, whose
		// payload goes to the keychain - so the file's absence right after a write means the write
		// was rejected and swallowed.
		if (File.Exists(cacheFilePath))
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Token-cache write confirmed at {cacheFilePath}");
			}

			return Task.CompletedTask;
		}

		if (Logger.IsEnabled(LogLevel.Error))
		{
			Logger.LogErrorMessage($"The token cache was serialized but nothing reached '{cacheFilePath}' - secure storage rejected the write and MsalCacheHelper swallowed the failure. Retrying storage setup; sign-in state won't survive an app restart until it succeeds");
		}

		// Deliberately racy with SetupStorage, which is already written to re-run whenever the
		// latched task is missing or unsuccessful: the worst outcome is one redundant setup. That
		// setup sees no cache file, so the persistence check runs even under Auto and either
		// recovers or takes the AllowUnprotectedTokenCacheFallback path.
		_setupStorageTask = null;

		return Task.CompletedTask;
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
