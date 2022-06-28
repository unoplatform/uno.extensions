namespace TestHarness.Ext.Authentication.Custom;

public record class CustomAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	// See https://dummyjson.com/docs/auth for username/password values
	public string? Name { get; set; } = "kminchelle";
	public string? Password { get; set; } = "0lelplR";
	public async Task Login()
	{
		await Flow.LoginAsync(new Dictionary<string, string>()
		{
			{"Name",Name??string.Empty },
			{"Password",Password??string.Empty}
		}, CancellationToken.None);
	}
}
