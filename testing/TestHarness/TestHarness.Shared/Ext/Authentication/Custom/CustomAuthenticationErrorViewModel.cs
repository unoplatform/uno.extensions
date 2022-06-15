
namespace TestHarness.Ext.Authentication.Custom;

public record class CustomAuthenticationErrorViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public async Task Login()
	{
		await Flow.LaunchAsync();
	}
}
