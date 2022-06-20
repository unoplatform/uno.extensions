namespace TestHarness.Ext.Authentication.Custom;

public record CustomAuthenticationHomeViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public async Task Logout()
	{
		await Flow.LogoutAsync(CancellationToken.None);
	}
}
