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
		if (!config.UseDefaultPlatformRedirectUri)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Default platform RedirectUri disabled by configuration");
			return;
		}

		if (config.RedirectUri is { Length: > 0 })
		{
			// CreateWithApplicationOptions already applied it; don't overwrite a configured value.
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectUri supplied by configuration, skipping platform default");
			return;
		}

		if (PlatformHelper.IsWebAssembly)
		{
			// Derived from Uno's WebAuthenticationBroker rather than from the client id.
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Configuring Web RedirectUri");
			builder.WithWebRedirectUri();
			return;
		}

		string? bundleId = null;
#if IOS
		bundleId = global::Foundation.NSBundle.MainBundle.BundleIdentifier;
#endif

		var platformRedirectUri = MsalRedirectDefaults.GetPlatformRedirectUri(
			config.ClientId,
			bundleId,
#if ANDROID
			isAndroid: true,
#else
			isAndroid: false,
#endif
#if IOS
			isIOS: true);
#else
			isIOS: false);
#endif

		if (platformRedirectUri is { Length: > 0 })
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Configuring platform RedirectUri '{platformRedirectUri}'");
			builder.WithRedirectUri(platformRedirectUri);
			return;
		}

#if WINDOWS
		// WinAppSDK head only: the WAM broker configured below owns the redirect URI, so leave it
		// alone rather than changing behaviour on a path this prototype hasn't exercised.
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Broker-managed RedirectUri, no platform default applied");
#else
		// Desktop and anything else: http://localhost on .NET, which is what the system-browser
		// flow needs (https://aka.ms/msal-net-default-reply-uri).
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Configuring MSAL default RedirectUri");
		builder.WithDefaultRedirectUri();
#endif
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

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		if (dispatcher is null)
		{
			throw new ArgumentNullException(nameof(dispatcher), "IDispatcher required to call LogoutAsync on MSAL provider");
		}

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
		return dispatcher.ExecuteAsync(async cancellation => await _pca!
		  .AcquireTokenInteractive(_scopes)
		  .WithUnoHelpers()
		  .ExecuteAsync(cancellationToken));
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
