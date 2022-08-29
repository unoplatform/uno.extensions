namespace TestHarness.Ext.Authentication.MSAL;

internal record MsalAuthenticationWelcomeViewModel(INavigator Navigator, IAuthenticationService auth, IAuthenticationService Authentication, IAuthenticationRouteInfo RouteInfo)
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
		var authenticated =await Authentication.LoginAsync(credentials: creds, provider: SelectedProvider);
		if (authenticated)
		{
			await Navigator.NavigateViewModelAsync(this, RouteInfo.HomeViewModel, qualifier: Qualifiers.ClearBackStack);
		}
	}
}
