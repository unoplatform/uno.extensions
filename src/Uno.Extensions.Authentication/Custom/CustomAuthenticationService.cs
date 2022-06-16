namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationService
(
	ITokenCache Tokens,
	CustomAuthenticationSettings Settings
) : IAuthenticationService
{
	public async Task<bool> CanRefresh() => Settings.RefreshCallback is not null;

	public Task<bool> LoginAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		return LoginAsync(dispatcher, null, cancellationToken);
	}

	public async Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		if(Settings.LoginCallback is null)
		{
			return false;
		}
		return await Settings.LoginCallback(dispatcher, Tokens, credentials!, cancellationToken);
	}

	public async Task<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		if (Settings.LogoutCallback is not null)
		{
			var loggedOut = await Settings.LogoutCallback(dispatcher, Tokens, cancellationToken);
			if (!loggedOut)
			{
				return false;
			}
		}

		await Tokens.ClearAsync();
		return true;
	}
	public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
	{
		if (Settings.RefreshCallback is null)
		{
			return false;
		}
		return await Settings.RefreshCallback(Tokens, cancellationToken);
	}
}
