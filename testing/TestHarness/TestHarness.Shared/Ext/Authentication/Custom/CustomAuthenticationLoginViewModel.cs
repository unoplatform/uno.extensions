namespace TestHarness.Ext.Authentication.Custom;

public record class CustomAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public string? Name { get; set; }
	public string? Password { get; set; }	
	public async Task Login()
	{
		await Flow.Login(new Dictionary<string, string>()
		{
			{"Name",Name??string.Empty },
			{"Password",Password??string.Empty}
		});
	}
}
