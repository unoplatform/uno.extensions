namespace TestHarness.Ext.Authentication.Oidc;

internal record class OidcAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationService Authentication, IAuthenticationRouteInfo RouteInfo)
{
	public async void Login()
	{
		var authenticated = await Authentication.LoginAsync(null);
		if (authenticated)
		{
			await Navigator.NavigateViewModelAsync(this, RouteInfo.HomeViewModel, qualifier: Qualifiers.ClearBackStack);
		}
	}
}
