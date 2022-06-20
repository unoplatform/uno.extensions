namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationService
(
	IServiceProvider Services,
	ITokenCache Tokens,
	CustomAuthenticationSettings Settings
) : BaseAuthenticationService(Tokens)
{
	public async override Task<bool> CanRefresh() => Settings.RefreshCallback is not null && await Tokens.HasTokenAsync();

	public async override Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if (Settings.LoginCallback is null)
		{
			return false;
		}
		return await Settings.LoginCallback(Services, dispatcher, Tokens, credentials!, cancellationToken);
	}

	protected async override Task<bool> InternalLogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		if (Settings.LogoutCallback is not null)
		{
			var loggedOut = await Settings.LogoutCallback(Services, dispatcher, Tokens, cancellationToken);
			if (!loggedOut)
			{
				return false;
			}
		}
		return true;
	}

	protected async override Task<bool> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings.RefreshCallback is null)
		{
			return false;
		}
		return await Settings.RefreshCallback(Services, Tokens, cancellationToken);
	}
}
