namespace TestHarness.Ext.Authentication.MSAL;

public record MsalAuthenticationWelcomeViewModel(INavigator Navigator, IAuthenticationFlow Flow, IAuthenticationService Authentication)
{
	public string[] Providers => Authentication.Providers;

	public string? SelectedProvider { get; set; }

	public async void Login()
	{
		if(SelectedProvider is null)
		{
			SelectedProvider = Providers.First();
		}

		var creds = (SelectedProvider?.StartsWith("Custom") ?? false) ?
			new Dictionary<string, string>(){
				{nameof(CustomAuthenticationCredentials.Username),DummyJsonEndpointConstants.ValidUserName },
				{nameof(CustomAuthenticationCredentials.Password),DummyJsonEndpointConstants.ValidPassword }
			} :
			default;
		await Flow.LoginAsync(credentials: creds, provider: SelectedProvider);
	}
}
