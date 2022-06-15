namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationService
(
	ITokenCache Tokens,
	CustomAuthenticationSettings Settings
) : IAuthenticationService
{
	public async Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials = null)
	{
		if(Settings.LoginCallback is null)
		{
			return false;
		}
		return await Settings.LoginCallback(dispatcher, Tokens, credentials!);
	}

	public async Task<bool> Logout(IDispatcher dispatcher)
	{
		if (Settings.LogoutCallback is not null)
		{
			var loggedOut = await Settings.LogoutCallback(dispatcher, Tokens);
			if (!loggedOut)
			{
				return false;
			}
		}

		await Tokens.ClearAsync();
		return true;
	}
	public async Task<bool> RefreshAsync()
	{
		if (Settings.RefreshCallback is null)
		{
			return false;
		}
		return await Settings.RefreshCallback(Tokens);
	}
}
