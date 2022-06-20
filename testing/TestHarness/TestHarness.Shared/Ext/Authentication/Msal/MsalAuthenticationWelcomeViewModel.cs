namespace TestHarness.Ext.Authentication.MSAL;

public record MsalAuthenticationWelcomeViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public async Task Login()
	{
		await Flow.LoginAsync(credentials: default, CancellationToken.None);
	}
}
