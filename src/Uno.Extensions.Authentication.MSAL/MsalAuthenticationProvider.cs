#if WINDOWS
using Microsoft.Identity.Client.Broker;
#endif
using Uno.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
#if UNO_EXT_MSAL
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MsalCacheHelper = Microsoft.Identity.Client.Extensions.Msal.MsalCacheHelper;
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

		// Before the app callback, so an app calling WithRedirectUri itself always wins.
		ApplyPlatformRedirectUri(builder, config);

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Invoking settings Build callback");
		Settings?.Build?.Invoke(builder);

		_scopes = config.Scopes ?? new string[] { };
		if (_scopes.Length == 0 &&
			Settings?.Scopes is not null)
		{
			_scopes = Settings.Scopes;
		}

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
			return new Dictionary<string, string>
							{
								{ TokenCacheExtensions.AccessTokenKey, result?.AccessToken??string.Empty}
							};
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
	/// Deliberately does not require <paramref name="dispatcher"/>, unlike login: nothing here shows
	/// UI - <c>RemoveAsync</c> only mutates MSAL's own cache. Requiring one made
	/// <c>IAuthenticationService.LogoutAsync(CancellationToken)</c> - the documented overload, which
	/// passes no dispatcher - throw <see cref="ArgumentNullException"/> every time, which callers see
	/// as sign-out silently doing nothing once their command swallows the exception. The Oidc
	/// provider ignores the parameter too.
	/// </remarks>
	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		await SetupStorage(cancellationToken);
		var accounts = await _pca!.GetAccountsAsync();
		var firstAccount = accounts.FirstOrDefault();
		if (firstAccount == null)
		{
			Logger.LogInformation(
			  "Unable to find any accounts to log out of.");
		}
		else
		{

			await _pca.RemoveAsync(firstAccount);
			Logger.LogInformation("Removed account, user successfully logged out.");
		}

		return true;
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		await SetupStorage(cancellationToken);

		if ((await _pca!.GetAccountsAsync()).Count() > 0)
		{


			var result = await AcquireSilentTokenAsync(cancellationToken);

			return new Dictionary<string, string>
			{
				{ TokenCacheExtensions.AccessTokenKey, result?.AccessToken??string.Empty}
			};
		}

		return default;
	}


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
#if UNO_EXT_MSAL_NOSTORAGE
			// WebAssembly: MsalCacheHelper has no browser persistence; MSAL caches tokens
			// in memory for the lifetime of the session.
			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformationMessage($"Token cache persistence isn't supported on WebAssembly; tokens are cached in memory only");
			}

			return true;
#else
			if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
			{
				// MSAL.NET persists the token cache natively on mobile targets; MsalCacheHelper
				// only supports desktop platforms (Windows/macOS/Linux).
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage($"MSAL persists the token cache natively on this platform");
				}

				return true;
			}

			cancellationToken.ThrowIfCancellationRequested();

			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Setting up storage location");
			}

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
				Logger.LogTraceMessage($"Folder: {folderPath}");
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

				// The app opted into keeping sign-in state at the cost of storing the cache in an
				// unprotected file. A distinct file name keeps the plaintext cache apart from the
				// protected one, so a later-recovered secure store never reads plaintext content.
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

	private async Task<AuthenticationResult?> AcquireTokenAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		var authentication = await AcquireSilentTokenAsync(cancellationToken);

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

			var timeout = Configuration.Get(Name)?.InteractiveTimeout ?? DefaultInteractiveTimeout;
			if (timeout <= TimeSpan.Zero)
			{
				return await interactive.ExecuteAsync(cancellationToken);
			}

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
				!cancellationToken.IsCancellationRequested &&
				ex is OperationCanceledException or MsalClientException)
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarningMessage($"Interactive sign-in did not complete within {timeout} and was treated as cancelled (for example, the browser window was closed). Adjust via 'InteractiveTimeout' in the Msal configuration section");
				}
				throw;
			}
		});
	}


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
		catch (OperationCanceledException)
		{
			// Don't treat cancellation as "silent sign-in unavailable" — that would
			// escalate a cancelled login to an interactive prompt.
			throw;
		}
		catch (MsalUiRequiredException ex)
		{
			Logger.LogWarning(ex, ex.Message);
			Logger.LogWarning(
			  "Unable to retrieve silent sign in Access Token");
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, ex.Message);
			Logger.LogWarning("Unable to retrieve silent sign in details");
		}

		return default;
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
