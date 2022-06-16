namespace TestHarness.Ext.Authentication.MSAL;

public record MsalAuthenticationHomeViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public async Task Logout()
	{
		await Flow.LogoutAsync();
	}
}
