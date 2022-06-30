namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationProvider(
		ILogger<MsalAuthenticationProvider> Logger,
		IOptions<MsalConfiguration> Configuration,
		ITokenCache Tokens,
		MsalAuthenticationSettings? Settings = null) : BaseAuthenticationProvider(DefaultName, Tokens)
{
	public const string DefaultName = "Msal";

	private IPublicClientApplication? _pca;
	private string[]? _scopes;

	public void Build()
	{
		var config = Configuration.Value ?? new MsalConfiguration();
		var builder = PublicClientApplicationBuilder.CreateWithApplicationOptions(config);

		Settings?.Build?.Invoke(builder);

		_scopes = Settings?.Scopes ?? new string[] { };

		if (PlatformHelper.IsWebAssembly)
		{
			builder.WithWebRedirectUri();
		}
		builder.WithUnoHelpers();

		_pca = builder.Build();
	}


	public async override ValueTask<bool> CanRefresh(CancellationToken cancellation) => (await _pca!.GetAccountsAsync()).Count() > 0;

	public async override ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		try
		{
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

	public async override ValueTask<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
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
	public async override ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{
		var result = await AcquireSilentTokenAsync();

		return new Dictionary<string, string>
			{
				{ TokenCacheExtensions.AccessTokenKey, result?.AccessToken??string.Empty}
			};
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
		return dispatcher.ExecuteAsync(async () => await _pca!
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
			  //.WaitForRefresh(false)
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
}
