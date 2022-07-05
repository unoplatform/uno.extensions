namespace TestHarness.Ext.Authentication.Oidc;

public record class OidcAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public async Task Login()
	{
		await Flow.LoginAsync(null);
	}
}
