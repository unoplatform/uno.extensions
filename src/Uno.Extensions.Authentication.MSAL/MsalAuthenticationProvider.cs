#if WINDOWS
using Microsoft.Identity.Client.Broker;
#endif
using Uno.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
#if UNO_EXT_MSAL
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

	private IPublicClientApplication? _pca;
	private string[]? _scopes;

	public void Build(Window? window)
	{
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Building MSAL Provider");
		var config = Configuration.Get(Name) ?? new MsalConfiguration();
		var builder = PublicClientApplicationBuilder.CreateWithApplicationOptions(config);

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Invoking settings Build callback");
		Settings?.Build?.Invoke(builder);

		_scopes = config.Scopes ?? new string[] { };
		if (_scopes.Length == 0 &&
			Settings?.Scopes is not null)
		{
			_scopes = Settings.Scopes;
		}

		if (PlatformHelper.IsWebAssembly)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Configuring Web RedirectUri");
			builder.WithWebRedirectUri();
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

	protected async override ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		try
		{
			if (dispatcher is null)
			{
				throw new ArgumentNullException(nameof(dispatcher), "IDispatcher required to call LoginAsync on MSAL provider");
			}

			await SetupStorage();

			var result = await AcquireTokenAsync(dispatcher);
			return new Dictionary<string, string>
							{
								{ TokenCacheExtensions.AccessTokenKey, result?.AccessToken??string.Empty}
							};
		}
		catch (MsalClientException ex)
		{
			//This is thrown when the user closes the webview before he can authenticate
			throw new MsalClientException(ex.ErrorCode, ex.Message);
		}
		catch (Exception ex)
		{
			throw new Exception(ex.Message);
		}
	}

	protected async override ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		if (dispatcher is null)
		{
			throw new ArgumentNullException(nameof(dispatcher), "IDispatcher required to call LogoutAsync on MSAL provider");
		}

		await SetupStorage();
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
			Logger.LogInformation($"Removed account: {firstAccount.Username}, user succesfully logged out.");
		}

		return true;
	}

	protected async override ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		await SetupStorage();

		if ((await _pca!.GetAccountsAsync()).Count() > 0)
		{


			var result = await AcquireSilentTokenAsync();

			return new Dictionary<string, string>
			{
				{ TokenCacheExtensions.AccessTokenKey, result?.AccessToken??string.Empty}
			};
		}

		return default;
	}


	private bool _isCompleted;
	private async Task SetupStorage()
	{
		try
		{
			if (_isCompleted)
			{
				return;
			}
			_isCompleted = true;

#if WINDOWS_UWP || !NET6_0_OR_GREATER
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"No further action required for setting up storage");
			}

			return;
#else
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Setting up storage location");
			}

			var folderPath = await Storage.CreateFolderAsync(Name.ToLower());
			if (folderPath is null)
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarningMessage($"Folder should not be null, exiting Msal storage setup");
				}
				return;
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

			var builder = new StorageCreationPropertiesBuilder(CacheFileName, folderPath);
			Settings?.Store?.Invoke(builder);
			var storage = builder.Build();
			var cacheHelper = await MsalCacheHelper.CreateAsync(storage);
			cacheHelper.RegisterCache(_pca!.UserTokenCache);
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"MSAL storage setup completed");
			}
#endif
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error))
			{
				Logger.LogErrorMessage($"Error setting up storage for MSAL - {ex.Message}");
			}
		}
	}

	private async Task<AuthenticationResult?> AcquireTokenAsync(IDispatcher dispatcher)
	{
		var authentication = await AcquireSilentTokenAsync();

		if (string.IsNullOrEmpty(authentication?.AccessToken))
		{
			authentication = await AcquireInteractiveTokenAsync(dispatcher);
		}

		return authentication;
	}

	private ValueTask<AuthenticationResult> AcquireInteractiveTokenAsync(IDispatcher dispatcher)
	{
		return dispatcher.ExecuteAsync(async cancellation => await _pca!
		  .AcquireTokenInteractive(_scopes)
		  .WithUnoHelpers()
		  .ExecuteAsync());
	}


	private async Task<AuthenticationResult?> AcquireSilentTokenAsync()
	{
		var accounts = await _pca!.GetAccountsAsync();
		var firstAccount = accounts.FirstOrDefault();

		if (firstAccount == null)
		{
			Logger.LogInformation("Unable to find Account in MSAL.NET cache");
			return default;
		}

		if (accounts.Any())
		{
			Logger.LogInformation($"Number of Accounts: {accounts.Count()}");
		}

		try
		{
			Logger.LogInformation("Attempting to perform silent sign in . . .");
			Logger.LogInformation($"Authentication Scopes: {JsonSerializer.Serialize(_scopes)}");

			Logger.LogInformation($"Account Name: {firstAccount.Username}");

			return await _pca
			  .AcquireTokenSilent(_scopes, firstAccount)
			  .ExecuteAsync();
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
#endif
}
