namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationService : BaseAuthenticationService
{
	private readonly ILogger _logger;
	private readonly ITokenCache _tokens;
	private readonly IPublicClientApplication _pca;
	private readonly string[] _scopes;

	public MsalAuthenticationService(
		ILogger<MsalAuthenticationService> Logger,
		MsalAuthenticationSettings settings,
		IOptions<MsalConfiguration> configuration,
		ITokenCache tokens) : base(tokens)
	{
		_logger = Logger;
		_tokens = tokens;


		_scopes = settings.Scopes ?? new string[] { };

		var config = configuration.Value;
		var authBuilder = settings.Builder!;
		if (config is not null)
		{
			_scopes = config.Scopes ?? _scopes;

			if (!string.IsNullOrWhiteSpace(config.RedirectUri))
			{
				authBuilder.WithRedirectUri(config.RedirectUri);
			}
		}

		_pca = authBuilder.Build();
	}
	public async override Task<bool> CanRefresh() => (await _pca.GetAccountsAsync()).Count() > 0;

	public async override Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		try
		{
			var result = await AcquireTokenAsync(dispatcher);
			//_user = !string.IsNullOrEmpty(result?.AccessToken)
			//	? CreateContextFromAuthResult(result!)
			//	: default;

			await _tokens.SaveAsync(
			new Dictionary<string, string>
			{
				{ "AccessToken", result?.AccessToken??string.Empty}
			});

			return !string.IsNullOrWhiteSpace(result?.AccessToken);
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

	protected async override Task<bool> InternalLogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		var accounts = await _pca.GetAccountsAsync();
		var firstAccount = accounts.FirstOrDefault();
		if (firstAccount == null)
		{
			_logger.LogInformation(
			  "Unable to find any accounts to log out of.");
		}
		else
		{

			await _pca.RemoveAsync(firstAccount);
			_logger.LogInformation($"Removed account: {firstAccount.Username}, user succesfully logged out.");
		}

		return true;
	}
	protected async override Task<bool> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		var result = await AcquireSilentTokenAsync();

		await _tokens.SaveAsync(
			new Dictionary<string, string>
			{
				{ "AccessToken", result?.AccessToken??string.Empty}
			});

		return !string.IsNullOrWhiteSpace(result?.AccessToken);
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
		return dispatcher.ExecuteAsync(async () => await _pca
		  .AcquireTokenInteractive(_scopes)
		  .WithUnoHelpers()
		  .ExecuteAsync());
	}


	private async Task<AuthenticationResult?> AcquireSilentTokenAsync()
	{
		var accounts = await _pca.GetAccountsAsync();
		var firstAccount = accounts.FirstOrDefault();

		if (firstAccount == null)
		{
			_logger.LogInformation("Unable to find Account in MSAL.NET cache");
			return default;
		}

		if (accounts.Any())
		{
			_logger.LogInformation($"Number of Accounts: {accounts.Count()}");
		}

		try
		{
			_logger.LogInformation("Attempting to perform silent sign in . . .");
			_logger.LogInformation($"Authentication Scopes: {JsonSerializer.Serialize(_scopes)}");

			_logger.LogInformation($"Account Name: {firstAccount.Username}");

			return await _pca
			  .AcquireTokenSilent(_scopes, firstAccount)
			  //.WaitForRefresh(false)
			  .ExecuteAsync();
		}
		catch (MsalUiRequiredException ex)
		{
			_logger.LogWarning(ex, ex.Message);
			_logger.LogWarning(
			  "Unable to retrieve silent sign in Access Token");
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, ex.Message);
			_logger.LogWarning("Unable to retrieve silent sign in details");
		}

		return default;
	}
}
