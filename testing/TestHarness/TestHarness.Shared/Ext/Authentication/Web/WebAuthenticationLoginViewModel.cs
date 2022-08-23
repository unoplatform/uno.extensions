namespace TestHarness.Ext.Authentication.Web;

public record class WebAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public async void Login()
	{
		await Flow.LoginAsync(new Dictionary<string, string>(){ { "LoginMetaData", "SocialPlatform" } });
	}
}
