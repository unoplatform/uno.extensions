namespace TestHarness.Ext.Authentication.Oidc;

public record class OidcAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public async void Login()
	{
		await Flow.LoginAsync(null);
	}
}
