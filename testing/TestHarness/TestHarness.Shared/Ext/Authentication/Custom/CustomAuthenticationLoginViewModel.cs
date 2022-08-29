namespace TestHarness.Ext.Authentication.Custom;

internal record class CustomAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationService Authentication, IAuthenticationRouteInfo RouteInfo)
{
	public string? Name { get; set; } = DummyJsonEndpointConstants.ValidUserName;
	public string? Password { get; set; } = DummyJsonEndpointConstants.ValidPassword;
	public async void Login()
	{
		var authenticated = await Authentication.LoginAsync(new Dictionary<string, string>()
		{
			{nameof(CustomAuthenticationCredentials.Username),Name??string.Empty },
			{nameof(CustomAuthenticationCredentials.Password),Password??string.Empty}
		});
		if (authenticated)
		{
			await Navigator.NavigateViewModelAsync(this, RouteInfo.HomeViewModel, qualifier: Qualifiers.ClearBackStack);
		}
	}
}
