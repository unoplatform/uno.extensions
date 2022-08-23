namespace TestHarness.Ext.Authentication.Custom;

public record class CustomAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationFlow Flow)
{
	public string? Name { get; set; } = DummyJsonEndpointConstants.ValidUserName;
	public string? Password { get; set; } = DummyJsonEndpointConstants.ValidPassword;
	public async void Login()
	{
		await Flow.LoginAsync(new Dictionary<string, string>()
		{
			{nameof(CustomAuthenticationCredentials.Username),Name??string.Empty },
			{nameof(CustomAuthenticationCredentials.Password),Password??string.Empty}
		});
	}
}
