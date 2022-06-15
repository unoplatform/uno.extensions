
namespace Uno.Extensions.Authentication;

public record CustomAuthenticationService
(
	ITokenCache Tokens,
	CustomAuthenticationSettings Settings
) : IAuthenticationService
{
	public async Task<bool> Login(IDispatcher dispatcher, IDictionary<string, string>? credentials = null)
	{
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

		await Tokens.Clear();
		return true;
	}
	public async Task<bool> Refresh()
	{
		if(Settings.RefreshCallback is null)
		{
			return false;
		}
		return await Settings.RefreshCallback(Tokens);
	}
}
